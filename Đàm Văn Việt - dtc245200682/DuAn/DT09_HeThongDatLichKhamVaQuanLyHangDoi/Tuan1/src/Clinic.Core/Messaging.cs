using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;

namespace Clinic.Core;

public static class Services
{
    public static IServiceCollection AddClinic(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(NpgsqlDataSource.Create(config.GetConnectionString("Clinic") ?? throw new InvalidOperationException("Missing ConnectionStrings:Clinic")));
        services.AddSingleton<Database>();
        return services;
    }
    public static async Task InitializeClinic(this IServiceProvider services, CancellationToken ct = default)
    {
        var log = services.GetRequiredService<ILogger<Database>>();
        while (!ct.IsCancellationRequested)
        {
            try { await services.GetRequiredService<Database>().Initialize(); return; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning(ex,"Database not ready; retrying migration/seed in 3 seconds");
                await Task.Delay(3000,ct);
            }
        }
    }
}

// Each hosted service owns a separate channel. No channel is shared between concurrent publishers.
public sealed class Broker(IConfiguration config) : IAsyncDisposable
{
    private IConnection? connection;
    private IChannel? channel;
    public async Task<IChannel> Channel(CancellationToken ct)
    {
        if (channel?.IsOpen == true) return channel;
        await Reset();
        var factory = new ConnectionFactory
        {
            Uri = new Uri(config["RabbitMQ:Uri"] ?? throw new InvalidOperationException("Missing RabbitMQ:Uri")),
            AutomaticRecoveryEnabled = false,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            ContinuationTimeout = TimeSpan.FromSeconds(5)
        };
        connection = await factory.CreateConnectionAsync(ct);
        channel = await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled:true,publisherConfirmationTrackingEnabled:true),ct);
        await channel.ExchangeDeclareAsync("clinic",ExchangeType.Direct,true,cancellationToken:ct);
        await channel.ExchangeDeclareAsync("clinic.dead",ExchangeType.Fanout,true,cancellationToken:ct);
        await channel.QueueDeclareAsync("clinic.dead",true,false,false,cancellationToken:ct);
        await channel.QueueBindAsync("clinic.dead","clinic.dead","",cancellationToken:ct);
        foreach(var (queue,route) in new[]{("clinic.numbers","number.requested"),("clinic.notifications","queue.changed")})
        {
            await channel.QueueDeclareAsync(queue,true,false,false,new Dictionary<string,object?>{{"x-dead-letter-exchange","clinic.dead"}},cancellationToken:ct);
            await channel.QueueBindAsync(queue,"clinic",route,cancellationToken:ct);
        }
        await channel.BasicQosAsync(0,1,false,ct);
        return channel;
    }
    public async Task Publish(string route,string body,Guid id,CancellationToken ct)
    {
        var ch = await Channel(ct);
        // Await includes publisher confirm; mandatory unroutable messages also fail the publish.
        await ch.BasicPublishAsync("clinic",route,true,new BasicProperties { Persistent=true,ContentType="application/json",MessageId=id.ToString(),CorrelationId=id.ToString() },Encoding.UTF8.GetBytes(body),ct);
    }
    public async Task Reset()
    {
        if(channel is not null) { try { await channel.DisposeAsync(); } catch { /* reconnect */ } channel=null; }
        if(connection is not null) { try { await connection.DisposeAsync(); } catch { /* reconnect */ } connection=null; }
    }
    public ValueTask DisposeAsync() => new(Reset());
}

public sealed class OutboxPublisher(Database db,IConfiguration config,ILogger<OutboxPublisher> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var broker = new Broker(config);
        while(!ct.IsCancellationRequested)
        {
            try
            {
                await using var c = await db.Open(ct);
                await using var tx = await c.BeginTransactionAsync(ct);
                var rows = await Database.Read(c,"SELECT id,kind,body FROM outbox WHERE sent_at IS NULL ORDER BY created_at,id LIMIT 1 FOR UPDATE SKIP LOCKED",r=>(Id:r.GetGuid(0),Kind:r.GetString(1),Body:r.GetString(2)));
                if(rows.Count==0) { await tx.CommitAsync(ct); await Task.Delay(250,ct); continue; }
                var row=rows[0];
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                await broker.Publish(row.Kind,row.Body,row.Id,timeout.Token);
                await Database.Execute(c,"UPDATE outbox SET sent_at=now() WHERE id=$1",row.Id);
                await tx.CommitAsync(ct);
                log.LogInformation("Outbox confirmed {Kind} {MessageId}",row.Kind,row.Id);
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested) { break; }
            catch(Exception ex)
            {
                log.LogWarning(ex,"Outbox retained for retry");
                await broker.Reset();
                try { await Task.Delay(1500,ct); } catch(OperationCanceledException) { break; }
            }
        }
    }
}

public abstract class RabbitConsumer(IConfiguration config,ILogger log,string queue) : BackgroundService
{
    protected abstract Task Handle(BusMessage message,CancellationToken ct);
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var broker=new Broker(config);
        while(!ct.IsCancellationRequested)
        {
            try
            {
                var ch=await broker.Channel(ct);
                var delivery=await ch.BasicGetAsync(queue,false,ct);
                if(delivery is null) { await Task.Delay(150,ct); continue; }
                try
                {
                    var message=JsonSerializer.Deserialize<BusMessage>(delivery.Body.Span) ?? throw new JsonException("Empty message");
                    if(message.AppointmentId==Guid.Empty || message.DoctorId<=0) throw new JsonException("Invalid message");
                    await Handle(message,ct);
                    await ch.BasicAckAsync(delivery.DeliveryTag,false,ct);
                    log.LogInformation("Processed {Queue} {MessageId} appointment {AppointmentId} redelivered={Redelivered}",queue,message.Id,message.AppointmentId,delivery.Redelivered);
                }
                catch(Exception ex) when(ex is JsonException || ex is RuleException { StatusCode:404 })
                {
                    log.LogError(ex,"Invalid message sent to clinic.dead");
                    await ch.BasicRejectAsync(delivery.DeliveryTag,false,ct);
                }
                // Any other failure closes the channel; RabbitMQ requeues the unacknowledged delivery.
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested) { break; }
            catch(Exception ex)
            {
                log.LogWarning(ex,"Consumer {Queue} reconnecting; unacknowledged message will be retried",queue);
                await broker.Reset();
                try { await Task.Delay(1500,ct); } catch(OperationCanceledException) { break; }
            }
        }
    }
}
