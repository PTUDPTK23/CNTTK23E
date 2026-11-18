using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Ánh xạ Hub SignalR
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
}
