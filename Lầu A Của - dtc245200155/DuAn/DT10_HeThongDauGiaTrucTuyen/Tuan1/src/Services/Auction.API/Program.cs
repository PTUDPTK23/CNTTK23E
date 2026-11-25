using Auction.API.Data;
using Auction.API.Workers;
using AuctionSystem.Contracts;
using AuctionSystem.Shared.Common;
using AuctionSystem.Shared.DTOs;
using AuctionSystem.Shared.Events;
using AuctionSystem.Shared.Models;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;

// Cho phép gRPC gọi qua cleartext HTTP/2
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Configure port
var port = builder.Configuration.GetValue<int>("PORT", 5001);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, port);
});

// Configure Database (PostgreSQL or SQLite)
var databaseType = builder.Configuration.GetValue<string>("DatabaseType", "Sqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? (databaseType.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) 
                           ? "Host=db;Port=5432;Database=auction_db;Username=postgres;Password=postgres" 
                           : "Data Source=auction.db");

builder.Services.AddDbContext<AuctionDbContext>(options =>
{
    if (databaseType.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Configure gRPC Client for Bid Validation Service
var bidValidationUrl = builder.Configuration.GetValue<string>("Services:BidValidationUrl", "http://localhost:5002");
builder.Services.AddGrpcClient<BidValidation.BidValidationClient>(o =>
{
    o.Address = new Uri(bidValidationUrl);
}).ConfigureChannel(o =>
{
    o.HttpHandler = new SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true
    };
});

// Configure RabbitMQ Publisher
var rabbitHost = builder.Configuration.GetValue<string>("RabbitMQ:Host", "localhost");
var rabbitPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
var rabbitUser = builder.Configuration.GetValue<string>("RabbitMQ:Username", "guest");
var rabbitPass = builder.Configuration.GetValue<string>("RabbitMQ:Password", "guest");

builder.Services.AddSingleton<IRabbitMqPublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
    return new RabbitMqPublisher(rabbitHost, rabbitPort, rabbitUser, rabbitPass, logger);
});

// Background Worker to auto-end expired auctions
builder.Services.AddHostedService<AuctionExpirationWorker>();

// Add CORS & Swagger & HealthChecks
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Migrate and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
    await DbInitializer.SeedAsync(db);
}

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");

// 1. GET / - Trả về thông tin dịch vụ (HTTP 200 OK)
app.MapGet("/", () => Results.Ok(new ServiceInfoDto(
    Service: "Auction.API",
    Version: "1.0.0",
    Status: "Online",
    Description: "RESTful API cho Hệ thống Đấu giá trực tuyến phân tán (.NET 10 Minimal API)",
    Timestamp: DateTime.UtcNow
)))
.WithName("GetServiceInfo")
.WithSummary("Thông tin trạng thái dịch vụ");

// 2. GET /auctions - Tra cứu danh sách phiên đấu giá (HTTP 200 OK)
app.MapGet("/auctions", async (string? status, AuctionDbContext db) =>
{
    IQueryable<AuctionSystem.Shared.Models.Auction> query = db.Auctions.Include(a => a.Bids);

    if (!string.IsNullOrWhiteSpace(status))
    {
        if (Enum.TryParse<AuctionStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }
    }

    var list = await query
        .OrderByDescending(a => a.CreatedAt)
        .Select(a => new AuctionResponse(
            a.Id,
            a.Title,
            a.Description,
            a.ImageUrl,
            a.StartingPrice,
            a.CurrentPrice,
            a.StepPrice,
            a.CurrentWinner,
            a.StartTime,
            a.EndTime,
            a.Status,
            a.Status.ToString(),
            a.Winner,
            a.Bids.Count(b => b.IsAccepted),
            a.CreatedAt
        ))
        .ToListAsync();

    return Results.Ok(list);
})
.WithName("GetAuctions")
.WithSummary("Lấy danh sách tất cả các phiên đấu giá");

// 3. GET /auctions/{id} - Tra cứu chi tiết một phiên đấu giá (200 OK hoặc 404 AUCTION_NOT_FOUND)
app.MapGet("/auctions/{id:guid}", async (Guid id, AuctionDbContext db) =>
{
    var auction = await db.Auctions
        .Include(a => a.Bids)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (auction == null)
    {
        return Results.NotFound(new ApiError(
            ApiErrorCodes.AuctionNotFound,
            $"Không tìm thấy phiên đấu giá với ID '{id}'."
        ));
    }

    var response = new AuctionResponse(
        auction.Id,
        auction.Title,
        auction.Description,
        auction.ImageUrl,
        auction.StartingPrice,
        auction.CurrentPrice,
        auction.StepPrice,
        auction.CurrentWinner,
        auction.StartTime,
        auction.EndTime,
        auction.Status,
        auction.Status.ToString(),
        auction.Winner,
        auction.Bids.Count(b => b.IsAccepted),
        auction.CreatedAt
    );

    return Results.Ok(response);
})
.WithName("GetAuctionById")
.WithSummary("Lấy thông tin chi tiết một phiên đấu giá theo ID");

