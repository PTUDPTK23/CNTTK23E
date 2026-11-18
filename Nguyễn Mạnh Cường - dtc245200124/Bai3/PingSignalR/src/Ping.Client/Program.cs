using Microsoft.AspNetCore.SignalR.Client;

var url = args.ElementAtOrDefault(0) ?? "http://localhost:5200";
var name = args.ElementAtOrDefault(1) ?? "SinhVien";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{url.TrimEnd('/')}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("EchoResponse", message => Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("GroupMessage", message => Console.WriteLine($"[Group] {message}"));

await connection.StartAsync();
Console.WriteLine($"Da ket noi SignalR: {url}");
await connection.InvokeAsync("Echo", $"Xin chao tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

Console.WriteLine("Nhan Enter de thoat.");
Console.ReadLine();
