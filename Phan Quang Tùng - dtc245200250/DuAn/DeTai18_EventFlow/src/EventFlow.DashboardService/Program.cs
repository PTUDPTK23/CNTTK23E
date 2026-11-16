using System;
using EventFlow.DashboardService.Hubs;
using EventFlow.DashboardService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add HttpClient
builder.Services.AddHttpClient();

// Add SignalR
builder.Services.AddSignalR();

// Register Dashboard State Service and Consumer
builder.Services.AddSingleton<IDashboardStateService, DashboardStateService>();
builder.Services.AddHostedService<DashboardRabbitMqConsumer>();

// Configure CORS for SignalR web client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "EventFlow.DashboardService", timestamp = DateTime.UtcNow }));

// REST API for current dashboard metrics
app.MapGet("/api/dashboard/stats", (IDashboardStateService stateService) =>
{
    var stats = stateService.GetCurrentStats();
    return Results.Ok(stats);
});

// SignalR Hub endpoint
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();

namespace EventFlow.DashboardService
{
    public partial class Program { }
}
