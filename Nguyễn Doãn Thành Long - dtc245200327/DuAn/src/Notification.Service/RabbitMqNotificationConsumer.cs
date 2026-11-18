using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

namespace Notification.Service;

public class RabbitMqNotificationConsumer(
    IHubContext<NotificationHub> hubContext,
    IConfiguration config,
    ILogger<RabbitMqNotificationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            Port = int.TryParse(config["RabbitMQ:Port"], out var p) ? p : 5672,
            UserName = config["RabbitMQ:UserName"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: RabbitMqQueues.SubmissionGraded,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var evt = JsonSerializer.Deserialize<SubmissionGradedEvent>(json);

                        if (evt is not null)
                        {
                            var groupName = $"student:{evt.StudentId}";
                            await hubContext.Clients.Group(groupName).SendAsync("SubmissionGraded", evt, stoppingToken);

                            logger.LogInformation(
                                "[Notification.Service] Đã đẩy thông báo realtime tới '{GroupName}': Submission #{SubmissionId} -> Status={Status}, Score={Score}",
                                groupName, evt.SubmissionId, evt.Status, evt.Score);
                        }

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Lỗi khi đẩy thông báo realtime.");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RabbitMqQueues.SubmissionGraded,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation("[Notification.Service] Đang lắng nghe sự kiện chấm xong trên queue '{Queue}'...", RabbitMqQueues.SubmissionGraded);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Notification.Service] Không thể kết nối RabbitMQ, thử lại sau 3 giây...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
