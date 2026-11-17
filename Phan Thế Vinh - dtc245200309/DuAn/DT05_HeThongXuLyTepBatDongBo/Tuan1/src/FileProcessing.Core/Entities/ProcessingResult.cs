namespace FileProcessing.Core.Entities;

public class ProcessingResult
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ProcessingJob Job { get; set; } = null!;
}

