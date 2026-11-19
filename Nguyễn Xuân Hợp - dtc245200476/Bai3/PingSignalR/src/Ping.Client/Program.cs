using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("========================================");
Console.WriteLine("       PING SIGNALR CLIENT");
Console.WriteLine("========================================");

var hubUrl = "http://localhost:5200/hubs/ping";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[] {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    })
    .Build();

// Đăng ký nhận sự kiện từ Hub
connection.On<string>("SystemMessage", message =>
{
    Console.WriteLine($"[Nhận] {message}");
});

connection.On<string>("EchoResponse", message =>
{
    Console.WriteLine($"[Nhận] {message}");
});

connection.On<string>("BroadcastMessage", message =>
{
    Console.WriteLine($"[Nhận] {message}");
});

connection.On<string, string>("GroupMessage", (group, message) =>
{
    Console.WriteLine($"[Nhận][Nhóm {group}] {message}");
});

// Sự kiện vòng đời kết nối
connection.Reconnecting += error =>
{
    Console.WriteLine($"[Mạng] Mất kết nối, đang thử kết nối lại... ({error?.Message})");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"[Mạng] Đã kết nối lại thành công! New ConnectionId: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    Console.WriteLine($"[Mạng] Kết nối đã đóng. ({error?.Message})");
    return Task.CompletedTask;
};

try
{
    Console.WriteLine($"[Client] Đang kết nối tới {hubUrl}...");
    await connection.StartAsync();
    Console.WriteLine($"[Client] Đã kết nối thành công! ConnectionId: {connection.ConnectionId}");
    Console.WriteLine("----------------------------------------");

    // Thực nghiệm 1: Gửi Echo
    Console.WriteLine("[Thực nghiệm 1] Gửi Echo: 'Hello SignalR!'");
    await connection.InvokeAsync("Echo", "Hello SignalR!");
    await Task.Delay(1000);

    // Thực nghiệm 2: Gửi Broadcast
    Console.WriteLine("[Thực nghiệm 2] Gửi Broadcast: 'Chào cả lớp CNTTK23E!'");
    await connection.InvokeAsync("Broadcast", "Chào cả lớp CNTTK23E!");
    await Task.Delay(1000);

    // Thực nghiệm 3: Tham gia Group và gửi tin nhắn nhóm
    var groupName = "CNTTK23E";
    Console.WriteLine($"[Thực nghiệm 3] Tham gia nhóm: {groupName}");
    await connection.InvokeAsync("JoinGroup", groupName);
    await Task.Delay(500);

    Console.WriteLine($"[Thực nghiệm 3] Gửi tin nhắn vào nhóm {groupName}: 'Thành viên Nguyễn Xuân Hợp điểm danh!'");
    await connection.InvokeAsync("SendToGroup", groupName, "Thành viên Nguyễn Xuân Hợp điểm danh!");
    await Task.Delay(1000);

    Console.WriteLine("----------------------------------------");
    Console.WriteLine("[Client] Hoàn thành các thực nghiệm tự động.");
}
catch (Exception ex)
{
    Console.WriteLine($"[Lỗi] Không thể kết nối hoặc gửi tin: {ex.Message}");
}
