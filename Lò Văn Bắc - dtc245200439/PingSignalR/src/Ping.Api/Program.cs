using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "Ping.Api", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");

app.Lifetime.ApplicationStarted.Register(() =>
    Console.WriteLine("Ping API listening. Hub: /hubs/ping"));

app.Run();

public sealed class PingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Disconnected: {Context.ConnectionId}; {exception?.Message}");
        await base.OnDisconnectedAsync(exception);
    }

    public Task<string> Echo(string message)
    {
        var reply = $"Echo: {message}";
        return Task.FromResult(reply);
    }

    public Task Broadcast(string user, string message)
    {
        return Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("SystemMessage", $"Joined group: {groupName}");
    }

    public Task SendToGroup(string groupName, string user, string message)
    {
        return Clients.Group(groupName).SendAsync("ReceiveMessage", user, message);
    }
}
