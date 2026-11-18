using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var repositoryUrl = args.Length > 2
    ? args[2]
    : $"https://example.com/{Uri.EscapeDataString(studentCode)}/signalr";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

Guid? submissionId = null;
var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

using var subscription = connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged", message =>
    {
        if (message.SubmissionId != submissionId)
            return;

        Console.WriteLine(
            $"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");

        if (message.Status == "graded")
            finished.TrySetResult(true);
    });

connection.Closed += _ =>
{
    finished.TrySetResult(false);
    return Task.CompletedTask;
};

connection.Reconnected += async _ =>
{
    if (submissionId is not Guid id || finished.Task.IsCompleted)
        return;

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await connection.InvokeAsync("WatchSubmission", id.ToString("D"), timeout.Token);
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    using var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }

    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, repositoryUrl));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc luot nop vua tao.");
        return;
    }

    submissionId = created.Id;
    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await connection.InvokeAsync("WatchSubmission", created.Id.ToString("D"), timeout.Token);
        Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

        if (!await finished.Task.WaitAsync(timeout.Token))
            Console.WriteLine("Ket noi SignalR da dong.");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }
    catch (Exception ex) when (ex is HubException or InvalidOperationException or IOException)
    {
        Console.WriteLine($"Theo doi SignalR bi gian doan: {ex.Message}");
    }

    var location = createResponse.Headers.Location?.ToString() ?? $"/api/submissions/{created.Id}";
    using var readResponse = await http.GetAsync(location);
    if (!readResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(readResponse);
        return;
    }

    var latest = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine(latest is null
        ? "Khong doc duoc trang thai luot nop."
        : $"Trang thai doc lai bang REST: {latest.Status}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi HTTP/SignalR: {ex.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Yeu cau bi huy hoac het thoi gian.");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    var body = await response.Content.ReadAsStringAsync();
    try
    {
        var error = System.Text.Json.JsonSerializer.Deserialize<ApiError>(body,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        if (error is not null && !string.IsNullOrWhiteSpace(error.Code))
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
            return;
        }
    }
    catch (System.Text.Json.JsonException) { }
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
}

// DTO
record AssignmentDto(
    int Id,
    string Title,
    string Description,
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

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
