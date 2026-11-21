using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampaignApi.Services;
using Shared.Contracts.Data;
using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;
using Shared.Contracts.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối PostgreSQL từ file appsettings.json hoặc biến môi trường (ConnectionStrings__PostgreSql)
var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    ?? throw new InvalidOperationException("PostgreSQL Connection String not found in appsettings.json or environment variables.");

// 2. Đăng ký Entity Framework Core với Npgsql PostgreSQL
builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// Đăng ký dịch vụ RabbitMQ Publisher (Tuần 2)
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

// 3. Đăng ký Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BulkNotificationSystem - Campaign API",
        Version = "v1",
        Description = "ASP.NET Core Minimal API quản lý chiến dịch gửi thông báo hàng loạt (.NET 10)"
    });
});

// Cấu hình CORS để Admin Client hoặc Web Client có thể gọi tự do
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Kích hoạt Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Campaign API v1");
    c.RoutePrefix = string.Empty; // Mở Swagger ngay trang chủ "/"
});

// Auto migrate / ensure created on startup if database is reachable
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        logger.LogInformation("Đang kiểm tra và khởi tạo cấu trúc CSDL PostgreSQL...");
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation("Khởi tạo cấu trúc CSDL thành công!");

        // Khởi tạo người nhận mẫu nếu danh sách đang rỗng
        if (!await db.Recipients.AnyAsync())
        {
            var seedRecipients = new List<Recipient>
            {
                new() { FullName = "Nguyễn Văn An", Email = "an.nguyen@example.com", PhoneNumber = "0901234567" },
                new() { FullName = "Trần Thị Bình", Email = "binh.tran@example.com", PhoneNumber = "0912345678" },
                new() { FullName = "Lê Hoàng Cường", Email = "cuong.le@example.com", PhoneNumber = "0987654321" },
                new() { FullName = "Phạm Thu Dung", Email = "dung.pham@example.com", PhoneNumber = "0934567890" },
                new() { FullName = "Hoàng Minh Đức", Email = "duc.hoang@example.com", PhoneNumber = "0945678901" }
            };
            db.Recipients.AddRange(seedRecipients);
            await db.SaveChangesAsync();
            logger.LogInformation("Đã khởi tạo sẵn {Count} người nhận mẫu vào CSDL.", seedRecipients.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Không thể kết nối đến PostgreSQL khi khởi động (CSDL có thể chưa chạy). Vui lòng đảm bảo PostgreSQL đã khởi chạy.");
    }
}

// ==========================================
// Minimal API Endpoints
// ==========================================

