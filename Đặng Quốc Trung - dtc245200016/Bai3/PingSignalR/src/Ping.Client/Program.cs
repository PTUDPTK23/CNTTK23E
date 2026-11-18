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

// Nhan thong diep Echo
connection.On<string>("EchoResponse", msg =>
{
    Console.WriteLine($"[Echo] {msg}");
});

// Nhan thong diep Broadcast
connection.On<string>("BroadcastMessage", msg =>
{
    Console.WriteLine($"[Broadcast] {msg}");
});

// Nhan thong diep nhom
connection.On<string>("GroupMessage", msg =>
{
    Console.WriteLine($"[Group] {msg}");
});

// Mo ket noi
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");

// Gui Echo
await connection.InvokeAsync("Echo",
    $"Xin chao SignalR tu {name}");

// Tham gia nhom
await connection.InvokeAsync("JoinGroup", groupName);

// Gui thong bao nhom
await connection.InvokeAsync("SendToGroup",
    groupName, $"Thong bao nhom tu {name}");

// Gui cho tat ca
await connection.InvokeAsync("Broadcast",
    $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
