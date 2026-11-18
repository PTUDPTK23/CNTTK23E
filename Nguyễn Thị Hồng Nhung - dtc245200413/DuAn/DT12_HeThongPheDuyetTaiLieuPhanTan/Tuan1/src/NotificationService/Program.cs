using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Hubs;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSignalR();
builder.Services.AddHostedService<RabbitMqConsumerWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (int attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("NotificationDbContext initialized successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Notification database not ready yet (attempt {Attempt}/15): {Error}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "NotificationService",
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/notifications", async (string? recipient, bool? unreadOnly, NotificationDbContext db) =>
{
    var query = db.Notifications.AsQueryable();

    if (!string.IsNullOrWhiteSpace(recipient))
    {
        var r = recipient.Trim().ToLower();
        query = query.Where(n => n.Recipient.ToLower().Contains(r));
    }

    if (unreadOnly == true)
    {
        query = query.Where(n => !n.IsRead);
    }

    var list = await query
        .OrderByDescending(n => n.CreatedAt)
        .Take(100)
        .ToListAsync();

    return Results.Ok(list);
});

app.MapPost("/notifications/{id:guid}/read", async (Guid id, NotificationDbContext db) =>
{
    var item = await db.Notifications.FindAsync(id);
    if (item is null) return Results.NotFound();

    item.IsRead = true;
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

app.MapPost("/notifications/read-all", async (NotificationDbContext db) =>
{
    var unread = await db.Notifications.Where(n => !n.IsRead).ToListAsync();
    foreach (var n in unread)
    {
        n.IsRead = true;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Da danh dau tat ca thong bao la da doc." });
});

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
