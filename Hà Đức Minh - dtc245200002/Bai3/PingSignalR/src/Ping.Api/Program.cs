
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Trang kiem tra server
app.MapGet("/", () => "Ping SignalR Server is running!");

// Dang ky Hub
app.MapHub<PingHub>("/ping");

// Chay server tai cong 5000
app.Run("http://localhost:5000");

// =====================================================
// PING HUB
// =====================================================

public class PingHub : Hub
{
    // 1. Echo: Chi phan hoi client dang goi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", message);
    }

    // 2. Broadcast: Gui den tat ca client
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    // 3. JoinGroup: Them client vao nhom
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName
        );

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}"
        );
    }

    // 4. SendToGroup: Gui tin nhan den nhom
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            message
        );
    }

    // 5. Khi client ket noi
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"[Hub] + {Context.ConnectionId[..8]} ket noi"
        );

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi."
        );

        await base.OnConnectedAsync();
    }

    // 6. Khi client ngat ket noi
    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine(
            $"[Hub] - {Context.ConnectionId[..8]} ngat"
        );

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ngat ket noi."
        );

        await base.OnDisconnectedAsync(ex);
    }
}
