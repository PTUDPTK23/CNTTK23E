using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect().Build();
connection.On<string>("EchoResponse", msg => Console.WriteLine($"[Echo] {msg}"));
connection.On<string>("BroadcastMessage", msg => Console.WriteLine($"[Broadcast] {msg}"));
connection.On<string>("GroupMessage", msg => Console.WriteLine($"[Group] {msg}"));
try
{
    await connection.StartAsync();
    Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId}");
    await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
    await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
    await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
    await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");
    Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
    Console.ReadLine();
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi: {ex.Message}");
    Environment.ExitCode = 1;
}
