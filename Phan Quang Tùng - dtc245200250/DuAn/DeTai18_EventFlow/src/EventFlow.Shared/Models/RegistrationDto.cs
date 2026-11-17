using System;
using System.ComponentModel.DataAnnotations;

namespace EventFlow.Shared.Models;

public enum RegistrationStatus
{
    Confirmed = 0,
    CheckedIn = 1,
    Cancelled = 2
}

public class RegistrationDto
{
    public Guid Id { get; set; }
    public string RegistrationCode { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string ParticipantPhone { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class RegistrationDetailDto : RegistrationDto
{
    public string EventLocation { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public string QrCodeData => RegistrationCode;
}

public class CheckInRequest
{
    [Required(ErrorMessage = "Mã đăng ký (Registration Code) không được để trống")]
    public string RegistrationCode { get; set; } = string.Empty;
}

public class CheckInResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RegistrationCode { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
    public RegistrationStatus Status { get; set; }
}
