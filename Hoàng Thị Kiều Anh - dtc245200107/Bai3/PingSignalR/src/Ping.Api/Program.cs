
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký SignalR
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    // Bước 3: Echo
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync(
            "EchoResponse", $"Echo: {message}");
    }

    // Bước 5: Broadcast
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync(
            "BroadcastMessage", message);
    }

    // Bước 5: Tham gia nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // Bước 5: Gửi tin cho nhóm
    public async Task SendToGroup(
        string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync(
            "GroupMessage", message);
    }

    // Bước 6: Khi máy khách kết nối
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"[Hub] + {Context.ConnectionId[..8]} ket noi");

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi.");

        await base.OnConnectedAsync();
    }

    // Bước 6: Khi máy khách ngắt kết nối
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
