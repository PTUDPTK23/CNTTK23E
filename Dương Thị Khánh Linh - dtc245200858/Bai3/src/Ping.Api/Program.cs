
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => "Ping SignalR API is running!");

app.MapHub<PingHub>("/pingHub");

app.Run("http://localhost:5200");

public class PingHub : Hub
{
    // Gửi tin nhắn đến tất cả Client
    public async Task SendPing(string message)
    {
        await Clients.All.SendAsync("ReceivePing", message);
    }
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync(
        "EchoResponse",
        $"Echo: {message}"
    );
    }

    // Tham gia một nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName
        );

        await Clients.Caller.SendAsync(
            "ReceivePing",
            $"Bạn đã tham gia nhóm: {groupName}"
        );
    }

    // Rời khỏi nhóm
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            groupName
        );

        await Clients.Caller.SendAsync(
            "ReceivePing",
            $"Bạn đã rời nhóm: {groupName}"
        );
    }

    // Gửi tin nhắn đến các Client trong nhóm
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync(
            "ReceivePing",
            $"[Nhóm {groupName}] {message}"
        );
    }
}
