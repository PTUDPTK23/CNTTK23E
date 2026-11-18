using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/')
                                : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Nhận Echo riêng
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));

// Nhận tin nhắn broadcast
connection.On<string>("BroadcastMessage",
    msg => Console.WriteLine($"[Broadcast] {msg}"));

// Nhận tin nhắn nhóm
connection.On<string>("GroupMessage",
    msg => Console.WriteLine($"[Group] {msg}"));

// Kết nối máy chủ
await connection.StartAsync();
// Nhận thông báo kết nối / ngắt kết nối từ Server
connection.On<string>("SystemMessage",
    msg => Console.WriteLine($"[System] {msg}"));

// Khi bắt đầu kết nối lại
connection.Reconnecting += ex =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

// Khi kết nối lại thành công
connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId}");
    return Task.CompletedTask;
};

// Khi không thể kết nối lại
connection.Closed += ex =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

Console.WriteLine($"{name} da ket noi den PingApi.");

// Gọi Echo
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");

// Tham gia nhóm lớp
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");

// Gửi tin nhắn nhóm
await connection.InvokeAsync(
    "SendToGroup",
    "lop-hoc-01",
    $"Thong bao nhom tu {name}");

// Gửi tin nhắn cho tất cả
await connection.InvokeAsync(
    "Broadcast",
    $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();