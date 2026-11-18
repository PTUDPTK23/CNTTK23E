<<<<<<< HEAD
using Microsoft.AspNetCore.SignalR.Client;

var apiUrl = args.ElementAtOrDefault(0)?.TrimEnd('/') ?? "http://localhost:5200";
var name = args.ElementAtOrDefault(1) ?? "An";
var experiment = args.ElementAtOrDefault(2);
var hubPath = experiment == "--wrong-hub" ? "/hub/ping" : "/hubs/ping";
var echoMethod = experiment == "--wrong-method" ? "Ech" : "Echo";
var echoEvent = experiment == "--wrong-event" ? "EchoRespone" : "EchoResponse";
const string group = "lop-hoc-01";

await using var connection = new HubConnectionBuilder()
    .WithUrl(apiUrl + hubPath)
    .WithAutomaticReconnect()
    .Build();

connection.On<string>(echoEvent, message => Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("GroupMessage", message => Console.WriteLine($"[Group] {message}"));
connection.On<string>("SystemMessage", message => Console.WriteLine($"[System] {message}"));

connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};
connection.Reconnected += async id =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {id}");
    try
    {
        await connection.InvokeAsync("JoinGroup", group);
    }
    catch (Exception error)
    {
        Console.Error.WriteLine($"[Group] Khong the vao lai nhom: {error.Message}");
    }
};
connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    Console.WriteLine($"{name} da ket noi den PingApi.");
    await connection.InvokeAsync(echoMethod, "Xin chao SignalR");
    await connection.InvokeAsync("JoinGroup", group);
    await connection.InvokeAsync("SendToGroup", group, $"Thong bao nhom tu {name}");
    await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");
    Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
    Console.ReadLine();
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"{error.GetType().Name}: {error.Message}");
    return 1;
=======
﻿
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5200";

var name = args.Length > 1 ? args[1] : "Client";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));

connection.On<string>("BroadcastMessage",
    msg => Console.WriteLine($"[Broadcast] {msg}"));

connection.On<string>("GroupMessage",
    msg => Console.WriteLine($"[Group] {msg}"));
connection.Reconnecting += error =>
{
    Console.WriteLine("[Reconnect] Mat ket noi, dang thu lai...");
    return Task.CompletedTask;
};

connection.Reconnected += async connectionId =>
{
    Console.WriteLine("[Reconnect] Da ket noi lai thanh cong!");

    await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
    Console.WriteLine("[Reconnect] Da tham gia lai nhom.");
};

connection.Closed += error =>
{
    Console.WriteLine("[Reconnect] Ket noi da dong.");
    return Task.CompletedTask;
};
await connection.StartAsync();

Console.WriteLine($"{name} da ket noi.");

await connection.InvokeAsync(
    "Echo", $"Xin chao SignalR tu {name}");

await connection.InvokeAsync("JoinGroup", "lop-hoc-01");

Console.WriteLine("Nhap lenh:");
Console.WriteLine("b <tin nhan> : Broadcast");
Console.WriteLine("g <tin nhan> : Gui cho nhom");
Console.WriteLine("q : Thoat");

while (true)
{
    var input = Console.ReadLine();

    if (input == null || input == "q")
        break;

    if (input.StartsWith("b "))
    {
        await connection.InvokeAsync(
            "Broadcast", $"{name}: {input[2..]}");
    }
    else if (input.StartsWith("g "))
    {
        await connection.InvokeAsync(
            "SendToGroup", "lop-hoc-01",
            $"{name}: {input[2..]}");
    }
>>>>>>> d93af16 (Bai 3 - SignalR - DTC245200767)
}
