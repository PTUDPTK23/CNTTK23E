using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

namespace Grading.Worker;

public class GradingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<GradingBackgroundService> logger) : BackgroundService
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
                    queue: RabbitMqQueues.SubmissionGrading,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: RabbitMqQueues.SubmissionGraded,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var request = JsonSerializer.Deserialize<SubmissionGradingMessage>(json);

                        if (request is not null)
                        {
                            await ProcessGradingAsync(request, channel, stoppingToken);
                        }

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Lỗi khi chấm bài nộp.");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RabbitMqQueues.SubmissionGrading,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation("[Grading.Worker] Đang lắng nghe yêu cầu chấm bài trên queue '{Queue}'...", RabbitMqQueues.SubmissionGrading);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Grading.Worker] Không thể kết nối RabbitMQ, thử lại sau 3 giây...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task ProcessGradingAsync(
        SubmissionGradingMessage request,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[Grading.Worker] Nhận yêu cầu chấm Submission #{SubmissionId} của sinh viên {StudentId} (Assignment #{AssignmentId}). Đang chấm tự động...",
            request.SubmissionId, request.StudentId, request.AssignmentId);

        // Bước 5: Worker thực hiện chấm tự động theo quy tắc giả lập (delay 3 giây)
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        var (score, comment) = EvaluateSubmission(request.Content);

        // Bước 6: Lưu điểm, nhận xét và trạng thái chấm = "Graded" vào Database
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var submission = await db.Submissions.FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);
        if (submission is null)
        {
            logger.LogWarning("[Grading.Worker] Không tìm thấy Submission #{SubmissionId} trong Database.", request.SubmissionId);
            return;
        }

        submission.Score = score;
        submission.Comment = comment;
        submission.Status = "Graded";

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[Grading.Worker] Đã chấm xong Submission #{SubmissionId}: Status={Status}, Score={Score}, Comment=\"{Comment}\"",
            submission.Id, submission.Status, submission.Score, submission.Comment);

        // Bước 7: Phát sự kiện khi chấm hoàn thành lên RabbitMQ
        var gradedEvent = new SubmissionGradedEvent(
            submission.Id,
            submission.StudentId,
            submission.Status,
            score,
            comment);

        var eventBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(gradedEvent));
        var props = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: RabbitMqQueues.SubmissionGraded,
            mandatory: false,
            basicProperties: props,
            body: eventBytes,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "[Grading.Worker] Đã phát sự kiện chấm hoàn thành cho Submission #{SubmissionId} vào queue '{Queue}'.",
            submission.Id, RabbitMqQueues.SubmissionGraded);
    }

    private static (double Score, string Comment) EvaluateSubmission(string content)
    {
        var text = content.Trim();

        if (text.Contains("pass", StringComparison.OrdinalIgnoreCase) ||
            (text.StartsWith("http", StringComparison.OrdinalIgnoreCase) && text.Length >= 25))
        {
            return (9.5, "Bài làm xuất sắc, vượt qua toàn bộ test case giả lập.");
        }

        if (text.Length >= 15)
        {
            return (8.5, "Bài làm đạt yêu cầu cơ bản, cấu trúc rõ ràng.");
        }

        return (6.5, "Bài làm còn ngắn, cần bổ sung thêm chi tiết.");
    }
}
