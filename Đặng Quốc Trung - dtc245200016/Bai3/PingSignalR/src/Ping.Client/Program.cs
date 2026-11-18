using Microsoft.AspNetCore.SignalR.Client;

string serverUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5200";

string name = args.Length > 1 ? args[1] : "Client";
const string groupName = "lop-hoc-01";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("EchoResponse", msg =>
    Console.WriteLine($"[Echo] {msg}"));

connection.On<string>("BroadcastMessage", msg =>
    Console.WriteLine($"[Broadcast] {msg}"));

connection.On<string>("GroupMessage", msg =>
    Console.WriteLine($"[Group] {msg}"));

connection.On<string>("SystemMessage", msg =>
    Console.WriteLine($"[System] {msg}"));

// Buoc 6: Dang ket noi lai
connection.Reconnecting += error =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

// Buoc 6: Da ket noi lai
connection.Reconnected += newId =>
{
    Console.WriteLine(
        $"[Lifecycle] Da ket noi lai: {newId?[..8]}");

    return Task.CompletedTask;
};

// Buoc 6: Ket noi da dong
connection.Closed += error =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

await connection.StartAsync();

Console.WriteLine(
    $"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId?[..8]}");

await connection.InvokeAsync("Echo",
    $"Xin chao SignalR tu {name}");

await connection.InvokeAsync("JoinGroup", groupName);

await connection.InvokeAsync("SendToGroup",
    groupName, $"Thong bao nhom tu {name}");

await connection.InvokeAsync("Broadcast",
    $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
