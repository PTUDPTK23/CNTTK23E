using System.Text.Json;
using FileProcessing.Core.Contracts;
using FileProcessing.Core.Entities;
using FileProcessing.Core.Enums;
using FileProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileProcessing.Infrastructure.Messaging;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherService background task started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FileProcessingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var now = DateTime.UtcNow;

        // Atomic fetch: Pending messages that are scheduled for now or earlier and not locked
        var messages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending &&
                        (m.ScheduledAt == null || m.ScheduledAt <= now) &&
                        (m.LockedUntil == null || m.LockedUntil < now))
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var outboxMsg in messages)
        {
            outboxMsg.Status = OutboxStatus.InFlight;
            outboxMsg.LockedUntil = now.AddMinutes(2);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var outboxMsg in messages)
        {
            try
            {
                var jobMessage = JsonSerializer.Deserialize<ProcessImageJobMessage>(outboxMsg.Payload);
                if (jobMessage != null)
                {
                    jobMessage.MessageId = outboxMsg.Id;
                    var success = await publisher.PublishImageJobAsync(jobMessage, cancellationToken);
                    if (success)
                    {
                        outboxMsg.Status = OutboxStatus.Processed;
                        outboxMsg.ProcessedAt = DateTime.UtcNow;
                        outboxMsg.LockedUntil = null;
                        outboxMsg.ErrorMessage = null;
                        continue;
                    }
                }

                // If publish failed
                outboxMsg.Status = OutboxStatus.Pending;
                outboxMsg.RetryCount++;
                outboxMsg.ScheduledAt = DateTime.UtcNow.AddSeconds(Math.Min(60, Math.Pow(2, outboxMsg.RetryCount) * 2));
                outboxMsg.LockedUntil = null;
                outboxMsg.ErrorMessage = "Failed to dispatch message to broker or queue.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", outboxMsg.Id);
                outboxMsg.Status = OutboxStatus.Pending;
                outboxMsg.RetryCount++;
                outboxMsg.ScheduledAt = DateTime.UtcNow.AddSeconds(10);
                outboxMsg.LockedUntil = null;
                outboxMsg.ErrorMessage = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
