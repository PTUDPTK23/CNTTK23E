using System.Text;
using System.Text.Json;
using FileProcessing.Core.Contracts;
using FileProcessing.Core.Entities;
using FileProcessing.Core.Enums;
using FileProcessing.Core.Interfaces;
using FileProcessing.Infrastructure.Messaging;
using FileProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace FileProcessing.Infrastructure.Workers;

public class ImageProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJobQueueChannel _fallbackQueue;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly ILogger<ImageProcessingWorker> _logger;
    private readonly string _workerId;

    public ImageProcessingWorker(
        IServiceProvider serviceProvider,
        IJobQueueChannel fallbackQueue,
        IOptions<RabbitMqOptions> rabbitOptions,
        ILogger<ImageProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _fallbackQueue = fallbackQueue;
        _rabbitOptions = rabbitOptions.Value;
        _logger = logger;
        _workerId = $"{Environment.MachineName}_{Environment.ProcessId}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImageProcessingWorker started with WorkerId: {WorkerId}", _workerId);

        // Start RabbitMQ consumer task
        var rabbitConsumerTask = RunRabbitMqConsumerAsync(stoppingToken);

        // Start Fallback queue consumer task
        var fallbackConsumerTask = RunFallbackQueueConsumerAsync(stoppingToken);

        // Start Periodic Stale Job Recovery task
        var recoveryTask = RunStaleJobRecoveryAsync(stoppingToken);

        await Task.WhenAll(rabbitConsumerTask, fallbackConsumerTask, recoveryTask);
    }

    private async Task RunRabbitMqConsumerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitOptions.HostName,
                    Port = _rabbitOptions.Port,
                    UserName = _rabbitOptions.UserName,
                    Password = _rabbitOptions.Password,
                    VirtualHost = _rabbitOptions.VirtualHost,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
                };

                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 5, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonSerializer.Deserialize<ProcessImageJobMessage>(json);

                        if (message != null)
                        {
                            var success = await ProcessJobAsync(message, stoppingToken);
                            if (success)
                            {
                                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                                return;
                            }
                        }

                        // Failed or unclaimable
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling RabbitMQ delivery tag {Tag}", ea.DeliveryTag);
                        try
                        {
                            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                        }
                        catch { }
                    }
                };

                await channel.BasicConsumeAsync(queue: _rabbitOptions.MainQueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                _logger.LogInformation("Subscribed to RabbitMQ queue: {Queue}", _rabbitOptions.MainQueueName);

                // Keep alive until cancelled
                while (!stoppingToken.IsCancellationRequested && channel.IsOpen)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is BrokerUnreachableException or System.Net.Sockets.SocketException or TimeoutException)
            {
                _logger.LogDebug("RabbitMQ consumer not connected. Retrying in 10s...");
                await Task.Delay(10000, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "RabbitMQ consumer error. Reconnecting in 10s...");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task RunFallbackQueueConsumerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _fallbackQueue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job from internal queue channel.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task RunStaleJobRecoveryAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FileProcessingDbContext>();
                var now = DateTime.UtcNow;

                // Stale jobs: Processing with expired Lease LockedUntil
                var staleJobs = await dbContext.ProcessingJobs
                    .Where(j => j.Status == JobStatus.Processing && j.LockedUntil != null && j.LockedUntil < now)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var job in staleJobs)
                {
                    _logger.LogWarning("Reclaiming stale job {JobId} whose lease expired at {LockedUntil}", job.Id, job.LockedUntil);
                    job.Status = JobStatus.Pending;
                    job.LeaseToken = null;
                    job.WorkerId = null;
                    job.LockedUntil = null;
                    job.NextAttemptAt = now;
                }

                if (staleJobs.Count > 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in stale job recovery loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task<bool> ProcessJobAsync(ProcessImageJobMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FileProcessingDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageProcessor>();

        var now = DateTime.UtcNow;

        // 1. Claim Job atomically
        var job = await dbContext.ProcessingJobs
            .Include(j => j.File)
            .FirstOrDefaultAsync(j => j.Id == message.JobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found in database.", message.JobId);
            return false;
        }

        // Check if already completed or failed
        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
        {
            _logger.LogInformation("Job {JobId} is already {Status}. Skipping.", job.Id, job.Status);
            return true;
        }

        // If processing with active lease, another worker owns it
        if (job.Status == JobStatus.Processing && job.LockedUntil > now)
        {
            _logger.LogInformation("Job {JobId} currently locked by worker {WorkerId} until {Until}.", job.Id, job.WorkerId, job.LockedUntil);
            return false;
        }

        // Claim
        var leaseToken = Guid.NewGuid();
        job.Status = JobStatus.Processing;
        job.LeaseToken = leaseToken;
        job.WorkerId = _workerId;
        job.LockedUntil = now.AddMinutes(2);
        job.StartedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Claimed Job {JobId} with LeaseToken {LeaseToken}.", job.Id, leaseToken);

        try
        {
            // 2. Open input file
            var file = job.File;
            if (file == null)
            {
                throw new InvalidOperationException($"Uploaded file {job.FileId} not found for Job {job.Id}");
            }

            using var inputStream = await storage.OpenReadStreamAsync(file.RelativePath, cancellationToken);

            // 3. Process image
            var result = await imageProcessor.ProcessAsync(inputStream, job.JobType, job.ParametersJson, cancellationToken);

            // 4. Save result file
            var resultFileName = $"{job.Id:N}{result.FileExtension}";
            using var resultDataStream = new MemoryStream(result.Data);
            var resultPath = await storage.SaveResultFileAsync(resultDataStream, resultFileName, cancellationToken);

            // 5. Commit result & Complete job
            var jobResult = new ProcessingResult
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                RelativePath = resultPath,
                FileSize = result.FileSize,
                Width = result.Width,
                Height = result.Height,
                DurationMs = result.DurationMs,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.ProcessingResults.Add(jobResult);

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = null;
            job.LockedUntil = null;

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully completed Job {JobId} in {Duration}ms. Result saved at {Path}", job.Id, result.DurationMs, resultPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed executing Job {JobId}", job.Id);

            job.RetryCount++;
            if (job.RetryCount >= job.MaxRetries)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                job.LockedUntil = null;
            }
            else
            {
                job.Status = JobStatus.Pending;
                job.ErrorMessage = ex.Message;
                job.NextAttemptAt = DateTime.UtcNow.AddSeconds(5 * job.RetryCount);
                job.LockedUntil = null;

                // Add Outbox retry message
                var retryMsg = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "RetryImageJob",
                    IdempotencyKey = $"RETRY_{job.Id}_{job.RetryCount}",
                    Payload = JsonSerializer.Serialize(new ProcessImageJobMessage
                    {
                        JobId = job.Id,
                        FileId = job.FileId,
                        JobType = job.JobType,
                        ParametersJson = job.ParametersJson,
                        RetryCount = job.RetryCount
                    }),
                    Status = OutboxStatus.Pending,
                    ScheduledAt = job.NextAttemptAt,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.OutboxMessages.Add(retryMsg);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }
    }
}

