using System;
using System.ComponentModel.DataAnnotations;
using EventFlow.Shared.Models;

namespace EventFlow.EventApi.Entities;

public class Registration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string RegistrationCode { get; set; } = string.Empty;

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
