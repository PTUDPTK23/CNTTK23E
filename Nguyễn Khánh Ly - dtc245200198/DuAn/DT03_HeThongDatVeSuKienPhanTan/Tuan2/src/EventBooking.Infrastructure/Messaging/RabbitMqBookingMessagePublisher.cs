using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBooking.Infrastructure.Messaging;

public sealed class RabbitMqBookingMessagePublisher : IBookingMessagePublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly CreateChannelOptions PublisherChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqBookingMessagePublisher> _logger;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    private IChannel? _channel;
    private volatile bool _queueDeclared;
    private bool _disposed;

    public RabbitMqBookingMessagePublisher(
        IRabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqBookingMessagePublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BookingPublishResult> PublishBookingRequestedAsync(
        BookingRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.BookingId.ToString()
        };

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var timeoutSeconds = _options.PublishTimeoutSeconds > 0 ? _options.PublishTimeoutSeconds : 5;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var channel = await GetOrCreatePublisherChannelAsync(timeoutCts.Token);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.BookingQueueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: timeoutCts.Token);

            _logger.LogInformation(
                "Published and confirmed BookingRequestedMessage for BookingId {BookingId} to queue {QueueName}",
                message.BookingId,
                _options.BookingQueueName);

            return BookingPublishResult.Confirmed();
        }
        catch (PublishReturnException ex)
        {
            _logger.LogError(
                ex,
                "RabbitMQ returned unroutable message for BookingId {BookingId}. Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
                message.BookingId,
                ex.Exchange,
                ex.RoutingKey,
                ex.ReplyCode,
                ex.ReplyText);

            _queueDeclared = false;
            return BookingPublishResult.Failed(
                $"Message could not be routed to queue '{ex.RoutingKey}' ({ex.ReplyCode}: {ex.ReplyText}).",
                isReturned: true);
        }
        catch (PublishException ex)
        {
            _logger.LogError(
                ex,
                "RabbitMQ did not confirm message for BookingId {BookingId} (SeqNo: {SequenceNumber}, IsReturn: {IsReturn}).",
                message.BookingId,
                ex.PublishSequenceNumber,
                ex.IsReturn);

            return BookingPublishResult.Failed(
                $"RabbitMQ rejected or failed to confirm message (IsReturn={ex.IsReturn}): {ex.Message}",
                isReturned: ex.IsReturn);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Timed out waiting for RabbitMQ publisher confirmation for BookingId {BookingId}.",
                message.BookingId);

            await ResetChannelAsync();
            return BookingPublishResult.Failed("Timed out waiting for RabbitMQ publisher confirmation.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to publish BookingRequestedMessage for BookingId {BookingId} to RabbitMQ.",
                message.BookingId);

            await ResetChannelAsync();
            return BookingPublishResult.Failed($"Failed to publish message to RabbitMQ: {ex.Message}");
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task<IChannel> GetOrCreatePublisherChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is not { IsOpen: true })
        {
            await ResetChannelAsync();

            _channel = await _connectionProvider.CreateChannelAsync(PublisherChannelOptions, cancellationToken);
            _channel.BasicReturnAsync += OnBasicReturnAsync;
            _channel.ChannelShutdownAsync += OnChannelShutdownAsync;
            _queueDeclared = false;
        }

        if (!_queueDeclared)
        {
            await _connectionProvider.EnsureBookingQueueDeclaredAsync(_channel, cancellationToken);
            _queueDeclared = true;
        }

        return _channel;
    }

    private Task OnBasicReturnAsync(object sender, BasicReturnEventArgs args)
    {
        _logger.LogWarning(
            "RabbitMQ BasicReturn received. Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
            args.Exchange,
            args.RoutingKey,
            args.ReplyCode,
            args.ReplyText);

        return Task.CompletedTask;
    }

    private Task OnChannelShutdownAsync(object sender, ShutdownEventArgs args)
    {
        _logger.LogWarning(
            "RabbitMQ publisher channel closed. ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
            args.ReplyCode,
            args.ReplyText);

        _queueDeclared = false;
        return Task.CompletedTask;
    }

    private async Task ResetChannelAsync()
    {
        if (_channel is not null)
        {
            _channel.BasicReturnAsync -= OnBasicReturnAsync;
            _channel.ChannelShutdownAsync -= OnChannelShutdownAsync;

            try
            {
                await _channel.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ignored error while disposing RabbitMQ publisher channel.");
            }

            _channel = null;
        }

        _queueDeclared = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _publishLock.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await ResetChannelAsync();
        }
        finally
        {
            _publishLock.Release();
            _publishLock.Dispose();
        }
    }
}
