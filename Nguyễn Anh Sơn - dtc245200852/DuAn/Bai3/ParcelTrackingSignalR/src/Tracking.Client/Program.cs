using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var trackingCode = args.Length > 1 ? args[1] : "VN20260000852";

// 1. Xây dựng kết nối tới hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/tracking")
    .WithAutomaticReconnect()
    .Build();

// 2. Đăng ký hàm xử lý cho tên thông điệp trả về
connection.On<string>("TrackingResponse",
    msg => Console.WriteLine($"[Tracking] {msg}"));

// 3. Mở kết nối
await connection.StartAsync();
Console.WriteLine($"Da ket noi den TrackingApi. Dang tra cuu: {trackingCode}");

// 4. Gọi phương thức trên hub
await connection.InvokeAsync("TrackParcel", trackingCode);

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();