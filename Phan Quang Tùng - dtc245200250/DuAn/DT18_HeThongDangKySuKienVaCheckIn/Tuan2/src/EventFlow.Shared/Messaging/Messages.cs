using System;

namespace EventFlow.Shared.Messaging;

public class ParticipantRegisteredMessage : IEventMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid RegistrationId { get; set; }
    public string RegistrationCode { get; set; } = string.Empty;
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int RemainingCapacity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "ParticipantRegistered";
}

public class ParticipantCancelledMessage : IEventMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid RegistrationId { get; set; }
    public string RegistrationCode { get; set; } = string.Empty;
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int RemainingCapacity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "ParticipantCancelled";
}

public class ParticipantCheckedInMessage : IEventMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid RegistrationId { get; set; }
    public string RegistrationCode { get; set; } = string.Empty;
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "ParticipantCheckedIn";
}
