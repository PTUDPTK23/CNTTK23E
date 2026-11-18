using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(serverUrl)
};

var statusCompleted = new TaskCompletionSource(
    TaskCreationOptions.RunContinuationsAsynchronously);
Guid? activeSubmissionId = null;

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Dang ky truoc StartAsync de khong bo lo thong diep tu hub.
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"[SignalR] {message.SubmissionId} -> {message.Status}");

        if (message.Status == SubmissionStatuses.Graded)
        {
            statusCompleted.TrySetResult();
        }
    });

connection.Reconnecting += _ =>
{
    Console.WriteLine("[SignalR] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"[SignalR] Da ket noi lai: {connectionId}");

    return activeSubmissionId is Guid submissionId
        ? connection.InvokeAsync("WatchSubmission", submissionId)
        : Task.CompletedTask;
};

connection.Closed += exception =>
{
    Console.WriteLine(exception is null
        ? "[SignalR] Ket noi da dong."
        : $"[SignalR] Ket noi da dong: {exception.Message}");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"Da ket noi den {serverUrl}/hubs/submissions.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
    "/api/assignments");

Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var assignment in assignments ?? [])
{
    Console.WriteLine($"[{assignment.Id}] {assignment.Title}");
}

var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest(
        "DTC245200859",
        "https://github.com/phamzduong-pixel/CNTTK23E"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    Console.WriteLine($"{error?.Code}: {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null || createResponse.Headers.Location is null)
{
    throw new InvalidOperationException("Phan hoi tao submission khong hop le.");
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai ban dau: {created.Status}");
Console.WriteLine($"Location: {createResponse.Headers.Location}");

// Watch dung group submission:{id}; server se gui lai trang thai hien tai.
activeSubmissionId = created.Id;
await connection.InvokeAsync("WatchSubmission", created.Id);
await statusCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

var detail = await http.GetFromJsonAsync<SubmissionDto>(
    createResponse.Headers.Location);
Console.WriteLine($"Trang thai doc lai bang REST: {detail?.Status}");

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);

static class SubmissionStatuses
{
    public const string Graded = "graded";
}
