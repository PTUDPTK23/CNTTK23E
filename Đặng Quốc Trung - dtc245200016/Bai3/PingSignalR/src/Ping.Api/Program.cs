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
    // Gui rieng cho nguoi goi
    public Task Echo(string message)
    {
        return Clients.Caller.SendAsync(
            "EchoResponse", $"Echo: {message}");
    }

    // Gui cho tat ca may khach
    public Task Broadcast(string message)
    {
        return Clients.All.SendAsync(
            "BroadcastMessage", message);
    }

    // Tham gia nhom
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, groupName);

        string message =
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}";

        await Clients.Group(groupName)
            .SendAsync("GroupMessage", message);
    }

    // Gui thong diep den nhom
    public Task SendToGroup(string groupName, string message)
    {
        return Clients.Group(groupName)
            .SendAsync("GroupMessage", message);
    }
}
