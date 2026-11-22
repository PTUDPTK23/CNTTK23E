using System;
using System.Linq;
using System.Text.Json.Serialization;
using EventFlow.EventApi.Data;
using EventFlow.EventApi.Entities;
using EventFlow.EventApi.Services;
using EventFlow.Shared.Messaging;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Database setup: support PostgreSQL and SQLite fallback
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
                           ? builder.Configuration.GetConnectionString("PostgresConnection")
                           : "Data Source=eventflow_events.db");

builder.Services.AddDbContext<EventFlowDbContext>(options =>
{
    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Register RabbitMQ publisher
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EventFlow - Event Registration & Check-in API", Version = "v1" });
});

var app = builder.Build();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventFlow API v1");
    c.RoutePrefix = "swagger";
});

// Ensure database created & seed sample events
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventFlowDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Ensuring EventFlow database exists using provider {Provider}...", dbProvider);
        await db.Database.EnsureCreatedAsync();
        await EventFlowDbContext.SeedAsync(db);
        logger.LogInformation("Database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "EventFlow.EventApi", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("System");

// ==========================================
// 1. EVENT ENDPOINTS
// ==========================================

// GET /events
app.MapGet("/events", async (string? search, EventStatus? status, EventFlowDbContext db) =>
{
    var query = db.Events.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim().ToLower();
        query = query.Where(e => e.Name.ToLower().Contains(s) || e.Location.ToLower().Contains(s) || e.Description.ToLower().Contains(s));
    }

    if (status.HasValue)
    {
        query = query.Where(e => e.Status == status.Value);
    }

    var events = await query
        .OrderBy(e => e.StartTime)
        .Select(e => new EventDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Location = e.Location,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            MaxCapacity = e.MaxCapacity,
            RegisteredCount = e.RegisteredCount,
            Status = e.Status,
            CreatedAt = e.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(events);
})
.WithName("GetEvents")
.WithTags("Events");

// GET /events/{id}
app.MapGet("/events/{id:guid}", async (Guid id, EventFlowDbContext db) =>
{
    var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    if (ev == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy sự kiện với mã {id}" });
    }

    var dto = new EventDto
    {
        Id = ev.Id,
        Name = ev.Name,
        Description = ev.Description,
        Location = ev.Location,
        StartTime = ev.StartTime,
        EndTime = ev.EndTime,
        MaxCapacity = ev.MaxCapacity,
        RegisteredCount = ev.RegisteredCount,
        Status = ev.Status,
        CreatedAt = ev.CreatedAt
    };

    return Results.Ok(dto);
})
.WithName("GetEventById")
.WithTags("Events");

// POST /events
app.MapPost("/events", async (CreateEventRequest request, EventFlowDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { message = "Tên sự kiện không được để trống" });

    if (request.StartTime >= request.EndTime)
        return Results.BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu" });

    if (request.MaxCapacity <= 0)
        return Results.BadRequest(new { message = "Sức chứa tối đa phải lớn hơn 0" });

    var newEvent = new Event
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Description = request.Description.Trim(),
        Location = request.Location.Trim(),
        StartTime = request.StartTime,
        EndTime = request.EndTime,
        MaxCapacity = request.MaxCapacity,
        RegisteredCount = 0,
        Status = EventStatus.Upcoming,
        CreatedAt = DateTime.UtcNow,
        ConcurrencyToken = Guid.NewGuid()
    };

    db.Events.Add(newEvent);
    await db.SaveChangesAsync();

    var dto = new EventDto
    {
        Id = newEvent.Id,
        Name = newEvent.Name,
        Description = newEvent.Description,
        Location = newEvent.Location,
        StartTime = newEvent.StartTime,
        EndTime = newEvent.EndTime,
        MaxCapacity = newEvent.MaxCapacity,
        RegisteredCount = newEvent.RegisteredCount,
        Status = newEvent.Status,
        CreatedAt = newEvent.CreatedAt
    };

    return Results.Created($"/events/{newEvent.Id}", dto);
})
.WithName("CreateEvent")
.WithTags("Events");

