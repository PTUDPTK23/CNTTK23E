using System;
using EventFlow.DashboardService.Hubs;
using EventFlow.DashboardService.Services;
using EventFlow.Shared.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Thêm HttpClient
builder.Services.AddHttpClient();

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

// Đăng ký Dashboard State Service và Consumer nền RabbitMQ
builder.Services.AddSingleton<IDashboardStateService, DashboardStateService>();
builder.Services.AddHostedService<DashboardRabbitMqConsumer>();

// Cấu hình CORS cho kết nối SignalR Web/Client
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

// Endpoint kiểm tra sức khỏe dịch vụ
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "Healthy", 
    service = "EventFlow.DashboardService", 
    signalRHub = "/hubs/dashboard",
    timestamp = DateTime.UtcNow 
}));

// REST API lấy số liệu dashboard hiện tại
app.MapGet("/api/dashboard/stats", (IDashboardStateService stateService) =>
{
    var stats = stateService.GetCurrentStats();
    return Results.Ok(stats);
});

// Endpoint nhận thông báo đăng ký từ EventApi và phát sóng qua SignalR Hub
app.MapPost("/api/dashboard/notify/registration", async (ParticipantRegisteredMessage message, IDashboardStateService stateService) =>
{
    await stateService.HandleParticipantRegisteredAsync(message);
    return Results.Ok(new { success = true, message = "Đã phát sóng thông báo đăng ký qua SignalR Hub." });
});

// Endpoint nhận thông báo check-in từ EventApi và phát sóng qua SignalR Hub
app.MapPost("/api/dashboard/notify/checkin", async (ParticipantCheckedInMessage message, IDashboardStateService stateService) =>
{
    await stateService.HandleParticipantCheckedInAsync(message);
    return Results.Ok(new { success = true, message = "Đã phát sóng thông báo check-in qua SignalR Hub." });
});

// Endpoint nhận thông báo hủy vé từ EventApi và phát sóng qua SignalR Hub
app.MapPost("/api/dashboard/notify/cancel", async (ParticipantCancelledMessage message, IDashboardStateService stateService) =>
{
    await stateService.HandleParticipantCancelledAsync(message);
    return Results.Ok(new { success = true, message = "Đã phát sóng thông báo hủy vé qua SignalR Hub." });
});

// Endpoint mô phỏng phát sóng thời gian thực (SignalR Simulation - Bài 3)
app.MapPost("/api/dashboard/simulate", async (IDashboardStateService stateService) =>
{
    var activity = await stateService.SimulateEventActivityAsync("Vietnam Tech Summit 2026", "Phan Quang Tùng", "Register");
    return Results.Ok(new 
    { 
        success = true, 
        message = "Đã mô phỏng hoạt động và phát sóng SignalR thành công!", 
        activity 
    });
});

// Đăng ký SignalR Hub endpoint tại đường dẫn /hubs/dashboard
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();

namespace EventFlow.DashboardService
{
    public partial class Program { }
}
