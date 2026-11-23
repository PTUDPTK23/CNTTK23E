using System.Text.Json;
using Library.Shared;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Library.Api;

public sealed class ReservationPublisher(IOptions<RabbitMqSettings> options, ILogger<ReservationPublisher> log)
    : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(ReservationRequested message, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var channel = await EnsureChannelAsync(ct);
            var props = new BasicProperties
            {
                Persistent = true,                                   // message được ghi xuống đĩa
                ContentType = "application/json",
                CorrelationId = message.ReservationId.ToString()
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            await channel.BasicPublishAsync("", RabbitMqTopology.RequestQueue,
                mandatory: true, basicProperties: props, body: body, cancellationToken: ct);

            log.LogInformation("Published reservation {ReservationId} to {Queue}",
                message.ReservationId, RabbitMqTopology.RequestQueue);
        }
        finally { _gate.Release(); }
    }

    // Kết nối lười: lần đầu hoặc khi kênh đã đóng (RabbitMQ restart) thì tự kết nối lại
    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        if (_connection is not null) await _connection.DisposeAsync();
        var s = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = s.Host, Port = s.Port, UserName = s.User, Password = s.Password
        };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true,
                                     publisherConfirmationTrackingEnabled: true), ct);
        await RabbitMqTopology.DeclareAsync(_channel, ct);
        log.LogInformation("Connected to RabbitMQ at {Host}:{Port}", s.Host, s.Port);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
