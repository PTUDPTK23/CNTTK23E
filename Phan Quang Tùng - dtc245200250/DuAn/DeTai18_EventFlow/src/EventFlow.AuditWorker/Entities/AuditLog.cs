using System;
using System.ComponentModel.DataAnnotations;

namespace EventFlow.AuditWorker.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }

    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    public Guid? RegistrationId { get; set; }

    [MaxLength(50)]
    public string? RegistrationCode { get; set; }

    public Guid? ParticipantId { get; set; }

    [MaxLength(150)]
    public string? ParticipantName { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string CorrelationId { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;
}
