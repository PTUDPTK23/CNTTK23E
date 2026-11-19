using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");
app.Run();

sealed class PingHub : Hub
{
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync(
            "GroupMessage", $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
    }

    public override async Task OnConnectedAsync()
    {
        var shortId = Context.ConnectionId[..8];
        Console.WriteLine($"[Hub] + {shortId} ket noi");
        await Clients.All.SendAsync("SystemMessage", $"Client {shortId} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var shortId = Context.ConnectionId.Length >= 8
            ? Context.ConnectionId[..8] : Context.ConnectionId;
        Console.WriteLine($"[Hub] - {shortId} ngat");
        await Clients.All.SendAsync("SystemMessage", $"Client {shortId} da ngat ket noi.");
        await base.OnDisconnectedAsync(exception);
    }
}
