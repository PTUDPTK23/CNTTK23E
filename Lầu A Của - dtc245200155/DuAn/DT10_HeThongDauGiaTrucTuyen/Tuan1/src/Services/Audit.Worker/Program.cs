using Audit.Worker.Consumers;
using Audit.Worker.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure port
var port = builder.Configuration.GetValue<int>("PORT", 5004);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

// Configure Database (PostgreSQL or SQLite)
var databaseType = builder.Configuration.GetValue<string>("DatabaseType", "Sqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? (databaseType.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) 
                           ? "Host=db;Port=5432;Database=audit_db;Username=postgres;Password=postgres" 
                           : "Data Source=audit.db");

builder.Services.AddDbContext<AuditDbContext>(options =>
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

builder.Services.AddHostedService<RabbitMqAuditConsumer>();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("AllowAll");
app.MapHealthChecks("/health");

// API tra cứu audit logs
app.MapGet("/api/audit-logs", async (string? auctionId, string? eventType, int limit, AuditDbContext db) =>
{
    var take = limit > 0 ? Math.Min(limit, 100) : 50;
    var query = db.AuditEntries.AsQueryable();

    if (!string.IsNullOrWhiteSpace(auctionId))
    {
        query = query.Where(a => a.AuctionId == auctionId);
    }

    if (!string.IsNullOrWhiteSpace(eventType))
    {
        query = query.Where(a => a.EventType == eventType);
    }

    var logs = await query
        .OrderByDescending(a => a.Timestamp)
        .Take(take)
        .ToListAsync();

    return Results.Ok(logs);
});

app.MapGet("/", () => "Audit Worker & History API is running.");

app.Run();
