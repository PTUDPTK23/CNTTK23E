using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

// ① Xây dựng kết nối tới hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// ② Đăng ký hàm xử lý cho từng TÊN thông điệp
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));
connection.On<string>("BroadcastMessage",
    m => Console.WriteLine($"[Broadcast] {m}"));
connection.On<string>("GroupMessage",
    m => Console.WriteLine($"[Group] {m}"));

// ③ Mở kết nối: negotiate + chọn WebSocket
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");

// Gọi phương thức Echo trên hub
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");

// Lời gọi nhóm và broadcast
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
