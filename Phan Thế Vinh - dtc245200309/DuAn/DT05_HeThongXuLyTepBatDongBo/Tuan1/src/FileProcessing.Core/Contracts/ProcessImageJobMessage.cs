using FileProcessing.Core.Enums;

namespace FileProcessing.Core.Contracts;

public class ProcessImageJobMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid FileId { get; set; }
    public JobType JobType { get; set; }
    public string? ParametersJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
}
