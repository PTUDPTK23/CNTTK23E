using System.Text.Json;
using FileProcessing.Core.Contracts;
using FileProcessing.Core.Entities;
using FileProcessing.Core.Enums;
using FileProcessing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileProcessing.Api.Controllers;

public class CreateJobRequest
{
    public required Guid FileId { get; set; }
    public string JobType { get; set; } = "Compress";
    public object? Parameters { get; set; }
}

[ApiController]
public class JobsController : ControllerBase
{
    private readonly FileProcessingDbContext _dbContext;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        FileProcessingDbContext dbContext,
        ILogger<JobsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Tạo yêu cầu xử lý tệp bất đồng bộ (Job)
    /// </summary>
    [HttpPost("jobs")]
    [HttpPost("api/jobs")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        if (request == null || request.FileId == Guid.Empty)
        {
            return BadRequest(new { error = "FileId không hợp lệ hoặc để trống." });
        }

        // Kiểm tra tệp nguồn tồn tại
        var fileExists = await _dbContext.UploadedFiles
            .AnyAsync(f => f.Id == request.FileId, cancellationToken);

        if (!fileExists)
        {
            return NotFound(new { error = $"Không tìm thấy tệp đã tải lên với mã {request.FileId}." });
        }

        // Parse JobType
        if (!Enum.TryParse<JobType>(request.JobType, ignoreCase: true, out var jobType))
        {
            return BadRequest(new
            {
                error = $"Loại tác vụ (JobType) '{request.JobType}' không hợp lệ. Các loại hỗ trợ: {string.Join(", ", Enum.GetNames<JobType>())}"
            });
        }

        var parametersJson = request.Parameters != null
            ? JsonSerializer.Serialize(request.Parameters)
            : null;

        var jobId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var job = new ProcessingJob
        {
            Id = jobId,
            FileId = request.FileId,
            JobType = jobType,
            Status = JobStatus.Pending,
            ParametersJson = parametersJson,
            RetryCount = 0,
            MaxRetries = 3,
            CreatedAt = now
        };

        var messageContract = new ProcessImageJobMessage
        {
            MessageId = Guid.NewGuid(),
            JobId = jobId,
            FileId = request.FileId,
            JobType = jobType,
            ParametersJson = parametersJson,
            CreatedAt = now,
            RetryCount = 0
        };

        var outboxMessage = new OutboxMessage
        {
            Id = messageContract.MessageId,
            EventType = "ProcessImageJob",
            IdempotencyKey = $"JOB_{jobId}",
            Payload = JsonSerializer.Serialize(messageContract),
            Status = OutboxStatus.Pending,
            ScheduledAt = now,
            CreatedAt = now
        };

        // Ghi Job và OutboxMessage trong cùng 1 SQL Transaction sử dụng ExecutionStrategy
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _dbContext.ProcessingJobs.Add(job);
            _dbContext.OutboxMessages.Add(outboxMessage);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogInformation("Job {JobId} (Loại: {JobType}) cho tệp {FileId} đã được tạo với OutboxMessage {MessageId}.",
            job.Id, job.JobType, job.FileId, outboxMessage.Id);

        return AcceptedAtAction(
            nameof(GetJobById),
            new { id = job.Id },
            new
            {
                jobId = job.Id,
                fileId = job.FileId,
                jobType = job.JobType.ToString(),
                status = job.Status.ToString(),
                createdAt = job.CreatedAt,
                statusUrl = $"/jobs/{job.Id}",
                resultUrl = $"/files/{job.Id}/result"
            });
    }

    /// <summary>
    /// Tra cứu chi tiết trạng thái của Job theo mã JobId
    /// </summary>
    [HttpGet("jobs/{id:guid}")]
    [HttpGet("api/jobs/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.ProcessingJobs
            .Include(j => j.File)
            .Include(j => j.Result)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job == null)
        {
            return NotFound(new { error = $"Không tìm thấy Job với mã {id}." });
        }

        return Ok(new
        {
            jobId = job.Id,
            fileId = job.FileId,
            originalFileName = job.File?.OriginalFileName,
            jobType = job.JobType.ToString(),
            status = job.Status.ToString(),
            retryCount = job.RetryCount,
            maxRetries = job.MaxRetries,
            createdAt = job.CreatedAt,
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt,
            errorMessage = job.ErrorMessage,
            result = job.Result != null
                ? new
                {
                    resultId = job.Result.Id,
                    fileSize = job.Result.FileSize,
                    width = job.Result.Width,
                    height = job.Result.Height,
                    durationMs = job.Result.DurationMs,
                    createdAt = job.Result.CreatedAt,
                    downloadUrl = $"/files/{job.Id}/result"
                }
                : null
        });
    }

    /// <summary>
    /// Danh sách các Job gần đây phục vụ giao diện giám sát
    /// </summary>
    [HttpGet("jobs")]
    [HttpGet("api/jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentJobs([FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var jobs = await _dbContext.ProcessingJobs
            .Include(j => j.File)
            .Include(j => j.Result)
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .Select(j => new
            {
                jobId = j.Id,
                fileId = j.FileId,
                originalFileName = j.File != null ? j.File.OriginalFileName : null,
                jobType = j.JobType.ToString(),
                status = j.Status.ToString(),
                retryCount = j.RetryCount,
                createdAt = j.CreatedAt,
                startedAt = j.StartedAt,
                completedAt = j.CompletedAt,
                durationMs = j.Result != null ? (long?)j.Result.DurationMs : null,
                resultDownloadUrl = j.Result != null ? $"/files/{j.Id}/result" : null,
                errorMessage = j.ErrorMessage
            })
            .ToListAsync(cancellationToken);

        return Ok(jobs);
    }
}

