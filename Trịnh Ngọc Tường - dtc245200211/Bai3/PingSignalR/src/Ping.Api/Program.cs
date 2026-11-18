
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Bước 2: Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Endpoint REST kiểm tra máy chủ
app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Bước 2: Ánh xạ Hub
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Bước 3 và 5: Khai báo PingHub
sealed class PingHub : Hub
{
    // Bước 3: Echo - trả lời riêng người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync(
            "EchoResponse",
            $"Echo: {message}");
    }

    // Bước 5: Broadcast - gửi cho tất cả máy khách
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync(
            "BroadcastMessage", message);
    }

    // Bước 5: JoinGroup - tham gia nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // Bước 5: SendToGroup - gửi tin trong nhóm
    public async Task SendToGroup(
        string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync(
            "GroupMessage", message);
    }


public override async Task OnConnectedAsync()
{
    Console.WriteLine(
        $"[Hub] + {Context.ConnectionId[..8]} ket noi");

    await Clients.All.SendAsync(
        "SystemMessage",
        $"Client {Context.ConnectionId[..8]} da ket noi.");

    await base.OnConnectedAsync();
}

public override async Task OnDisconnectedAsync(Exception? ex)
{
    Console.WriteLine(
        $"[Hub] - {Context.ConnectionId[..8]} ngat");

    await Clients.All.SendAsync(
        "SystemMessage",
        $"Client {Context.ConnectionId[..8]} da ngat ket noi.");

    await base.OnDisconnectedAsync(ex);
    }
}