// PUT /events/{id}
app.MapPut("/events/{id:guid}", async (Guid id, UpdateEventRequest request, EventFlowDbContext db) =>
{
    var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
    if (ev == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy sự kiện với mã {id}" });
    }

    if (request.StartTime >= request.EndTime)
        return Results.BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu" });

    if (request.MaxCapacity < ev.RegisteredCount)
        return Results.BadRequest(new { message = $"Sức chứa mới ({request.MaxCapacity}) không được nhỏ hơn số người đã đăng ký ({ev.RegisteredCount})" });

    ev.Name = request.Name.Trim();
    ev.Description = request.Description.Trim();
    ev.Location = request.Location.Trim();
    ev.StartTime = request.StartTime;
    ev.EndTime = request.EndTime;
    ev.MaxCapacity = request.MaxCapacity;
    ev.Status = request.Status;
    ev.ConcurrencyToken = Guid.NewGuid();

    await db.SaveChangesAsync();

    return Results.Ok(new EventDto
    {
        Id = ev.Id,
        Name = ev.Name,
        Description = ev.Description,
        Location = ev.Location,
        StartTime = ev.StartTime,
        EndTime = ev.EndTime,
        MaxCapacity = ev.MaxCapacity,
        RegisteredCount = ev.RegisteredCount,
        Status = ev.Status,
        CreatedAt = ev.CreatedAt
    });
})
.WithName("UpdateEvent")
.WithTags("Events");

// ==========================================
// 2. REGISTRATION ENDPOINTS
// ==========================================

// POST /events/{id}/registrations (Đăng ký tham dự với concurrency control)
app.MapPost("/events/{id:guid}/registrations", async (
    Guid id,
    RegisterParticipantRequest request,
    EventFlowDbContext db,
    IRabbitMqPublisher publisher,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { message = "Họ tên và email không được để trống" });
    }

    var cleanEmail = request.Email.Trim().ToLowerInvariant();
    var cleanPhone = request.PhoneNumber?.Trim() ?? string.Empty;
    var cleanName = request.FullName.Trim();

    const int maxRetries = 3;
    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
            {
                return Results.NotFound(new { message = $"Không tìm thấy sự kiện với mã {id}" });
            }

            if (ev.Status == EventStatus.Cancelled || ev.Status == EventStatus.Completed)
            {
                return Results.BadRequest(new { message = "Sự kiện này đã kết thúc hoặc bị hủy, không thể đăng ký." });
            }

            // Check capacity
            if (ev.RegisteredCount >= ev.MaxCapacity)
            {
                return Results.Conflict(new { message = "Sự kiện đã hết chỗ! Không thể tiếp nhận thêm đăng ký." });
            }

            // Find or create participant
            var participant = await db.Participants.FirstOrDefaultAsync(p => p.Email == cleanEmail);
            if (participant == null)
            {
                participant = new Participant
                {
                    Id = Guid.NewGuid(),
                    FullName = cleanName,
                    Email = cleanEmail,
                    PhoneNumber = cleanPhone,
                    CreatedAt = DateTime.UtcNow
                };
                db.Participants.Add(participant);
                await db.SaveChangesAsync();
            }
            else
            {
                // Update contact details if provided
                if (!string.IsNullOrEmpty(cleanPhone)) participant.PhoneNumber = cleanPhone;
                participant.FullName = cleanName;
            }

            // Check if participant already has active registration for this event
            var existingReg = await db.Registrations.FirstOrDefaultAsync(r =>
                r.EventId == id &&
                r.ParticipantId == participant.Id &&
                r.Status != RegistrationStatus.Cancelled);

            if (existingReg != null)
            {
                return Results.Conflict(new { 
                    message = $"Bạn đã đăng ký sự kiện này trước đó với mã vé: {existingReg.RegistrationCode}",
                    registrationCode = existingReg.RegistrationCode
                });
            }

            // Generate unique Registration Code
            string registrationCode;
            do
            {
                registrationCode = $"EVT-{Random.Shared.Next(100000, 999999)}";
            } while (await db.Registrations.AnyAsync(r => r.RegistrationCode == registrationCode));

            // Create registration
            var registration = new Registration
            {
                Id = Guid.NewGuid(),
                RegistrationCode = registrationCode,
                EventId = ev.Id,
                ParticipantId = participant.Id,
                Status = RegistrationStatus.Confirmed,
                RegisteredAt = DateTime.UtcNow
            };

            // Increment event registered count & update concurrency token
            ev.RegisteredCount += 1;
            ev.ConcurrencyToken = Guid.NewGuid();

            db.Registrations.Add(registration);
            await db.SaveChangesAsync();

            // Publish message to RabbitMQ Topic Exchange
            var registeredMessage = new ParticipantRegisteredMessage
            {
                EventId = ev.Id,
                EventName = ev.Name,
                RegistrationId = registration.Id,
                RegistrationCode = registration.RegistrationCode,
                ParticipantId = participant.Id,
                ParticipantName = participant.FullName,
                ParticipantEmail = participant.Email,
                RemainingCapacity = Math.Max(0, ev.MaxCapacity - ev.RegisteredCount),
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString("N")
            };

            await publisher.PublishAsync(RabbitMqConstants.RoutingKeyRegistered, registeredMessage);

            var result = new RegistrationDetailDto
            {
                Id = registration.Id,
                RegistrationCode = registration.RegistrationCode,
                EventId = ev.Id,
                EventName = ev.Name,
                EventDescription = ev.Description,
                EventLocation = ev.Location,
                EventStartTime = ev.StartTime,
                EventEndTime = ev.EndTime,
                ParticipantId = participant.Id,
                ParticipantName = participant.FullName,
                ParticipantEmail = participant.Email,
                ParticipantPhone = participant.PhoneNumber,
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt
            };

            return Results.Created($"/registrations/{registration.Id}", result);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict detected while registering for event {EventId}, retrying attempt {Attempt}...", id, retry + 1);
            db.ChangeTracker.Clear();
            if (retry == maxRetries - 1)
            {
                return Results.Conflict(new { message = "Hệ thống đang tiếp nhận lượng đăng ký lớn. Vui lòng thử lại sau giây lát." });
            }
        }
    }

    return Results.Conflict(new { message = "Không thể hoàn tất đăng ký lúc này. Vui lòng thử lại sau." });
})
.WithName("RegisterForEvent")
.WithTags("Registrations");