// 4. POST /auctions - Tạo phiên đấu giá mới (201 Created kèm header Location, hoặc 400 INVALID_AUCTION_DATA)
app.MapPost("/auctions", async (
    CreateAuctionRequest req,
    AuctionDbContext db,
    IRabbitMqPublisher publisher,
    HttpContext httpContext) =>
{
    // Kiểm tra dữ liệu đầu vào
    if (string.IsNullOrWhiteSpace(req.Title))
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.InvalidAuctionData,
            "Tên phiên đấu giá không được để trống."
        ));
    }

    if (req.StartingPrice <= 0)
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.InvalidAuctionData,
            "Giá khởi điểm phải lớn hơn 0."
        ));
    }

    if (req.StepPrice <= 0)
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.InvalidAuctionData,
            "Bước giá tối thiểu phải lớn hơn 0."
        ));
    }

    if (req.EndTime <= req.StartTime)
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.InvalidAuctionData,
            "Thời gian kết thúc phải diễn ra sau thời gian bắt đầu."
        ));
    }

    var now = DateTime.UtcNow;
    var status = (now >= req.StartTime && now < req.EndTime)
        ? AuctionStatus.Active
        : (now < req.StartTime ? AuctionStatus.Pending : AuctionStatus.Ended);

    var auction = new AuctionSystem.Shared.Models.Auction
    {
        Id = Guid.NewGuid(),
        Title = req.Title.Trim(),
        Description = req.Description?.Trim() ?? string.Empty,
        ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl)
            ? "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=60"
            : req.ImageUrl.Trim(),
        StartingPrice = req.StartingPrice,
        CurrentPrice = req.StartingPrice,
        StepPrice = req.StepPrice,
        CurrentWinner = null,
        StartTime = req.StartTime,
        EndTime = req.EndTime,
        Status = status,
        CreatedAt = now
    };

    db.Auctions.Add(auction);
    await db.SaveChangesAsync();

    // Publish event qua RabbitMQ (Bất đồng bộ)
    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    var createdEvent = new AuctionCreatedEvent
    {
        AuctionId = auction.Id,
        CorrelationId = correlationId,
        Title = auction.Title,
        StartingPrice = auction.StartingPrice,
        StepPrice = auction.StepPrice,
        StartTime = auction.StartTime,
        EndTime = auction.EndTime,
        CreatedAt = auction.CreatedAt
    };

    _ = Task.Run(() =>
    {
        try
        {
            publisher.Publish(RabbitMqConstants.RoutingKeyAuctionCreated, createdEvent, correlationId);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not publish AuctionCreatedEvent immediately to RabbitMQ.");
        }
    });

    var resultDto = new AuctionResponse(
        auction.Id,
        auction.Title,
        auction.Description,
        auction.ImageUrl,
        auction.StartingPrice,
        auction.CurrentPrice,
        auction.StepPrice,
        auction.CurrentWinner,
        auction.StartTime,
        auction.EndTime,
        auction.Status,
        auction.Status.ToString(),
        auction.Winner,
        0,
        auction.CreatedAt
    );

    return Results.Created($"/auctions/{auction.Id}", resultDto);
})
.WithName("CreateAuction")
.WithSummary("Tạo phiên đấu giá mới");

