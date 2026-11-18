using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Trang chủ để kiểm tra máy chủ
app.MapGet("/", () =>
    new { service = "PingApi", status = "running" });

// Đăng ký Hub SignalR
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Hub SignalR
sealed class PingHub : Hub
{
    // Trả lời riêng người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Gửi cho tất cả máy khách
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    // Tham gia nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId} da tham gia nhom {groupName}");
    }

    // Gửi tin nhắn cho nhóm
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
    }
    // Khi một máy khách kết nối
public override async Task OnConnectedAsync()
{
    Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} ket noi");

    await Clients.All.SendAsync(
        "SystemMessage",
        $"{Context.ConnectionId[..8]} da ket noi.");

    await base.OnConnectedAsync();
}

// Khi một máy khách ngắt kết nối
public override async Task OnDisconnectedAsync(Exception? ex)
{
    Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} ngat");

    await Clients.All.SendAsync(
        "SystemMessage",
        $"{Context.ConnectionId[..8]} da ngat ket noi.");

    await base.OnDisconnectedAsync(ex);
}
}