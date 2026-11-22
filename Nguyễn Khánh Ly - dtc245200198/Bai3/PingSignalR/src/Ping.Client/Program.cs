
using Microsoft.AspNetCore.SignalR.Client;

Console.Write("Nhap ten: ");
var name = Console.ReadLine() ?? "User";

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5200/hubs/ping")
    .WithAutomaticReconnect()
    .Build();
connection.On<string>("EchoResponse",
    message => Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage",
    m => Console.WriteLine($"[Broadcast] {m}"));

connection.On<string>("GroupMessage",
    m => Console.WriteLine($"[Group] {m}"));
connection.On<string>("SystemMessage",
    m => Console.WriteLine($"[System] {m}"));

await connection.StartAsync();
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");


connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};


await connection.InvokeAsync("JoinGroup", "lop-hoc-01");

await connection.InvokeAsync(
    "SendToGroup",
    "lop-hoc-01",
    $"Thong bao nhom tu {name}");

await connection.InvokeAsync(
    "Broadcast",
    $"Xin chao tat ca tu {name}");

Console.ReadLine();
