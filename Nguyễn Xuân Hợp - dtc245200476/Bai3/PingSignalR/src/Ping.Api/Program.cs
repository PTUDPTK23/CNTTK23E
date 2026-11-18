using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Thiết lập cổng 5200 theo yêu cầu bài giảng
builder.WebHost.UseUrls("http://localhost:5200");

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();

// Endpoint GET / kiểm tra trạng thái
app.MapGet("/", () => Results.Ok(new
{
    service = "PingApi",
    status = "running",
    hub = "/hubs/ping",
    timestamp = DateTime.UtcNow
}));

// Ánh xạ SignalR Hub
app.MapHub<PingHub>("/hubs/ping");

Console.WriteLine("========================================");
Console.WriteLine("   PING SIGNALR SERVER ĐANG CHẠY");
Console.WriteLine("   URL: http://localhost:5200");
Console.WriteLine("   Hub: http://localhost:5200/hubs/ping");
Console.WriteLine("========================================");

app.Run();

public class PingHub : Hub
{
    private readonly ILogger<PingHub> _logger;

    public PingHub(ILogger<PingHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client đã kết nối: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("SystemMessage", $"[Hệ thống] Kết nối thành công! ConnectionId: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client đã ngắt kết nối: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Echo(string message)
    {
        _logger.LogInformation("[Echo] Nhận từ {ConnectionId}: {Message}", Context.ConnectionId, message);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.Caller.SendAsync("EchoResponse", $"[Server Echo] {message} (lúc {timestamp})");
    }

    public async Task Broadcast(string message)
    {
        _logger.LogInformation("[Broadcast] Nhận từ {ConnectionId}: {Message}", Context.ConnectionId, message);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("BroadcastMessage", $"[Broadcast từ {Context.ConnectionId}] {message} (lúc {timestamp})");
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("[Group] {ConnectionId} đã tham gia nhóm {GroupName}", Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("SystemMessage", $"[Nhóm {groupName}] Thành viên {Context.ConnectionId} đã tham gia.");
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("[Group] {ConnectionId} đã rời nhóm {GroupName}", Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("SystemMessage", $"[Nhóm {groupName}] Thành viên {Context.ConnectionId} đã rời nhóm.");
    }

    public async Task SendToGroup(string groupName, string message)
    {
        _logger.LogInformation("[Group {GroupName}] Nhận từ {ConnectionId}: {Message}", groupName, Context.ConnectionId, message);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.Group(groupName).SendAsync("GroupMessage", groupName, $"[{groupName}][{Context.ConnectionId}]: {message} (lúc {timestamp})");
    }
}
