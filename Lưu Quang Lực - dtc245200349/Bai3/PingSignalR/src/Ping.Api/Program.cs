using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");
app.Run();

sealed class PingHub : Hub
{
    public Task Broadcast(string message) => Clients.All.SendAsync("BroadcastMessage", message);
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage", $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }
    public Task SendToGroup(string groupName, string message) => Clients.Group(groupName).SendAsync("GroupMessage", message);
    public Task Echo(string message) => Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
}
