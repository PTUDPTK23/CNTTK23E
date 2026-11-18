
using Microsoft.AspNetCore.SignalR.Client;

// Lấy URL máy chủ và tên máy khách từ tham số
var serverUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5200";

var name = args.Length > 1 ? args[1] : "Client";

// Bước 4: Xây dựng kết nối SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Bước 4: Đăng ký nhận EchoResponse
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));

// Bước 5: Đăng ký nhận BroadcastMessage
connection.On<string>("BroadcastMessage",
    m => Console.WriteLine($"[Broadcast] {m}"));

// Bước 5: Đăng ký nhận GroupMessage
connection.On<string>("GroupMessage",
    m => Console.WriteLine($"[Group] {m}"));


connection.On<string>("SystemMessage",
    m => Console.WriteLine($"[System] {m}"));


connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId =>
{
    Console.WriteLine(
        $"[Lifecycle] Da ket noi lai: {newId?[..8]}");

    return Task.CompletedTask;
};

connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// Mở kết nối
await connection.StartAsync();

Console.WriteLine($"{name} da ket noi den PingApi.");

// Bước 4: Gọi Echo
await connection.InvokeAsync(
    "Echo",
    $"Xin chao SignalR tu {name}");

// Bước 5: Tham gia nhóm
await connection.InvokeAsync(
    "JoinGroup",
    "lop-hoc-01");

// Bước 5: Gửi thông báo cho nhóm
await connection.InvokeAsync(
    "SendToGroup",
    "lop-hoc-01",
    $"Thong bao nhom tu {name}");

// Bước 5: Gửi thông báo cho tất cả
await connection.InvokeAsync(
    "Broadcast",
    $"Xin chao tat ca tu {name}");

Console.WriteLine(
    "Dang cho thong diep... Nhan Enter de thoat.");

Console.ReadLine();
