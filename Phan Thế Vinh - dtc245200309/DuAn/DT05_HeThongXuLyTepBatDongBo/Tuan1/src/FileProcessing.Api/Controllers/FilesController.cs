using FileProcessing.Core.Entities;
using FileProcessing.Core.Enums;
using FileProcessing.Core.Interfaces;
using FileProcessing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileProcessing.Api.Controllers;

[ApiController]
public class FilesController : ControllerBase
{
    private readonly FileProcessingDbContext _dbContext;
    private readonly IFileStorageService _storageService;
    private readonly ILogger<FilesController> _logger;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/pjpeg"
    };

    private const long MaxFileSizeInBytes = 25 * 1024 * 1024; // 25 MB

    public FilesController(
        FileProcessingDbContext dbContext,
        IFileStorageService storageService,
        ILogger<FilesController> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload tệp ảnh PNG/JPEG cần xử lý
    /// </summary>
    [HttpPost("files")]
    [HttpPost("api/files")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Tệp tải lên không được để trống." });
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            return BadRequest(new { error = $"Kích thước tệp vượt quá giới hạn cho phép (tối đa {MaxFileSizeInBytes / (1024 * 1024)} MB)." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { error = $"Định dạng tệp không được hỗ trợ. Chỉ chấp nhận các định dạng: {string.Join(", ", AllowedExtensions)}" });
        }

        var contentType = file.ContentType;
        if (string.IsNullOrEmpty(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            contentType = extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        }

        // Lưu tệp vật lý vào File Storage
        string relativePath;
        using (var stream = file.OpenReadStream())
        {
            relativePath = await _storageService.SaveUploadFileAsync(stream, file.FileName, contentType, cancellationToken);
        }

        var storedFileName = Path.GetFileName(relativePath);

        // Lưu metadata vào SQL Server
        var uploadedFile = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            RelativePath = relativePath,
            ContentType = contentType,
            FileSize = file.Length,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UploadedFiles.Add(uploadedFile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tệp {FileId} ({OriginalFileName}) đã được lưu thành công tại {RelativePath}",
            uploadedFile.Id, uploadedFile.OriginalFileName, relativePath);

        return CreatedAtAction(
            nameof(GetFileMetadata),
            new { id = uploadedFile.Id },
            new
            {
                fileId = uploadedFile.Id,
                originalFileName = uploadedFile.OriginalFileName,
                storedFileName = uploadedFile.StoredFileName,
                contentType = uploadedFile.ContentType,
                fileSize = uploadedFile.FileSize,
                createdAt = uploadedFile.CreatedAt,
                downloadRawUrl = $"/files/{uploadedFile.Id}/raw"
            });
    }

    /// <summary>
    /// Tra cứu thông tin metadata của tệp đã upload
    /// </summary>
    [HttpGet("files/{id:guid}")]
    [HttpGet("api/files/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileMetadata(Guid id, CancellationToken cancellationToken)
    {
        var file = await _dbContext.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (file == null)
        {
            return NotFound(new { error = $"Không tìm thấy tệp với mã {id}" });
        }

        return Ok(new
        {
            fileId = file.Id,
            originalFileName = file.OriginalFileName,
            storedFileName = file.StoredFileName,
            contentType = file.ContentType,
            fileSize = file.FileSize,
            createdAt = file.CreatedAt,
            downloadRawUrl = $"/files/{file.Id}/raw"
        });
    }

    /// <summary>
    /// Tải tệp kết quả xử lý.
    /// Hỗ trợ truyền {id} là JobId, FileId hoặc ResultId.
    /// </summary>
    [HttpGet("files/{id:guid}/result")]
    [HttpGet("api/files/{id:guid}/result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileResult(Guid id, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra nếu id là JobId
        var job = await _dbContext.ProcessingJobs
            .Include(j => j.Result)
            .Include(j => j.File)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        // 2. Nếu không tìm thấy theo JobId, kiểm tra nếu id là FileId (lấy Job mới nhất của File đó)
        if (job == null)
        {
            job = await _dbContext.ProcessingJobs
                .Include(j => j.Result)
                .Include(j => j.File)
                .Where(j => j.FileId == id)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // 3. Nếu vẫn không thấy, kiểm tra nếu id trực tiếp là ResultId
        if (job == null)
        {
            var directResult = await _dbContext.ProcessingResults
                .Include(r => r.Job)
                .ThenInclude(j => j.File)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (directResult != null)
            {
                job = directResult.Job;
            }
        }

        if (job == null)
        {
            return NotFound(new { error = $"Không tìm thấy Job hoặc Tệp với mã {id}." });
        }

        // Kiểm tra trạng thái xử lý
        if (job.Status == JobStatus.Pending || job.Status == JobStatus.Processing)
        {
            return Accepted(new
            {
                message = "Tệp đang trong quá trình xử lý, kết quả chưa sẵn sàng.",
                jobId = job.Id,
                status = job.Status.ToString()
            });
        }

        if (job.Status == JobStatus.Failed)
        {
            return BadRequest(new
            {
                error = "Quá trình xử lý tệp đã thất bại.",
                jobId = job.Id,
                status = job.Status.ToString(),
                errorMessage = job.ErrorMessage
            });
        }

        if (job.Result == null)
        {
            return NotFound(new { error = $"Không tìm thấy dữ liệu kết quả cho Job {job.Id}." });
        }

        if (!_storageService.FileExists(job.Result.RelativePath))
        {
            return NotFound(new { error = "Tệp kết quả vật lý không tồn tại trên hệ thống lưu trữ." });
        }

        var stream = await _storageService.OpenReadStreamAsync(job.Result.RelativePath, cancellationToken);
        var extension = Path.GetExtension(job.Result.RelativePath).ToLowerInvariant();
        var contentType = extension == ".png" ? "image/png" : "image/jpeg";

        var downloadFileName = $"processed_{Path.GetFileNameWithoutExtension(job.File?.OriginalFileName ?? "result")}{extension}";

        return File(stream, contentType, downloadFileName);
    }

    /// <summary>
    /// Tải tệp gốc đã upload
    /// </summary>
    [HttpGet("files/{id:guid}/raw")]
    [HttpGet("api/files/{id:guid}/raw")]
    public async Task<IActionResult> GetFileRaw(Guid id, CancellationToken cancellationToken)
    {
        var file = await _dbContext.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (file == null)
        {
            return NotFound(new { error = $"Không tìm thấy tệp với mã {id}" });
        }

        if (!_storageService.FileExists(file.RelativePath))
        {
            return NotFound(new { error = "Tệp vật lý không tồn tại trên hệ thống lưu trữ." });
        }

        var stream = await _storageService.OpenReadStreamAsync(file.RelativePath, cancellationToken);
        return File(stream, file.ContentType, file.OriginalFileName);
    }
}

