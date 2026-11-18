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
    public async Task Echo(string message)
{
await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");

}   // bước 3 thêm phương thức
public async Task Broadcast(string message)
=> await Clients.All.SendAsync("BroadcastMessage", message);
public async Task JoinGroup(string groupName)
{
await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
await Clients.Group(groupName).SendAsync("GroupMessage",
$"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
}
public async Task SendToGroup(string groupName, string message)
=> await Clients.Group(groupName).SendAsync("GroupMessage", message);
}