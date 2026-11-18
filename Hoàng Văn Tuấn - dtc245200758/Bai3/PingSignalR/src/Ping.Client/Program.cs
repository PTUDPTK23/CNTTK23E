using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("EchoResponse", msg => Console.WriteLine($"[Echo] {msg}"));
connection.On<string>("BroadcastMessage", msg => Console.WriteLine($"[Broadcast] {msg}"));
connection.On<string>("GroupMessage", msg => Console.WriteLine($"[Group] {msg}"));
connection.On<string>("SystemMessage", msg => Console.WriteLine($"[System] {msg}"));

connection.Reconnecting += error =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};
connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {Short(newId)}");
    // Group membership is tied to the old connection; join again after reconnect.
    return connection.InvokeAsync("JoinGroup", "lop-hoc-01");
};
connection.Closed += error =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {Short(connection.ConnectionId)}");

await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();

static string Short(string? value) =>
    string.IsNullOrEmpty(value) ? "(none)" : value[..Math.Min(8, value.Length)];
