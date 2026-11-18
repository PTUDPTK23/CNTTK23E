namespace EventFlow.Shared.Messaging;

public static class RabbitMqConstants
{
    public const string ExchangeName = "eventflow.events.exchange";
    public const string ExchangeType = "topic";

    // Routing Keys
    public const string RoutingKeyRegistered = "event.participant.registered";
    public const string RoutingKeyCancelled = "event.participant.cancelled";
    public const string RoutingKeyCheckedIn = "event.participant.checkedin";
    public const string RoutingKeyPatternAll = "event.participant.*";

    // Queues
    public const string DashboardQueue = "eventflow.dashboard.queue";
    public const string AuditQueue = "eventflow.audit.queue";
}
