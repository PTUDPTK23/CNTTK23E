using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<WorkerDb>(o => o.UseNpgsql(
    Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "Host=localhost;Database=supportdb;Username=supportuser;Password=local_demo_password_change"));
builder.Services.AddHostedService<AssignmentWorker>();
await builder.Build().RunAsync();

record TicketCreated(Guid TicketId, string RequesterId, string Priority);
record TicketAssigned(Guid TicketId, string RequesterId, string Status, string AssignedGroup, string AssignedAgent);
class Ticket { public Guid Id { get; set; } public string Status { get; set; } = "New"; public string? AssignedGroup { get; set; } public string? AssignedAgent { get; set; } public DateTimeOffset? UpdatedAt { get; set; } }
class TicketHistory { public long Id { get; set; } public Guid TicketId { get; set; } public string ChangedBy { get; set; } = "worker"; public string Change { get; set; } = ""; public string? OldValue { get; set; } public string? NewValue { get; set; } public DateTimeOffset At { get; set; } }
class WorkerDb(DbContextOptions<WorkerDb> options) : DbContext(options) { public DbSet<Ticket> Tickets => Set<Ticket>(); public DbSet<TicketHistory> History => Set<TicketHistory>(); }

class AssignmentWorker(ILogger<AssignmentWorker> log, IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost", UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest", Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest" };
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var conn = await factory.CreateConnectionAsync(stoppingToken);
                await using var ch = await conn.CreateChannelAsync(cancellationToken: stoppingToken);
                await ch.ExchangeDeclareAsync("support.events", ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await ch.QueueDeclareAsync("assignment.queue", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await ch.QueueBindAsync("assignment.queue", "support.events", "ticket.created", cancellationToken: stoppingToken);
                await ch.BasicQosAsync(0, 1, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(ch);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var evt = JsonSerializer.Deserialize<TicketCreated>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                        if (evt is null) { await ch.BasicAckAsync(ea.DeliveryTag, false, stoppingToken); return; }
                        using var scope = scopes.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<WorkerDb>();
                        var t = await db.Tickets.FindAsync(new object[] { evt.TicketId }, stoppingToken);
                        if (t is not null && t.Status == "New")
                        {
                            var group = string.Equals(evt.Priority, "High", StringComparison.OrdinalIgnoreCase) ? "PrioritySupport" : "GeneralSupport";
                            t.AssignedGroup = group;
                            t.AssignedAgent = group == "PrioritySupport" ? "agent-urgent-01" : "agent-general-01";
                            var old = t.Status; t.Status = "Assigned"; t.UpdatedAt = DateTimeOffset.UtcNow;
                            db.History.Add(new TicketHistory { TicketId = t.Id, ChangedBy = "assignment-worker", Change = "Assignment", OldValue = old, NewValue = $"{t.Status}:{group}:{t.AssignedAgent}", At = DateTimeOffset.UtcNow });
                            await db.SaveChangesAsync(stoppingToken);
                            log.LogInformation("Assigned ticket {TicketId} to {Group}", t.Id, group);
                        }
                        // If publishing the assigned event fails after the database update, the
                        // requeued created event arrives with Status=Assigned. Republish it safely.
                        if (t is not null && t.Status == "Assigned" && t.AssignedGroup is not null && t.AssignedAgent is not null)
                        {
                            var assignedEvent = new TicketAssigned(t.Id, evt.RequesterId, t.Status, t.AssignedGroup, t.AssignedAgent);
                            await ch.BasicPublishAsync("support.events", "ticket.assigned", JsonSerializer.SerializeToUtf8Bytes(assignedEvent), cancellationToken: stoppingToken);
                        }
                        await ch.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex) { log.LogWarning(ex, "Ticket assignment failed; message will be retried"); await ch.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken); }
                };
                await ch.BasicConsumeAsync("assignment.queue", false, consumer, cancellationToken: stoppingToken);
                log.LogInformation("Assignment worker consuming queue");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogWarning(ex, "RabbitMQ unavailable; retrying in 5 seconds"); await Task.Delay(5000, stoppingToken); }
        }
    }
}