// 1. Health check & DB/RabbitMQ status
app.MapGet("/health", async (NotificationDbContext db, IRabbitMqPublisher rabbitPublisher) =>
{
    bool canConnectDb = false;
    string? errorDb = null;
    try
    {
        canConnectDb = await db.Database.CanConnectAsync();
    }
    catch (Exception ex)
    {
        errorDb = ex.Message;
    }

    bool canConnectRabbit = await rabbitPublisher.CanConnectAsync();

    return Results.Ok(new
    {
        status = (canConnectDb && canConnectRabbit) ? "Healthy" : (canConnectDb ? "Degraded" : "Unhealthy"),
        database = "PostgreSQL",
        databaseConnected = canConnectDb,
        databaseError = errorDb,
        rabbitMq = "RabbitMQ",
        rabbitMqConnected = canConnectRabbit,
        timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck")
.WithTags("System");

// 2. Thống kê tổng quan hệ thống
app.MapGet("/api/stats", async (NotificationDbContext db) =>
{
    var totalCampaigns = await db.Campaigns.CountAsync();
    var totalRecipients = await db.Recipients.CountAsync();
    var totalDeliveries = await db.NotificationDeliveries.CountAsync();
    var pendingDeliveries = await db.NotificationDeliveries.CountAsync(d => d.Status == DeliveryStatus.Pending);
    var sentDeliveries = await db.NotificationDeliveries.CountAsync(d => d.Status == DeliveryStatus.Sent);
    var failedDeliveries = await db.NotificationDeliveries.CountAsync(d => d.Status == DeliveryStatus.Failed);

    return Results.Ok(new
    {
        totalCampaigns,
        totalRecipients,
        deliveries = new
        {
            total = totalDeliveries,
            pending = pendingDeliveries,
            sent = sentDeliveries,
            failed = failedDeliveries
        }
    });
})
.WithName("GetSystemStats")
.WithTags("System");

// 3. Lấy danh sách chiến dịch
var getCampaignsHandler = async (NotificationDbContext db) =>
{
    var campaigns = await db.Campaigns
        .AsNoTracking()
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new CampaignSummaryDto
        {
            Id = c.Id,
            Name = c.Name,
            Title = c.Title,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            CompletedAt = c.CompletedAt,
            TotalRecipients = c.Deliveries.Count,
            SentCount = c.Deliveries.Count(d => d.Status == DeliveryStatus.Sent),
            FailedCount = c.Deliveries.Count(d => d.Status == DeliveryStatus.Failed),
            PendingCount = c.Deliveries.Count(d => d.Status == DeliveryStatus.Pending)
        })
        .ToListAsync();

    return Results.Ok(campaigns);
};

app.MapGet("/api/campaigns", getCampaignsHandler)
    .WithName("GetCampaigns")
    .WithTags("Campaigns");

app.MapGet("/campaigns", getCampaignsHandler)
    .WithName("GetCampaignsWithoutPrefix")
    .WithTags("Campaigns");

// 4. Lấy chi tiết chiến dịch
var getCampaignByIdHandler = async (Guid id, NotificationDbContext db) =>
{
    var campaign = await db.Campaigns
        .AsNoTracking()
        .Include(c => c.Deliveries)
            .ThenInclude(d => d.Recipient)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (campaign is null)
        return Results.NotFound(new { message = $"Không tìm thấy chiến dịch có mã: {id}" });

    var detail = new CampaignDetailDto
    {
        Id = campaign.Id,
        Name = campaign.Name,
        Title = campaign.Title,
        Content = campaign.Content,
        Status = campaign.Status,
        CreatedAt = campaign.CreatedAt,
        CompletedAt = campaign.CompletedAt,
        TotalRecipients = campaign.Deliveries.Count,
        SentCount = campaign.Deliveries.Count(d => d.Status == DeliveryStatus.Sent),
        FailedCount = campaign.Deliveries.Count(d => d.Status == DeliveryStatus.Failed),
        PendingCount = campaign.Deliveries.Count(d => d.Status == DeliveryStatus.Pending),
        Deliveries = campaign.Deliveries.Select(d => new NotificationDeliveryDto
        {
            JobId = d.JobId,
            CampaignId = d.CampaignId,
            RecipientId = d.RecipientId,
            RecipientName = d.Recipient?.FullName ?? "N/A",
            Destination = d.Recipient?.Email ?? d.Recipient?.PhoneNumber ?? "N/A",
            Status = d.Status,
            RetryCount = d.RetryCount,
            ErrorMessage = d.ErrorMessage,
            CreatedAt = d.CreatedAt,
            ProcessedAt = d.ProcessedAt,
            PublishedAt = d.PublishedAt
        }).ToList()
    };

    return Results.Ok(detail);
};

app.MapGet("/api/campaigns/{id:guid}", getCampaignByIdHandler)
    .WithName("GetCampaignById")
    .WithTags("Campaigns");

app.MapGet("/campaigns/{id:guid}", getCampaignByIdHandler)
    .WithName("GetCampaignByIdWithoutPrefix")
    .WithTags("Campaigns");

// 5. Tạo mới chiến dịch thông báo
var createCampaignHandler = async ([FromBody] CreateCampaignRequest request, NotificationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(new { message = "Tên chiến dịch, tiêu đề và nội dung không được để trống." });
    }

    // Xác định danh sách người nhận
    List<Recipient> targetRecipients;
    if (request.RecipientIds is not null && request.RecipientIds.Count > 0)
    {
        targetRecipients = await db.Recipients
            .Where(r => request.RecipientIds.Contains(r.Id))
            .ToListAsync();
    }
    else
    {
        // Mặc định gửi tới toàn bộ người nhận hiện có trong hệ thống
        targetRecipients = await db.Recipients.ToListAsync();
    }

    if (targetRecipients.Count == 0)
    {
        return Results.BadRequest(new { message = "Không có người nhận nào trong hệ thống. Vui lòng thêm người nhận trước khi tạo chiến dịch." });
    }

    var campaign = new Campaign
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Title = request.Title.Trim(),
        Content = request.Content.Trim(),
        Status = request.SendImmediately ? CampaignStatus.Processing : CampaignStatus.Draft,
        CreatedAt = DateTime.UtcNow
    };

    // Tạo chi tiết từng lượt gửi (NotificationDelivery) với trạng thái Pending
    foreach (var recipient in targetRecipients)
    {
        var delivery = new NotificationDelivery
        {
            JobId = Guid.NewGuid(),
            CampaignId = campaign.Id,
            RecipientId = recipient.Id,
            Status = DeliveryStatus.Pending,
            RetryCount = 0,
            ErrorMessage = null,
            CreatedAt = DateTime.UtcNow
        };
        campaign.Deliveries.Add(delivery);
    }

    db.Campaigns.Add(campaign);
    await db.SaveChangesAsync();

    return Results.Created($"/api/campaigns/{campaign.Id}", new
    {
        campaign.Id,
        campaign.Name,
        campaign.Title,
        campaign.Status,
        TotalJobs = campaign.Deliveries.Count,
        message = "Chiến dịch đã được tạo thành công và các lượt gửi đang ở trạng thái Pending."
    });
};

app.MapPost("/api/campaigns", createCampaignHandler)
    .WithName("CreateCampaign")
    .WithTags("Campaigns");

app.MapPost("/campaigns", createCampaignHandler)
    .WithName("CreateCampaignWithoutPrefix")
    .WithTags("Campaigns");

// 6. Kích hoạt phát chiến dịch và đưa vào RabbitMQ (Hỗ trợ cả /start và /send, cả có và không có tiền tố /api)
var startCampaignHandler = async (
    Guid id,
    NotificationDbContext db,
    IRabbitMqPublisher rabbitPublisher,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var campaign = await db.Campaigns
        .Include(c => c.Deliveries)
            .ThenInclude(d => d.Recipient)
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    if (campaign is null)
        return Results.NotFound(new { message = $"Không tìm thấy chiến dịch: {id}" });

    if (campaign.Deliveries.Count == 0)
        return Results.BadRequest(new { message = "Chiến dịch không có lượt gửi nào." });

    // Lọc các lượt gửi chưa được đẩy vào RabbitMQ và còn ở trạng thái Pending
    var unPublishedDeliveries = campaign.Deliveries
        .Where(d => d.PublishedAt == null && d.Status == DeliveryStatus.Pending)
        .ToList();

    if (unPublishedDeliveries.Count == 0)
    {
        return Results.Ok(new
        {
            message = "Tất cả lượt gửi của chiến dịch này đã được đưa vào hàng đợi trước đó (tránh trùng lặp job).",
            campaign.Id,
            campaign.Status,
            totalDeliveries = campaign.Deliveries.Count,
            newlyPublishedCount = 0
        });
    }

    // Kiểm tra kết nối tới RabbitMQ trước khi tiến hành xuất bản
    var isRabbitConnected = await rabbitPublisher.CanConnectAsync(cancellationToken);
    if (!isRabbitConnected)
    {
        logger.LogError("Không thể kết nối tới RabbitMQ message broker khi khởi chạy chiến dịch [{CampaignId}].", campaign.Id);
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "RabbitMQ Broker Không Khả Dụng",
            detail: "Không thể kết nối đến RabbitMQ message broker tại cổng 5672. Vui lòng đảm bảo container RabbitMQ đã khởi chạy.");
    }

    // Chuẩn bị danh sách thông điệp (NotificationJobMessage)
    var messages = unPublishedDeliveries.Select(d => new NotificationJobMessage
    {
        JobId = d.JobId,
        CampaignId = d.CampaignId,
        RecipientId = d.RecipientId,
        RecipientName = d.Recipient?.FullName ?? "N/A",
        Destination = d.Recipient?.Email ?? d.Recipient?.PhoneNumber ?? "N/A",
        Title = campaign.Title,
        Content = campaign.Content,
        RetryCount = d.RetryCount,
        QueuedAt = DateTime.UtcNow
    }).ToList();

    int publishedCount = 0;
    try
    {
        // Xuất bản hàng loạt với cơ chế ghi nhận trạng thái từng message
        publishedCount = await rabbitPublisher.PublishBatchAsync(messages, async (jobId) =>
        {
            var delivery = unPublishedDeliveries.FirstOrDefault(d => d.JobId == jobId);
            if (delivery != null)
            {
                delivery.PublishedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

        // Chuyển trạng thái chiến dịch sang Processing
        campaign.Status = CampaignStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("🚀 Đã xuất bản thành công {Count} lượt gửi của chiến dịch [{CampaignId}] vào RabbitMQ queue [notification_queue].",
            publishedCount, campaign.Id);

        return Results.Ok(new
        {
            message = $"Đã kích hoạt chiến dịch và đưa thành công {publishedCount} lượt gửi vào RabbitMQ queue [notification_queue].",
            campaign.Id,
            campaign.Status,
            totalDeliveries = campaign.Deliveries.Count,
            newlyPublishedCount = publishedCount
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra trong quá trình xuất bản message vào RabbitMQ cho chiến dịch [{CampaignId}]. Đã xuất bản {PublishedCount}/{Total}",
            campaign.Id, publishedCount, unPublishedDeliveries.Count);

        return Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Lỗi xuất bản thông điệp vào RabbitMQ",
            detail: $"Đã phát sinh sự cố khi gửi vào hàng đợi: {ex.Message}. Đã ghi nhận thành công {publishedCount}/{unPublishedDeliveries.Count} lượt gửi.");
    }
};

app.MapPost("/api/campaigns/{id:guid}/send", startCampaignHandler)
    .WithName("SendCampaign")
    .WithTags("Campaigns");

app.MapPost("/api/campaigns/{id:guid}/start", startCampaignHandler)
    .WithName("StartCampaign")
    .WithTags("Campaigns");

app.MapPost("/campaigns/{id:guid}/start", startCampaignHandler)
    .WithName("StartCampaignWithoutPrefix")
    .WithTags("Campaigns");

app.MapPost("/campaigns/{id:guid}/send", startCampaignHandler)
    .WithName("SendCampaignWithoutPrefix")
    .WithTags("Campaigns");

// 7. Lấy danh sách người nhận
var getRecipientsHandler = async (NotificationDbContext db) =>
{
    var recipients = await db.Recipients
        .AsNoTracking()
        .OrderBy(r => r.FullName)
        .Select(r => new RecipientDto
        {
            Id = r.Id,
            FullName = r.FullName,
            Email = r.Email,
            PhoneNumber = r.PhoneNumber,
            CreatedAt = r.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(recipients);
};

app.MapGet("/api/recipients", getRecipientsHandler)
    .WithName("GetRecipients")
    .WithTags("Recipients");

app.MapGet("/recipients", getRecipientsHandler)
    .WithName("GetRecipientsWithoutPrefix")
    .WithTags("Recipients");

// 8. Thêm người nhận mới
var createRecipientHandler = async ([FromBody] CreateRecipientRequest request, NotificationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName))
        return Results.BadRequest(new { message = "Họ tên người nhận không được để trống." });

    if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        return Results.BadRequest(new { message = "Phải cung cấp ít nhất Email hoặc Số điện thoại." });

    var recipient = new Recipient
    {
        Id = Guid.NewGuid(),
        FullName = request.FullName.Trim(),
        Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
        PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    db.Recipients.Add(recipient);
    await db.SaveChangesAsync();

    return Results.Created($"/api/recipients/{recipient.Id}", recipient);
};

app.MapPost("/api/recipients", createRecipientHandler)
    .WithName("CreateRecipient")
    .WithTags("Recipients");

app.MapPost("/recipients", createRecipientHandler)
    .WithName("CreateRecipientWithoutPrefix")
    .WithTags("Recipients");

// 9. Lấy danh sách lượt gửi (Deliveries)
var getDeliveriesHandler = async (
    [FromQuery] Guid? campaignId,
    [FromQuery] DeliveryStatus? status,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    NotificationDbContext db = null!) =>
{
    var query = db.NotificationDeliveries
        .AsNoTracking()
        .Include(d => d.Recipient)
        .AsQueryable();

    if (campaignId.HasValue)
        query = query.Where(d => d.CampaignId == campaignId.Value);

    if (status.HasValue)
        query = query.Where(d => d.Status == status.Value);

    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(d => d.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(d => new NotificationDeliveryDto
        {
            JobId = d.JobId,
            CampaignId = d.CampaignId,
            RecipientId = d.RecipientId,
            RecipientName = d.Recipient != null ? d.Recipient.FullName : "N/A",
            Destination = d.Recipient != null ? (d.Recipient.Email ?? d.Recipient.PhoneNumber) : "N/A",
            Status = d.Status,
            RetryCount = d.RetryCount,
            ErrorMessage = d.ErrorMessage,
            CreatedAt = d.CreatedAt,
            ProcessedAt = d.ProcessedAt
        })
        .ToListAsync();

    return Results.Ok(new
    {
        total,
        page,
        pageSize,
        items
    });
};

app.MapGet("/api/deliveries", getDeliveriesHandler)
    .WithName("GetDeliveries")
    .WithTags("Deliveries");

app.MapGet("/deliveries", getDeliveriesHandler)
    .WithName("GetDeliveriesWithoutPrefix")
    .WithTags("Deliveries");

app.Run();
