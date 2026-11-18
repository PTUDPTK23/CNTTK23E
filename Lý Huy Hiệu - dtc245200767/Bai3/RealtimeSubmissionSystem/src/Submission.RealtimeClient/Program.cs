using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiUrl = args.ElementAtOrDefault(0)?.TrimEnd('/') ?? "http://localhost:5000";
var studentCode = args.ElementAtOrDefault(1) ?? "SV001";
var mode = args.ElementAtOrDefault(2);
var hubPath = mode == "--wrong-hub" ? "/hub/submissions" : "/hubs/submissions";
var eventName = mode == "--wrong-event" ? "SubmissionStatusChange" : "SubmissionStatusChanged";
var pending = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl(apiUrl + hubPath)
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusMessage>(eventName, message =>
{
    if (!pending.TryGetValue(message.SubmissionId, out var completion))
        return;

    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status}");
    if (message.Status == "failed")
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed).");
        Console.ForegroundColor = previousColor;
    }

    if (message.Status is "graded" or "failed")
        completion.TrySetResult(message.Status);
});
connection.Reconnected += async _ =>
{
    foreach (var id in pending.Keys)
    {
        try
        {
            await connection.InvokeAsync("WatchSubmission", id);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Khong the theo doi lai {id}: {error.Message}");
        }
    }
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<AssignmentDto[]>("/api/assignments");
    var assignment = assignments?.FirstOrDefault(item => item.IsOpen);
    if (assignment is null)
    {
        Console.Error.WriteLine("Khong co bai tap dang mo.");
        return 1;
    }

    var scheme = mode == "--failed" ? "http" : "https";
    var first = await CreateAndWatchAsync(assignment.Id, studentCode,
        $"{scheme}://example.com/{studentCode}/signalr");
    if (mode != "--two")
    {
        await ReadFinalStatusAsync(first);
        return 0;
    }

    var secondCode = studentCode == "SV002" ? "SV003" : "SV002";
    var second = await CreateAndWatchAsync(assignment.Id, secondCode,
        $"http://example.com/{secondCode}/signalr");
    await Task.WhenAll(ReadFinalStatusAsync(first), ReadFinalStatusAsync(second));
    return 0;
}
catch (HttpRequestException error)
{
    Console.Error.WriteLine($"HttpRequestException: {error.Message}");
    return 1;
}
catch (Exception error)
{
    Console.Error.WriteLine($"{error.GetType().Name}: {error.Message}");
    return 1;
}

async Task<SubmissionDto> CreateAndWatchAsync(int assignmentId, string code, string repositoryUrl)
{
    using var response = await http.PostAsJsonAsync($"/api/assignments/{assignmentId}/submissions",
        new CreateSubmissionRequest(code, repositoryUrl));
    if (response.StatusCode != HttpStatusCode.Created)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error?.Code} {error?.Message}");
    }

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new InvalidOperationException("Phan hoi 201 khong co du lieu luot nop.");
    Console.WriteLine($"Da tao luot nop: {created.Id} ({created.StudentCode})");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

    pending[created.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    if (mode == "--late-watch")
        await Task.Delay(TimeSpan.FromSeconds(4));

    await connection.InvokeAsync("WatchSubmission", created.Id);
    return created;
}

async Task ReadFinalStatusAsync(SubmissionDto created)
{
    var notification = pending[created.Id].Task;
    var signalRStatus = "khong nhan duoc";
    try
    {
        signalRStatus = await notification.WaitAsync(TimeSpan.FromSeconds(10));
    }
    catch (TimeoutException)
    {
        Console.WriteLine($"Qua thoi gian cho thong bao ket thuc: {created.Id}.");
    }
    finally
    {
        pending.TryRemove(created.Id, out _);
    }

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}")
        ?? throw new InvalidOperationException("Khong doc duoc trang thai cuoi qua REST.");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest.Status}");
    Console.WriteLine($"{created.StudentCode} ({created.Id}): SignalR = {signalRStatus}, REST = {latest.Status}");
}

sealed record AssignmentDto(int Id, string Title, bool IsOpen);
sealed record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
sealed record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
sealed record ApiError(string Code, string Message);
