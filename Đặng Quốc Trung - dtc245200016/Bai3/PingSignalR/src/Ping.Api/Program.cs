using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    public Task Echo(string message)
    {
        return Clients.Caller.SendAsync(
            "EchoResponse", $"Echo: {message}");
    }

    public Task Broadcast(string message)
    {
        return Clients.All.SendAsync(
            "BroadcastMessage", message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    public Task SendToGroup(string groupName, string message)
    {
        return Clients.Group(groupName)
            .SendAsync("GroupMessage", message);
    }

    // Buoc 6: Khi may khach ket noi
    public override async Task OnConnectedAsync()
    {
        var id = Context.ConnectionId[..8];

        Console.WriteLine($"[Hub] + {id} ket noi");

        await Clients.All.SendAsync(
            "SystemMessage", $"Client {id} da ket noi.");

        await base.OnConnectedAsync();
    }

    // Buoc 6: Khi may khach ngat ket noi
    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var id = Context.ConnectionId[..8];

        Console.WriteLine($"[Hub] - {id} ngat");

        await Clients.All.SendAsync(
            "SystemMessage", $"Client {id} da ngat ket noi.");

        await base.OnDisconnectedAsync(ex);
    }
}
