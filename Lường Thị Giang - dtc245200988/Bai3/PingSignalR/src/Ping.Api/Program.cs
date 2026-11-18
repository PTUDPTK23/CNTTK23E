using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
// Việc 1: đăng ký dịch vụ SignalR vào DI (bắt buộc đặt TRƯỚC builder.Build)
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));

// Việc 2: gắn đường dẫn hub /hubs/ping
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    // 1. Máy khách gọi Echo -> trả lời RIÊNG người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // 2. Phát rộng tới TẤT CẢ kết nối đang mở
    public async Task Broadcast(string message)
        => await Clients.All.SendAsync("BroadcastMessage", message);

    // 3. Thêm kết nối vào nhóm và thông báo cho nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // 4. Gửi thông điệp tới riêng một nhóm
    public async Task SendToGroup(string groupName, string message)
        => await Clients.Group(groupName).SendAsync("GroupMessage", message);

    // 5. Vòng đời kết nối
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
