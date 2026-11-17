namespace FileProcessing.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveUploadFileAsync(Stream inputStream, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadStreamAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<string> SaveResultFileAsync(Stream inputStream, string fileName, CancellationToken cancellationToken = default);
    Task<string> SaveTempFileAsync(Stream inputStream, string fileName, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    string GetPhysicalPath(string relativePath);
    bool FileExists(string relativePath);
}
