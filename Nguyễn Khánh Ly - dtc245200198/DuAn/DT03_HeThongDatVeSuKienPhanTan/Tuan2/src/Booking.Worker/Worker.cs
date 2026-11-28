using System.Net.Sockets;
using System.Text.Json;
using Booking.Worker.Services;
using EventBooking.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Booking.Worker;

public sealed class Worker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RequeueDelay = TimeSpan.FromSeconds(2);

    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IRabbitMqConnectionProvider connectionProvider,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<Worker> logger)
    {
        _connectionProvider = connectionProvider;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IChannel? channel = null;
            string? consumerTag = null;
            var channelFaultedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            void MarkChannelFaulted() => channelFaultedTcs.TrySetResult();

            Task OnChannelShutdownAsync(object sender, ShutdownEventArgs args)
            {
                _logger.LogWarning(
                    "RabbitMQ consumer channel closed. ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
                    args.ReplyCode,
                    args.ReplyText);

                MarkChannelFaulted();
                return Task.CompletedTask;
            }

            try
            {
                channel = await _connectionProvider.CreateChannelAsync(cancellationToken: stoppingToken);
                await _connectionProvider.EnsureBookingQueueDeclaredAsync(channel, stoppingToken);
                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                channel.ChannelShutdownAsync += OnChannelShutdownAsync;

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, ea) =>
                    HandleDeliveryAsync(channel, ea, MarkChannelFaulted, stoppingToken);

                consumerTag = await channel.BasicConsumeAsync(
                    queue: _options.BookingQueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "Booking.Worker is listening on queue '{QueueName}' (Host: {HostName}:{Port}).",
                    _options.BookingQueueName,
                    _options.HostName,
                    _options.Port);

                await Task.WhenAny(channelFaultedTcs.Task, Task.Delay(Timeout.Infinite, stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in Booking.Worker RabbitMQ consumer loop. Retrying in {DelaySeconds}s...",
                    ReconnectDelay.TotalSeconds);
            }
            finally
            {
                if (channel is not null)
                {
                    channel.ChannelShutdownAsync -= OnChannelShutdownAsync;

                    if (!string.IsNullOrWhiteSpace(consumerTag) && channel.IsOpen)
                    {
                        try
                        {
                            await channel.BasicCancelAsync(consumerTag, cancellationToken: CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Ignored error while cancelling RabbitMQ consumer.");
                        }
                    }

                    try
                    {
                        await channel.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Ignored error while disposing RabbitMQ consumer channel.");
                    }
                }
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        Action markChannelFaulted,
        CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested || !channel.IsOpen)
        {
            _logger.LogInformation(
                "Worker is stopping or channel is closed. Leaving DeliveryTag {DeliveryTag} unacknowledged for redelivery.",
                ea.DeliveryTag);
            return;
        }

        BookingRequestedMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<BookingRequestedMessage>(ea.Body.Span, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid JSON payload at DeliveryTag {DeliveryTag} (Redelivered: {Redelivered}). Message will not be requeued.",
                ea.DeliveryTag,
                ea.Redelivered);

            await SafeNackAsync(channel, ea.DeliveryTag, requeue: false, markChannelFaulted, stoppingToken);
            return;
        }

        if (message is null || message.BookingId <= 0)
        {
            _logger.LogError(
                "Invalid BookingRequestedMessage at DeliveryTag {DeliveryTag} (BookingId={BookingId}). Message will not be requeued.",
                ea.DeliveryTag,
                message?.BookingId);

            await SafeNackAsync(channel, ea.DeliveryTag, requeue: false, markChannelFaulted, stoppingToken);
            return;
        }

        // Tách riêng phần xử lý nghiệp vụ với phần ACK/NACK trên RabbitMQ
        BookingProcessDecision decision;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBookingProcessor>();
            decision = await processor.ProcessAsync(message, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Worker shutdown requested while processing Booking #{BookingId} (DeliveryTag {DeliveryTag}). Leaving message unacknowledged for redelivery.",
                message.BookingId,
                ea.DeliveryTag);
            return;
        }
        catch (Exception ex) when (IsTransientInfrastructureException(ex))
        {
            _logger.LogError(
                ex,
                "Transient infrastructure/DI error while preparing to process Booking #{BookingId} (DeliveryTag {DeliveryTag}). Scheduling requeue.",
                message.BookingId,
                ea.DeliveryTag);

            decision = BookingProcessDecision.RequeueWithDelay;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Non-transient DI or application error before settlement for Booking #{BookingId} (DeliveryTag {DeliveryTag}). Rejecting without requeue.",
                message.BookingId,
                ea.DeliveryTag);

            decision = BookingProcessDecision.RejectWithoutRequeue;
        }

        // Chỉ ACK/NACK một lần duy nhất; lỗi RabbitMQ ở bước này không làm xử lý lại hay NACK chồng
        switch (decision)
        {
            case BookingProcessDecision.Acknowledge:
                await SafeAckAsync(channel, ea.DeliveryTag, markChannelFaulted, stoppingToken);
                break;

            case BookingProcessDecision.RejectWithoutRequeue:
                await SafeNackAsync(channel, ea.DeliveryTag, requeue: false, markChannelFaulted, stoppingToken);
                break;

            case BookingProcessDecision.RequeueWithDelay:
                try
                {
                    await Task.Delay(RequeueDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Worker shutdown requested before requeue NACK for DeliveryTag {DeliveryTag}. Leaving message unacknowledged for redelivery.",
                        ea.DeliveryTag);
                    return;
                }

                await SafeNackAsync(channel, ea.DeliveryTag, requeue: true, markChannelFaulted, stoppingToken);
                break;
        }
    }

    private Task SafeAckAsync(
        IChannel channel,
        ulong deliveryTag,
        Action markChannelFaulted,
        CancellationToken stoppingToken) =>
        TrySettleDeliveryAsync(
            channel,
            deliveryTag,
            operationName: "ACK",
            settleAction: () => channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken),
            markChannelFaulted,
            stoppingToken);

    private Task SafeNackAsync(
        IChannel channel,
        ulong deliveryTag,
        bool requeue,
        Action markChannelFaulted,
        CancellationToken stoppingToken) =>
        TrySettleDeliveryAsync(
            channel,
            deliveryTag,
            operationName: $"NACK (requeue={requeue})",
            settleAction: () => channel.BasicNackAsync(
                deliveryTag,
                multiple: false,
                requeue: requeue,
                cancellationToken: stoppingToken),
            markChannelFaulted,
            stoppingToken);

    private async Task TrySettleDeliveryAsync(
        IChannel channel,
        ulong deliveryTag,
        string operationName,
        Func<ValueTask> settleAction,
        Action markChannelFaulted,
        CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Skipped {Operation} for DeliveryTag {DeliveryTag} because worker is stopping. RabbitMQ will redeliver.",
                operationName,
                deliveryTag);
            return;
        }

        if (!channel.IsOpen)
        {
            _logger.LogWarning(
                "Skipped {Operation} for DeliveryTag {DeliveryTag} because RabbitMQ channel is closed. Marking channel faulted for reconnect.",
                operationName,
                deliveryTag);
            markChannelFaulted();
            return;
        }

        try
        {
            await settleAction();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Shutdown cancelled {Operation} for DeliveryTag {DeliveryTag}. RabbitMQ will redeliver.",
                operationName,
                deliveryTag);
        }
        catch (Exception ex) when (IsRabbitMqCommunicationException(ex))
        {
            // Không NACK lại deliveryTag này khi ACK/NACK lỗi; đánh dấu channel lỗi để đóng và reconnect, để RabbitMQ tự redeliver nếu chưa nhận được ACK
            _logger.LogError(
                ex,
                "RabbitMQ communication error during {Operation} for DeliveryTag {DeliveryTag}. Marking channel faulted and stopping use of this channel.",
                operationName,
                deliveryTag);
            markChannelFaulted();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during {Operation} for DeliveryTag {DeliveryTag}. Marking channel faulted and stopping use of this channel without retrying settlement.",
                operationName,
                deliveryTag);
            markChannelFaulted();
        }
    }

    private static bool IsTransientInfrastructureException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException { IsTransient: true }
                or TimeoutException
                or SocketException
                or IOException)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRabbitMqCommunicationException(Exception ex) =>
        ex is AlreadyClosedException
            or OperationInterruptedException
            or RabbitMQClientException
            or IOException
            or SocketException
            or ObjectDisposedException
            or OperationCanceledException;
}
