using System;
using System.ComponentModel.DataAnnotations;

namespace EventFlow.Shared.Models;

public enum EventStatus
{
    Upcoming = 0,
    Ongoing = 1,
    Completed = 2,
    Cancelled = 3
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public int RegisteredCount { get; set; }
    public int RemainingSpots => Math.Max(0, MaxCapacity - RegisteredCount);
    public EventStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEventRequest
{
    [Required(ErrorMessage = "Tên sự kiện không được để trống")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên sự kiện phải từ 3 đến 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả sự kiện không được để trống")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa điểm không được để trống")]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required]
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddDays(7).AddHours(4);

    [Range(1, 100000, ErrorMessage = "Sức chứa tối đa phải từ 1 người trở lên")]
    public int MaxCapacity { get; set; } = 100;
}

public class UpdateEventRequest
{
    [Required(ErrorMessage = "Tên sự kiện không được để trống")]
    [StringLength(200, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả sự kiện không được để trống")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa điểm không được để trống")]
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Range(1, 100000)]
    public int MaxCapacity { get; set; }

    public EventStatus Status { get; set; }
}
