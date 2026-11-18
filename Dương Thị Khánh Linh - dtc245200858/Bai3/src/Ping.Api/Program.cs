
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// API kiểm tra server
app.MapGet("/", () => "Ping SignalR API is running!");

// Tạo endpoint SignalR
app.MapHub<PingHub>("/pingHub");

app.Run("http://localhost:5200");

// Hub nhận và gửi thông điệp
public class PingHub : Hub
{
    public async Task SendPing(string message)
    {
        await Clients.All.SendAsync("ReceivePing", message);
    }
}
