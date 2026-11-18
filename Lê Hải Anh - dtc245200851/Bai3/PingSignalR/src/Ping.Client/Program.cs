using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";
var experiment = args.Length > 2 ? args[2] : "normal";
var hubPath = experiment == "wrong-hub" ? "/hub/ping" : "/hubs/ping";
var echoEvent = experiment == "wrong-event" ? "EchoRespone" : "EchoResponse";
Console.WriteLine($"Thi nghiem: {experiment}; hub: {hubPath}; su kien: {echoEvent}");
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}{hubPath}")
    .WithAutomaticReconnect()
    .Build();
connection.On<string>(echoEvent, m => Console.WriteLine($"[Echo] {m}"));
connection.On<string>("BroadcastMessage", m => Console.WriteLine($"[Broadcast] {m}"));
connection.On<string>("GroupMessage", m => Console.WriteLine($"[Group] {m}"));
connection.On<string>("SystemMessage", m => Console.WriteLine($"[System] {m}"));
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};
connection.Reconnected += async newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId}");
    await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
};
connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId}");
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");
Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
