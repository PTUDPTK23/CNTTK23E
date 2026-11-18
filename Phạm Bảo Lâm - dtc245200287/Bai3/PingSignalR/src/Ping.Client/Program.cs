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

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();