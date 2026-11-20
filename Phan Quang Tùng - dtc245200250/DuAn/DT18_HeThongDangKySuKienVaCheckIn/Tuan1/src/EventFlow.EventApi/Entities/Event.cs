using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EventFlow.Shared.Models;

namespace EventFlow.EventApi.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int MaxCapacity { get; set; }
    public int RegisteredCount { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Upcoming;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ConcurrencyCheck]
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