// 5. POST /auctions/{id}/bids - Gửi mức giá mới (gRPC Validate -> 201 Created có Location hoặc 400/404 ApiError)
app.MapPost("/auctions/{id:guid}/bids", async (
    Guid id,
    PlaceBidRequest req,
    AuctionDbContext db,
    BidValidation.BidValidationClient grpcClient,
    IRabbitMqPublisher publisher,
    HttpContext httpContext) =>
{
    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

    // 1. Kiểm tra phiên đấu giá có tồn tại không?
    var auction = await db.Auctions.FirstOrDefaultAsync(a => a.Id == id);
    if (auction == null)
    {
        return Results.NotFound(new ApiError(
            ApiErrorCodes.AuctionNotFound,
            $"Không tìm thấy phiên đấu giá với ID '{id}'."
        ));
    }

    // 2. Kiểm tra tên người trả giá
    if (string.IsNullOrWhiteSpace(req.Bidder))
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.BidRejected,
            "Tên người trả giá không được để trống."
        ));
    }

    // 3. Kiểm tra nếu phiên đã đóng
    var now = DateTime.UtcNow;
    if (auction.Status == AuctionStatus.Ended || now >= auction.EndTime)
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.AuctionEnded,
            "Phiên đấu giá đã kết thúc, không thể gửi lượt đặt giá mới."
        ));
    }

    // 4. Gọi Bid Validation Service qua gRPC để kiểm tra độc lập
    ValidateBidResponse validationResult;
    try
    {
        var grpcRequest = new ValidateBidRequest
        {
            AuctionId = id.ToString(),
            Bidder = req.Bidder.Trim(),
            Amount = (double)req.Amount,
            CurrentPrice = (double)auction.CurrentPrice,
            StepPrice = (double)auction.StepPrice,
            StartingPrice = (double)auction.StartingPrice,
            AuctionStatus = auction.Status.ToString(),
            StartTime = auction.StartTime.ToString("o"),
            EndTime = auction.EndTime.ToString("o")
        };

        validationResult = await grpcClient.ValidateBidAsync(grpcRequest);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to call Bid Validation Service via gRPC for AuctionId {AuctionId}", id);
        return Results.Json(
            new ApiError(
                ApiErrorCodes.ServiceUnavailable,
                "Dịch vụ kiểm tra giá (Bid Validation gRPC) hiện không phản hồi. Vui lòng thử lại sau.",
                ex.Message
            ),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // Nếu gRPC trả về không hợp lệ
    if (!validationResult.IsValid)
    {
        var rejectedBid = new Bid
        {
            AuctionId = id,
            Bidder = req.Bidder.Trim(),
            Amount = req.Amount,
            BidTime = DateTime.UtcNow,
            IsAccepted = false,
            Reason = validationResult.Reason
        };
        db.Bids.Add(rejectedBid);
        await db.SaveChangesAsync();

        return Results.BadRequest(new ApiError(
            ApiErrorCodes.BidRejected,
            validationResult.Reason,
            $"Mức giá tối thiểu hợp lệ là: {validationResult.MinRequiredPrice:N0} đ"
        ));
    }

    // 5. Nếu hợp lệ: Cập nhật Auction & lưu Bid hợp lệ
    var previousPrice = auction.CurrentPrice;
    auction.CurrentPrice = req.Amount;
    auction.CurrentWinner = req.Bidder.Trim();

    var acceptedBid = new Bid
    {
        Id = Guid.NewGuid(),
        AuctionId = id,
        Bidder = req.Bidder.Trim(),
        Amount = req.Amount,
        BidTime = DateTime.UtcNow,
        IsAccepted = true,
        Reason = "Hợp lệ"
    };

    db.Bids.Add(acceptedBid);
    await db.SaveChangesAsync();

    // 6. Phát sự kiện BidAccepted qua RabbitMQ (Bất đồng bộ)
    var bidAcceptedEvent = new BidAcceptedEvent
    {
        AuctionId = id,
        CorrelationId = correlationId,
        Bidder = acceptedBid.Bidder,
        Amount = acceptedBid.Amount,
        PreviousPrice = previousPrice,
        CurrentWinner = auction.CurrentWinner,
        EndTime = auction.EndTime,
        Timestamp = acceptedBid.BidTime
    };

    _ = Task.Run(() =>
    {
        try
        {
            publisher.Publish(RabbitMqConstants.RoutingKeyBidAccepted, bidAcceptedEvent, correlationId);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not publish BidAcceptedEvent to RabbitMQ.");
        }
    });

    var bidDto = new BidResponse(
        acceptedBid.Id,
        acceptedBid.AuctionId,
        acceptedBid.Bidder,
        acceptedBid.Amount,
        acceptedBid.BidTime,
        acceptedBid.IsAccepted,
        acceptedBid.Reason
    );

    // Trả 201 Created kèm header Location trỏ tới lượt nộp/đặt giá vừa tạo
    return Results.Created($"/auctions/{id}/bids/{acceptedBid.Id}", bidDto);
})
.WithName("PlaceBid")
.WithSummary("Gửi mức giá mới cho phiên đấu giá (gọi gRPC kiểm tra & phát RabbitMQ)");

