using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

// Cau hinh dia chi may chu va ma sinh vien
var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1
    ? args[1]
    : "SV001";

// Kenh 1: REST API
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Kenh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Cho thong bao cham bai xong
var gradedNotification = new TaskCompletionSource();

// Dang ky nhan thong bao tu may chu
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged", message =>
{
    Console.WriteLine(
        $"Thong bao SignalR: {message.Id} -> {message.Status}");

    if (message.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

// Ket noi SignalR
await connection.StartAsync();

Console.WriteLine("Da ket noi SignalR.");

// Cac DTO
record AssignmentDto(
    int Id,
    string Title,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
