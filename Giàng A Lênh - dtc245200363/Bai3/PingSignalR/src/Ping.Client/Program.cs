using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

// 1. Xay dung ket noi toi hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// 2. Dang ky ham xu ly cho tung TEN thong diep truoc khi StartAsync
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));
connection.On<string>("BroadcastMessage",
    msg => Console.WriteLine($"[Broadcast] {msg}"));
connection.On<string>("GroupMessage",
    msg => Console.WriteLine($"[Group] {msg}"));
connection.On<string>("SystemMessage",
    msg => Console.WriteLine($"[System] {msg}"));

// Vong doi ket noi
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId =>
{
    var shortId = newId is { Length: >= 8 } ? newId[..8] : newId;
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {shortId}");
    return Task.CompletedTask;
};

connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// 3. Mo ket noi: negotiate + chon WebSocket
await connection.StartAsync();
var connShortId = connection.ConnectionId is { Length: >= 8 }
    ? connection.ConnectionId[..8]
    : connection.ConnectionId;
Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connShortId}");

// 4. Goi cac phuong thuc tren hub
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
