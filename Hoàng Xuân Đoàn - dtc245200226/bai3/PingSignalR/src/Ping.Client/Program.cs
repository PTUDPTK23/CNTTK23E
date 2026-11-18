using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

// Xây dựng kết nối và bật cơ chế tự động kết nối lại
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")// Cố ý đổi sai đường dẫn để lấy lỗi 404
    .WithAutomaticReconnect()
    .Build();

// Đăng ký nghe các thông điệp
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));

connection.On<string>("BroadcastMessage",
    m => Console.WriteLine($"[Broadcast] {m}"));

connection.On<string>("GroupMessage",
    m => Console.WriteLine($"[Group] {m}"));

// Lắng nghe hệ thống và vòng đời
connection.On<string>("SystemMessage",
    m => Console.WriteLine($"[System] {m}"));

connection.Reconnecting += _ => {
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId => {
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

connection.Closed += _ => {
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// Mở kết nối
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");

await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();