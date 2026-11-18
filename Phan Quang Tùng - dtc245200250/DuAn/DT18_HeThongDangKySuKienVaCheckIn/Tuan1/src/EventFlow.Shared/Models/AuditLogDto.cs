using System;

namespace EventFlow.Shared.Models;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid? RegistrationId { get; set; }
    public string? RegistrationCode { get; set; }
    public Guid? ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