// DELETE /registrations/{id} (Hủy đăng ký)
app.MapDelete("/registrations/{id:guid}", async (
    Guid id,
    EventFlowDbContext db,
    IRabbitMqPublisher publisher) =>
{
    var registration = await db.Registrations
        .Include(r => r.Event)
        .Include(r => r.Participant)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (registration == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy thông tin đăng ký với mã {id}" });
    }

    if (registration.Status == RegistrationStatus.Cancelled)
    {
        return Results.BadRequest(new { message = "Lượt đăng ký này đã được hủy trước đó." });
    }

    if (registration.Status == RegistrationStatus.CheckedIn)
    {
        return Results.BadRequest(new { message = "Không thể hủy lượt đăng ký đã thực hiện check-in." });
    }

    registration.Status = RegistrationStatus.Cancelled;
    registration.CancelledAt = DateTime.UtcNow;

    // Restore spot in event
    if (registration.Event != null)
    {
        registration.Event.RegisteredCount = Math.Max(0, registration.Event.RegisteredCount - 1);
        registration.Event.ConcurrencyToken = Guid.NewGuid();
    }

    await db.SaveChangesAsync();

    // Publish ParticipantCancelled message
    var cancelledMsg = new ParticipantCancelledMessage
    {
        EventId = registration.EventId,
        EventName = registration.Event?.Name ?? "Event",
        RegistrationId = registration.Id,
        RegistrationCode = registration.RegistrationCode,
        ParticipantId = registration.ParticipantId,
        ParticipantName = registration.Participant?.FullName ?? "Participant",
        RemainingCapacity = registration.Event != null ? Math.Max(0, registration.Event.MaxCapacity - registration.Event.RegisteredCount) : 0,
        Timestamp = DateTime.UtcNow,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    await publisher.PublishAsync(RabbitMqConstants.RoutingKeyCancelled, cancelledMsg);

    return Results.Ok(new { 
        message = "Hủy đăng ký thành công. Chỗ trống đã được hoàn trả lại cho sự kiện.",
        registrationCode = registration.RegistrationCode
    });
})
.WithName("CancelRegistration")
.WithTags("Registrations");

// POST /registrations/{id}/checkin (Check-in theo ID)
app.MapPost("/registrations/{id:guid}/checkin", async (
    Guid id,
    EventFlowDbContext db,
    IRabbitMqPublisher publisher) =>
{
    var reg = await db.Registrations
        .Include(r => r.Event)
        .Include(r => r.Participant)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reg == null)
    {
        return Results.NotFound(new { message = "Không tìm thấy thông tin đăng ký" });
    }

    if (reg.Status == RegistrationStatus.Cancelled)
    {
        return Results.BadRequest(new { message = "Mã đăng ký này đã bị hủy, không thể check-in!" });
    }

    if (reg.Status == RegistrationStatus.CheckedIn)
    {
        return Results.Conflict(new CheckInResultDto
        {
            Success = false,
            Message = $"Người tham dự đã check-in trước đó vào lúc {reg.CheckedInAt:HH:mm:ss dd/MM/yyyy}!",
            RegistrationCode = reg.RegistrationCode,
            ParticipantName = reg.Participant?.FullName ?? string.Empty,
            EventName = reg.Event?.Name ?? string.Empty,
            CheckedInAt = reg.CheckedInAt,
            Status = reg.Status
        });
    }

    reg.Status = RegistrationStatus.CheckedIn;
    reg.CheckedInAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    // Publish ParticipantCheckedIn message
    var checkedInMsg = new ParticipantCheckedInMessage
    {
        EventId = reg.EventId,
        EventName = reg.Event?.Name ?? string.Empty,
        RegistrationId = reg.Id,
        RegistrationCode = reg.RegistrationCode,
        ParticipantId = reg.ParticipantId,
        ParticipantName = reg.Participant?.FullName ?? string.Empty,
        CheckedInAt = reg.CheckedInAt.Value,
        Timestamp = DateTime.UtcNow,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    await publisher.PublishAsync(RabbitMqConstants.RoutingKeyCheckedIn, checkedInMsg);

    return Results.Ok(new CheckInResultDto
    {
        Success = true,
        Message = "Check-in thành công! Chào mừng người tham dự đến với sự kiện.",
        RegistrationCode = reg.RegistrationCode,
        ParticipantName = reg.Participant?.FullName ?? string.Empty,
        EventName = reg.Event?.Name ?? string.Empty,
        CheckedInAt = reg.CheckedInAt,
        Status = reg.Status
    });
})
.WithName("CheckInRegistrationById")
.WithTags("CheckIn");

// POST /checkin (Check-in bằng Registration Code)
app.MapPost("/checkin", async (
    CheckInRequest request,
    EventFlowDbContext db,
    IRabbitMqPublisher publisher) =>
{
    if (string.IsNullOrWhiteSpace(request.RegistrationCode))
    {
        return Results.BadRequest(new { message = "Vui lòng nhập mã đăng ký (Registration Code)" });
    }

    var code = request.RegistrationCode.Trim().ToUpperInvariant();
    var reg = await db.Registrations
        .Include(r => r.Event)
        .Include(r => r.Participant)
        .FirstOrDefaultAsync(r => r.RegistrationCode == code);

    if (reg == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy thông tin đăng ký với mã '{code}'" });
    }

    if (reg.Status == RegistrationStatus.Cancelled)
    {
        return Results.BadRequest(new { message = "Vé đăng ký này đã bị hủy, không thể check-in!" });
    }

    if (reg.Status == RegistrationStatus.CheckedIn)
    {
        return Results.Conflict(new CheckInResultDto
        {
            Success = false,
            Message = $"Vé này đã check-in trước đó vào lúc {reg.CheckedInAt:HH:mm:ss dd/MM/yyyy}!",
            RegistrationCode = reg.RegistrationCode,
            ParticipantName = reg.Participant?.FullName ?? string.Empty,
            EventName = reg.Event?.Name ?? string.Empty,
            CheckedInAt = reg.CheckedInAt,
            Status = reg.Status
        });
    }

    reg.Status = RegistrationStatus.CheckedIn;
    reg.CheckedInAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var checkedInMsg = new ParticipantCheckedInMessage
    {
        EventId = reg.EventId,
        EventName = reg.Event?.Name ?? string.Empty,
        RegistrationId = reg.Id,
        RegistrationCode = reg.RegistrationCode,
        ParticipantId = reg.ParticipantId,
        ParticipantName = reg.Participant?.FullName ?? string.Empty,
        CheckedInAt = reg.CheckedInAt.Value,
        Timestamp = DateTime.UtcNow,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    await publisher.PublishAsync(RabbitMqConstants.RoutingKeyCheckedIn, checkedInMsg);

    return Results.Ok(new CheckInResultDto
    {
        Success = true,
        Message = "Check-in thành công! Chào mừng người tham dự.",
        RegistrationCode = reg.RegistrationCode,
        ParticipantName = reg.Participant?.FullName ?? string.Empty,
        EventName = reg.Event?.Name ?? string.Empty,
        CheckedInAt = reg.CheckedInAt,
        Status = reg.Status
    });
})
.WithName("CheckInByCode")
.WithTags("CheckIn");

// GET /events/{id}/registrations (Danh sách đăng ký của một sự kiện)
app.MapGet("/events/{id:guid}/registrations", async (Guid id, EventFlowDbContext db) =>
{
    var registrations = await db.Registrations
        .AsNoTracking()
        .Include(r => r.Event)
        .Include(r => r.Participant)
        .Where(r => r.EventId == id)
        .OrderByDescending(r => r.RegisteredAt)
        .Select(r => new RegistrationDto
        {
            Id = r.Id,
            RegistrationCode = r.RegistrationCode,
            EventId = r.EventId,
            EventName = r.Event.Name,
            ParticipantId = r.ParticipantId,
            ParticipantName = r.Participant.FullName,
            ParticipantEmail = r.Participant.Email,
            ParticipantPhone = r.Participant.PhoneNumber,
            Status = r.Status,
            RegisteredAt = r.RegisteredAt,
            CheckedInAt = r.CheckedInAt,
            CancelledAt = r.CancelledAt
        })
        .ToListAsync();

    return Results.Ok(registrations);
})
.WithName("GetRegistrationsByEventId")
.WithTags("Registrations");

// GET /registrations (Tất cả đăng ký với filter)
app.MapGet("/registrations", async (string? search, RegistrationStatus? status, EventFlowDbContext db) =>
{
    var query = db.Registrations
        .AsNoTracking()
        .Include(r => r.Event)
        .Include(r => r.Participant)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim().ToLower();
        query = query.Where(r => r.RegistrationCode.ToLower().Contains(s)
                              || r.Participant.FullName.ToLower().Contains(s)
                              || r.Participant.Email.ToLower().Contains(s)
                              || r.Event.Name.ToLower().Contains(s));
    }

    if (status.HasValue)
    {
        query = query.Where(r => r.Status == status.Value);
    }

    var list = await query
        .OrderByDescending(r => r.RegisteredAt)
        .Select(r => new RegistrationDto
        {
            Id = r.Id,
            RegistrationCode = r.RegistrationCode,
            EventId = r.EventId,
            EventName = r.Event.Name,
            ParticipantId = r.ParticipantId,
            ParticipantName = r.Participant.FullName,
            ParticipantEmail = r.Participant.Email,
            ParticipantPhone = r.Participant.PhoneNumber,
            Status = r.Status,
            RegisteredAt = r.RegisteredAt,
            CheckedInAt = r.CheckedInAt,
            CancelledAt = r.CancelledAt
        })
        .ToListAsync();

    return Results.Ok(list);
})
.WithName("GetAllRegistrations")
.WithTags("Registrations");

// GET /registrations/code/{code} (Tra cứu chi tiết theo Registration Code)
app.MapGet("/registrations/code/{code}", async (string code, EventFlowDbContext db) =>
{
    var c = code.Trim().ToUpperInvariant();
    var reg = await db.Registrations
        .AsNoTracking()
        .Include(r => r.Event)
        .Include(r => r.Participant)
        .FirstOrDefaultAsync(r => r.RegistrationCode == c);

    if (reg == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy thông tin đăng ký với mã '{c}'" });
    }

    var detail = new RegistrationDetailDto
    {
        Id = reg.Id,
        RegistrationCode = reg.RegistrationCode,
        EventId = reg.EventId,
        EventName = reg.Event?.Name ?? string.Empty,
        EventDescription = reg.Event?.Description ?? string.Empty,
        EventLocation = reg.Event?.Location ?? string.Empty,
        EventStartTime = reg.Event?.StartTime ?? DateTime.MinValue,
        EventEndTime = reg.Event?.EndTime ?? DateTime.MinValue,
        ParticipantId = reg.ParticipantId,
        ParticipantName = reg.Participant?.FullName ?? string.Empty,
        ParticipantEmail = reg.Participant?.Email ?? string.Empty,
        ParticipantPhone = reg.Participant?.PhoneNumber ?? string.Empty,
        Status = reg.Status,
        RegisteredAt = reg.RegisteredAt,
        CheckedInAt = reg.CheckedInAt,
        CancelledAt = reg.CancelledAt
    };

    return Results.Ok(detail);
})
.WithName("GetRegistrationByCode")
.WithTags("Registrations");

// GET /api/dashboard/stats (Thống kê tổng quan từ cơ sở dữ liệu)
app.MapGet("/api/dashboard/stats", async (EventFlowDbContext db) =>
{
    var totalEvents = await db.Events.CountAsync();
    var totalRegistrations = await db.Registrations.CountAsync(r => r.Status != RegistrationStatus.Cancelled);
    var totalCheckedIn = await db.Registrations.CountAsync(r => r.Status == RegistrationStatus.CheckedIn);
    var events = await db.Events.AsNoTracking().ToListAsync();
    var totalRemainingSpots = events.Sum(e => Math.Max(0, e.MaxCapacity - e.RegisteredCount));

    var stats = new DashboardStatsDto
    {
        TotalEvents = totalEvents,
        TotalRegistrations = totalRegistrations,
        TotalCheckedIn = totalCheckedIn,
        TotalRemainingSpots = totalRemainingSpots,
        LastUpdated = DateTime.UtcNow
    };

    return Results.Ok(stats);
})
.WithName("GetDashboardStatsFromDb")
.WithTags("Dashboard");

app.Run();

namespace EventFlow.EventApi
{
    public partial class Program { }
}
