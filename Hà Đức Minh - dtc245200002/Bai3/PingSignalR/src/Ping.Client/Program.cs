
using Microsoft.AspNetCore.SignalR.Client;

// Lay URL server tu tham so dong lenh
var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

// Lay ten client
var name = args.Length > 1
    ? args[1]
    : "Client";

// Tao ket noi SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/ping")
    .WithAutomaticReconnect()
    .Build();

// =====================================================
// DANG KY CAC SU KIEN NHAN TU SERVER
// =====================================================

// Nhan phan hoi Echo
connection.On<string>("EchoResponse", message =>
{
    Console.WriteLine($"[Echo] {message}");
});

// Nhan Broadcast
connection.On<string>("BroadcastMessage", message =>
{
    Console.WriteLine($"[Broadcast] {message}");
});

// Nhan tin nhan nhom
connection.On<string>("GroupMessage", message =>
{
    Console.WriteLine($"[Group] {message}");
});

// Nhan thong bao ket noi
connection.On<string>("SystemMessage", message =>
{
    Console.WriteLine($"[System] {message}");
});

// =====================================================
// CAC SU KIEN VONG DOI KET NOI
// =====================================================

// Khi SignalR bat dau thu ket noi lai
connection.Reconnecting += error =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

// Khi ket noi lai thanh cong
connection.Reconnected += async newConnectionId =>
{
    Console.WriteLine(
        $"[Lifecycle] Da ket noi lai: {newConnectionId?[..8]}"
    );

    // Ket noi moi phai tham gia lai nhom
    await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
};

// Khi ket noi dong hoan toan
connection.Closed += error =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// =====================================================
// BAT DAU KET NOI
// =====================================================

await connection.StartAsync();

Console.WriteLine($"Da ket noi SignalR: {name}");
Console.WriteLine($"ConnectionId: {connection.ConnectionId}");

// =====================================================
// GOI CAC PHUONG THUC TREN HUB
// =====================================================

// 1. Goi Echo
await connection.InvokeAsync(
    "Echo",
    $"Xin chao tu {name}"
);

// 2. Tham gia nhom
await connection.InvokeAsync(
    "JoinGroup",
    "lop-hoc-01"
);

// 3. Gui tin nhan vao nhom
await connection.InvokeAsync(
    "SendToGroup",
    "lop-hoc-01",
    $"Thong bao nhom tu {name}"
);

// 4. Gui tin nhan den tat ca client
await connection.InvokeAsync(
    "Broadcast",
    $"Xin chao tat ca tu {name}"
);

// =====================================================
// GIU CHUONG TRINH HOAT DONG
// =====================================================

Console.WriteLine();
Console.WriteLine("Nhan Enter de thoat.");
Console.ReadLine();
