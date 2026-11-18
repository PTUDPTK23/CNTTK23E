using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1]: "Client";

// Xây dựng kết nối tới hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Đăng ký hàm xử lý cho từng TÊN thông điệp (Phải khớp với tên ở máy chủ)
connection.On<string>("EchoResponse", 
    msg => Console.WriteLine($"[Echo] {msg}"));

// Mở kết nối
connection.On<string>("BroadcastMessage", 
    m => Console.WriteLine($"[Broadcast] {m}"));
connection.On<string>("GroupMessage", 
    m => Console.WriteLine($"[Group] {m}"));
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


// Gọi phương thức Echo trên hub
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

// Giữ chương trình sống để nhận thông điệp
Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
