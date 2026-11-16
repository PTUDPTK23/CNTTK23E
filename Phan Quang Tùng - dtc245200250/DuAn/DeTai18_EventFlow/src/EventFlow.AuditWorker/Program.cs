using System;
using System.Linq;
using EventFlow.AuditWorker.Data;
using EventFlow.AuditWorker.Services;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
                           ? builder.Configuration.GetConnectionString("PostgresConnection")
                           : "Data Source=eventflow_audit.db");

builder.Services.AddDbContext<AuditDbContext>(options =>
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

builder.Services.AddHostedService<AuditRabbitMqConsumer>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

// Auto create database table
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Ensuring Audit database schema exists using {Provider}...", dbProvider);
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation("Audit database schema is ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Audit database.");
    }
}

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "EventFlow.AuditWorker", timestamp = DateTime.UtcNow }));

// GET /api/audit/logs
app.MapGet("/api/audit/logs", async (
    string? eventType,
    string? search,
    int? limit,
    AuditDbContext db) =>
{
    var query = db.AuditLogs.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(eventType))
    {
        query = query.Where(a => a.EventType == eventType);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim().ToLower();
        query = query.Where(a => a.EventName.ToLower().Contains(s)
                              || (a.ParticipantName != null && a.ParticipantName.ToLower().Contains(s))
                              || (a.RegistrationCode != null && a.RegistrationCode.ToLower().Contains(s))
                              || a.CorrelationId.ToLower().Contains(s));
    }

    var take = Math.Clamp(limit ?? 50, 1, 200);

    var logs = await query
        .OrderByDescending(a => a.Timestamp)
        .Take(take)
        .Select(a => new AuditLogDto
        {
            Id = a.Id,
            EventId = a.EventId,
            EventName = a.EventName,
            RegistrationId = a.RegistrationId,
            RegistrationCode = a.RegistrationCode,
            ParticipantId = a.ParticipantId,
            ParticipantName = a.ParticipantName,
            EventType = a.EventType,
            Timestamp = a.Timestamp,
            CorrelationId = a.CorrelationId,
            PayloadJson = a.PayloadJson
        })
        .ToListAsync();

    return Results.Ok(logs);
});

app.Run();

namespace EventFlow.AuditWorker
{
    public partial class Program { }
}
