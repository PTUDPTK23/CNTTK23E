using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5200";

var name = args.Length > 1 ? args[1] : "Client";

// Xây dựng kết nối tới Hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Đăng ký nhận thông điệp EchoResponse
connection.On<string>("EchoResponse", message =>
{
    Console.WriteLine($"[Echo] {message}");
});

// Mở kết nối
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");

// Gọi phương thức Echo trên máy chủ
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
