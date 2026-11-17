using System.Diagnostics;
using System.Text.Json;
using FileProcessing.Core.Enums;
using FileProcessing.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace FileProcessing.Infrastructure.Processing;

public class ImageSharpProcessor : IImageProcessor
{
    public async Task<ImageProcessingResult> ProcessAsync(Stream inputStream, JobType jobType, string? parametersJson, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        using var image = await Image.LoadAsync(inputStream, cancellationToken);
        var outputStream = new MemoryStream();

        string outputExtension;
        string contentType;

        switch (jobType)
        {
            case JobType.Thumbnail:
                int targetWidth = 200;
                int targetHeight = 200;
                if (!string.IsNullOrWhiteSpace(parametersJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(parametersJson);
                        if (doc.RootElement.TryGetProperty("width", out var w)) targetWidth = w.GetInt32();
                        if (doc.RootElement.TryGetProperty("height", out var h)) targetHeight = h.GetInt32();
                    }
                    catch
                    {
                        // Use default 200x200 on parse error
                    }
                }

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(targetWidth, targetHeight),
                    Mode = ResizeMode.Max
                }));

                // Keep same format or default to jpeg
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 }, cancellationToken);
                outputExtension = ".jpg";
                contentType = "image/jpeg";
                break;

            case JobType.ConvertPngToJpeg:
                int quality = 85;
                if (!string.IsNullOrWhiteSpace(parametersJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(parametersJson);
                        if (doc.RootElement.TryGetProperty("quality", out var q)) quality = q.GetInt32();
                    }
                    catch { }
                }

                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = quality }, cancellationToken);
                outputExtension = ".jpg";
                contentType = "image/jpeg";
                break;

            case JobType.Compress:
            default:
                int compressQuality = 65;
                if (!string.IsNullOrWhiteSpace(parametersJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(parametersJson);
                        if (doc.RootElement.TryGetProperty("quality", out var q)) compressQuality = q.GetInt32();
                    }
                    catch { }
                }

                // If original format is png and user wanted png compression, or compress as jpeg
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = compressQuality }, cancellationToken);
                outputExtension = ".jpg";
                contentType = "image/jpeg";
                break;
        }

        sw.Stop();
        var data = outputStream.ToArray();

        return new ImageProcessingResult
        {
            Data = data,
            Width = image.Width,
            Height = image.Height,
            FileSize = data.Length,
            DurationMs = sw.ElapsedMilliseconds,
            FileExtension = outputExtension,
            ContentType = contentType
        };
    }
}
