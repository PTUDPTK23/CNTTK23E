using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions").WithAutomaticReconnect().Build();
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status}"));
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
