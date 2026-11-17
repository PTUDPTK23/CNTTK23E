using FileProcessing.Core.Enums;

namespace FileProcessing.Core.Interfaces;

public class ImageProcessingResult
{
    public required byte[] Data { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FileSize { get; init; }
    public required long DurationMs { get; init; }
    public required string FileExtension { get; init; }
    public required string ContentType { get; init; }
}

public interface IImageProcessor
{
    Task<ImageProcessingResult> ProcessAsync(Stream inputStream, JobType jobType, string? parametersJson, CancellationToken cancellationToken = default);
}
