using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
// Việc 1: đăng ký dịch vụ SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Việc 2: ánh xạ hub vào tuyến /hubs/ping
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    // Bước 3: Phương thức Echo (Caller)
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Bước 5: Broadcast (All)
    public async Task Broadcast(string message)
        => await Clients.All.SendAsync("BroadcastMessage", message);

    // Bước 5: JoinGroup (Group)
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // Bước 5: SendToGroup (Group)
    public async Task SendToGroup(string groupName, string message)
        => await Clients.Group(groupName).SendAsync("GroupMessage", message);

    // Bước 6: Vòng đời kết nối
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {Context.ConnectionId[..8]} ket noi");
        await Clients.All.SendAsync("SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine($"[Hub] - {Context.ConnectionId[..8]} ngat");
        await Clients.All.SendAsync("SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ngat ket noi.");
        await base.OnDisconnectedAsync(ex);
    }
}
