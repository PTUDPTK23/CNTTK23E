using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.AuditWorker.Data;
using EventFlow.AuditWorker.Entities;
using EventFlow.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventFlow.AuditWorker.Services;

public class AuditRabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditRabbitMqConsumer> _logger;

    public AuditRabbitMqConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AuditRabbitMqConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditRabbitMqConsumer background service is initializing...");

        var host = _configuration["RabbitMQ:Host"] ?? "localhost";
        var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
        var user = _configuration["RabbitMQ:Username"] ?? "guest";
        var pass = _configuration["RabbitMQ:Password"] ?? "guest";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("AuditWorker connecting to RabbitMQ at {Host}:{Port}...", host, port);

                var factory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = user,
                    Password = pass,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                using var connection = await factory.CreateConnectionAsync(stoppingToken);
                using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // Declare topic exchange
                await channel.ExchangeDeclareAsync(
                    exchange: RabbitMqConstants.ExchangeName,
                    type: RabbitMqConstants.ExchangeType,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Declare durable queue for audit
                await channel.QueueDeclareAsync(
                    queue: RabbitMqConstants.AuditQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Bind queue to topic exchange
                await channel.QueueBindAsync(
                    queue: RabbitMqConstants.AuditQueue,
                    exchange: RabbitMqConstants.ExchangeName,
                    routingKey: RabbitMqConstants.RoutingKeyPatternAll,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Audit queue bound to {Exchange} with routing pattern '{Pattern}'",
                    RabbitMqConstants.ExchangeName, RabbitMqConstants.RoutingKeyPatternAll);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 20, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;

                        _logger.LogInformation("AuditWorker processing message on '{RoutingKey}' (DeliveryTag: {Tag})", routingKey, ea.DeliveryTag);

                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

                        Guid eventId = Guid.Empty;
                        string eventName = string.Empty;
                        Guid? registrationId = null;
                        string? registrationCode = null;
                        Guid? participantId = null;
                        string? participantName = null;
                        string correlationId = string.Empty;
                        string eventType = string.Empty;
                        DateTime timestamp = DateTime.UtcNow;

                        if (routingKey.Equals(RabbitMqConstants.RoutingKeyRegistered, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = JsonSerializer.Deserialize<ParticipantRegisteredMessage>(json);
                            if (msg != null)
                            {
                                eventId = msg.EventId;
                                eventName = msg.EventName;
                                registrationId = msg.RegistrationId;
                                registrationCode = msg.RegistrationCode;
                                participantId = msg.ParticipantId;
                                participantName = msg.ParticipantName;
                                correlationId = msg.CorrelationId;
                                eventType = msg.EventType;
                                timestamp = msg.Timestamp;
                            }
                        }
                        else if (routingKey.Equals(RabbitMqConstants.RoutingKeyCancelled, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = JsonSerializer.Deserialize<ParticipantCancelledMessage>(json);
                            if (msg != null)
                            {
                                eventId = msg.EventId;
                                eventName = msg.EventName;
                                registrationId = msg.RegistrationId;
                                registrationCode = msg.RegistrationCode;
                                participantId = msg.ParticipantId;
                                participantName = msg.ParticipantName;
                                correlationId = msg.CorrelationId;
                                eventType = msg.EventType;
                                timestamp = msg.Timestamp;
                            }
                        }
                        else if (routingKey.Equals(RabbitMqConstants.RoutingKeyCheckedIn, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = JsonSerializer.Deserialize<ParticipantCheckedInMessage>(json);
                            if (msg != null)
                            {
                                eventId = msg.EventId;
                                eventName = msg.EventName;
                                registrationId = msg.RegistrationId;
                                registrationCode = msg.RegistrationCode;
                                participantId = msg.ParticipantId;
                                participantName = msg.ParticipantName;
                                correlationId = msg.CorrelationId;
                                eventType = msg.EventType;
                                timestamp = msg.Timestamp;
                            }
                        }
                        else
                        {
                            eventType = routingKey;
                        }

                        var auditLog = new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EventId = eventId,
                            EventName = eventName,
                            RegistrationId = registrationId,
                            RegistrationCode = registrationCode,
                            ParticipantId = participantId,
                            ParticipantName = participantName,
                            EventType = eventType,
                            Timestamp = timestamp,
                            CorrelationId = correlationId,
                            PayloadJson = json
                        };

                        dbContext.AuditLogs.Add(auditLog);
                        await dbContext.SaveChangesAsync();

                        _logger.LogInformation("AuditLog saved successfully: Event={Type}, Code={Code}, CorrelationId={Corr}",
                            eventType, registrationCode, correlationId);

                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to record audit log from RabbitMQ message");
                        await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RabbitMqConstants.AuditQueue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("AuditWorker is actively listening and recording event streams...");

                while (!stoppingToken.IsCancellationRequested && connection.IsOpen)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("RabbitMQ connection error in AuditWorker ({Msg}). Retrying in 5 seconds...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
