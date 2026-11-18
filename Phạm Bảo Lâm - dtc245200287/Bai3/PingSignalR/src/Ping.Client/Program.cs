using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Pham Bao Lam";

// Xây dựng kết nối tới hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Đăng ký hàm xử lý khi nhận thông điệp "EchoResponse" từ hub
connection.On<string>("EchoResponse", msg => Console.WriteLine($"[Echo] {msg}"));

// Mở kết nối (negotiate + WebSocket)
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");

// Gọi phương thức Echo trên hub
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");

// PHẦN MỚI THÊM: Gọi JoinGroup, SendToGroup và Broadcast
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

// Hàm Echo yêu cầu 1 tham số string, ta truyền sai hoặc để trống
await connection.InvokeAsync("Echo");

// Lắng nghe thông điệp định kỳ từ Background Service của server
connection.On<string>("ReceiveTick", tickMsg => Console.WriteLine(tickMsg));

// Đăng ký sự kiện xử lý mất kết nối và tự động kết nối lại
connection.Reconnecting += error =>
{
    Console.WriteLine($"Dang ket noi lai... Loi: {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"Da ket noi lai thanh cong! ConnectionId: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    Console.WriteLine($"Ket noi da bi dong hoàn toàn. Loi: {error?.Message}");
    return Task.CompletedTask;
};

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();