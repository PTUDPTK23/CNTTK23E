using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR vao DI
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "PingApi",
    status = "running"
}));

// Anh xa hub vao tuyen /hubs/ping
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var shortId = Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)];
        Console.WriteLine($"[Hub] + {shortId} ket noi");
        await Clients.All.SendAsync("SystemMessage", $"Client {shortId} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var shortId = Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)];
        Console.WriteLine($"[Hub] - {shortId} ngat");
        await Clients.All.SendAsync("SystemMessage", $"Client {shortId} da ngat ket noi.");
        await base.OnDisconnectedAsync(exception);
    }

    // May khach goi Echo -> tra loi RIENG nguoi goi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Phat rong cho moi ket noi dang mo
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    // Vao nhom va bao cho thanh vien nhom
    public async Task JoinGroup(string groupName)
    {
        var shortId = Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)];
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{shortId} da tham gia nhom {groupName}");
    }

    // Gui thong diep toi thanh vien nhom
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
    }
}
