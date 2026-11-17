using FileProcessing.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FileProcessing.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _uploadsPath;
    private readonly string _tempPath;
    private readonly string _resultsPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:BasePath"] ?? "storage";
        _basePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(Directory.GetCurrentDirectory(), configuredPath);

        _uploadsPath = Path.Combine(_basePath, "uploads");
        _tempPath = Path.Combine(_basePath, "temp");
        _resultsPath = Path.Combine(_basePath, "results");

        Directory.CreateDirectory(_uploadsPath);
        Directory.CreateDirectory(_tempPath);
        Directory.CreateDirectory(_resultsPath);
    }

    public async Task<string> SaveUploadFileAsync(Stream inputStream, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            extension = contentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
        }

        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine("uploads", uniqueName).Replace('\\', '/');
        var physicalPath = Path.Combine(_uploadsPath, uniqueName);

        using (var outputStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            await inputStream.CopyToAsync(outputStream, cancellationToken);
        }

        return relativePath;
    }

    public async Task<Stream> OpenReadStreamAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        if (!File.Exists(physicalPath))
        {
            throw new FileNotFoundException($"Storage file not found at: {relativePath}");
        }

        var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
        {
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
        }
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<string> SaveResultFileAsync(Stream inputStream, string fileName, CancellationToken cancellationToken = default)
    {
        var relativePath = Path.Combine("results", fileName).Replace('\\', '/');
        var physicalPath = Path.Combine(_resultsPath, fileName);

        using (var outputStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            await inputStream.CopyToAsync(outputStream, cancellationToken);
        }

        return relativePath;
    }

    public async Task<string> SaveTempFileAsync(Stream inputStream, string fileName, CancellationToken cancellationToken = default)
    {
        var relativePath = Path.Combine("temp", fileName).Replace('\\', '/');
        var physicalPath = Path.Combine(_tempPath, fileName);

        using (var outputStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            await inputStream.CopyToAsync(outputStream, cancellationToken);
        }

        return relativePath;
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        if (File.Exists(physicalPath))
        {
            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
                // Ignore transient delete issues during cleanup
            }
        }
        return Task.CompletedTask;
    }

    public string GetPhysicalPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(_basePath, normalized);
    }

    public bool FileExists(string relativePath)
    {
        return File.Exists(GetPhysicalPath(relativePath));
    }
}
