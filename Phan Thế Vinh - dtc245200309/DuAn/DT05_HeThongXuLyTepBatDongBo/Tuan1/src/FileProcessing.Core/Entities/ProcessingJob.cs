using FileProcessing.Core.Enums;

namespace FileProcessing.Core.Entities;

public class ProcessingJob
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public JobType JobType { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? ParametersJson { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextAttemptAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public string? WorkerId { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public UploadedFile File { get; set; } = null!;
    public ProcessingResult? Result { get; set; }
}