// 6. GET /auctions/{id}/bids - Tra cứu lịch sử trả giá của phiên (HTTP 200 OK)
app.MapGet("/auctions/{id:guid}/bids", async (Guid id, AuctionDbContext db) =>
{
    var exists = await db.Auctions.AnyAsync(a => a.Id == id);
    if (!exists)
    {
        return Results.NotFound(new ApiError(
            ApiErrorCodes.AuctionNotFound,
            $"Không tìm thấy phiên đấu giá với ID '{id}'."
        ));
    }

    var bids = await db.Bids
        .Where(b => b.AuctionId == id)
        .OrderByDescending(b => b.BidTime)
        .Select(b => new BidResponse(
            b.Id,
            b.AuctionId,
            b.Bidder,
            b.Amount,
            b.BidTime,
            b.IsAccepted,
            b.Reason
        ))
        .ToListAsync();

    return Results.Ok(bids);
})
.WithName("GetAuctionBids")
.WithSummary("Lấy lịch sử các lượt trả giá của một phiên");

// 7. GET /auctions/{id}/bids/{bidId} - Đọc lại lượt trả giá theo ID lấy từ Location (HTTP 200 OK hoặc 404 BID_NOT_FOUND)
app.MapGet("/auctions/{id:guid}/bids/{bidId:guid}", async (Guid id, Guid bidId, AuctionDbContext db) =>
{
    var bid = await db.Bids.FirstOrDefaultAsync(b => b.AuctionId == id && b.Id == bidId);
    if (bid == null)
    {
        return Results.NotFound(new ApiError(
            ApiErrorCodes.BidNotFound,
            $"Không tìm thấy lượt đặt giá với ID '{bidId}' trong phiên '{id}'."
        ));
    }

    return Results.Ok(new BidResponse(
        bid.Id,
        bid.AuctionId,
        bid.Bidder,
        bid.Amount,
        bid.BidTime,
        bid.IsAccepted,
        bid.Reason
    ));
})
.WithName("GetBidById")
.WithSummary("Đọc lại lượt đặt giá theo ID từ header Location");

// 8. POST /auctions/{id}/end - Kết thúc thủ công một phiên đấu giá (HTTP 200 OK)
app.MapPost("/auctions/{id:guid}/end", async (
    Guid id,
    AuctionDbContext db,
    IRabbitMqPublisher publisher,
    HttpContext httpContext) =>
{
    var auction = await db.Auctions
        .Include(a => a.Bids)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (auction == null)
    {
        return Results.NotFound(new ApiError(
            ApiErrorCodes.AuctionNotFound,
            $"Không tìm thấy phiên đấu giá với ID '{id}'."
        ));
    }

    if (auction.Status == AuctionStatus.Ended)
    {
        return Results.BadRequest(new ApiError(
            ApiErrorCodes.AuctionEnded,
            "Phiên đấu giá đã kết thúc từ trước."
        ));
    }

    var now = DateTime.UtcNow;
    auction.Status = AuctionStatus.Ended;
    auction.EndTime = now;

    var highestBid = auction.Bids
        .Where(b => b.IsAccepted)
        .OrderByDescending(b => b.Amount)
        .FirstOrDefault();

    auction.Winner = highestBid?.Bidder;
    auction.CurrentWinner = auction.Winner;

    await db.SaveChangesAsync();

    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    var endedEvent = new AuctionEndedEvent
    {
        AuctionId = auction.Id,
        CorrelationId = correlationId,
        Title = auction.Title,
        Winner = auction.Winner,
        FinalPrice = auction.CurrentPrice,
        TotalBids = auction.Bids.Count(b => b.IsAccepted),
        EndedAt = now
    };

    try
    {
        publisher.Publish(RabbitMqConstants.RoutingKeyAuctionEnded, endedEvent, correlationId);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to publish AuctionEndedEvent to RabbitMQ.");
    }

    return Results.Ok(new
    {
        message = "Phiên đấu giá đã kết thúc thành công.",
        winner = auction.Winner ?? "Không có người thắng (chưa có lượt đặt giá)",
        finalPrice = auction.CurrentPrice
    });
})
.WithName("EndAuction")
.WithSummary("Kết thúc phiên đấu giá và chốt người chiến thắng");

// 9. DELETE /auctions/{id} - Hủy phiên đấu giá (HTTP 204 No Content - Phần mở rộng)
app.MapDelete("/auctions/{id:guid}", async (Guid id, AuctionDbContext db) =>
{
    var auction = await db.Auctions.FirstOrDefaultAsync(a => a.Id == id);
    if (auction == null)
    {
        return Results.NotFound(new ApiError(
            ApiErrorCodes.AuctionNotFound,
            $"Không tìm thấy phiên đấu giá với ID '{id}' để hủy."
        ));
    }

    db.Auctions.Remove(auction);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteAuction")
.WithSummary("Hủy phiên đấu giá (trả 204 No Content)");

app.Run();

public partial class Program { }
