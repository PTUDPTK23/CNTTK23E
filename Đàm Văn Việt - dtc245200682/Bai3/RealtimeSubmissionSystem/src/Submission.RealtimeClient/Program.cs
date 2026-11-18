using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
string Option(string key, string fallback)
{
    var i = Array.IndexOf(args, key);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}
var hubPath = Option("--hub-path", "/hubs/submissions");
var eventName = Option("--event", "SubmissionStatusChanged");
var watchDelay = int.TryParse(Option("--watch-delay", "0"), out var delay) ? Math.Clamp(delay, 0, 30) : 0;
var two = args.Contains("--two");
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(10) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}{hubPath}").WithAutomaticReconnect().Build();
var tracked = new ConcurrentDictionary<Guid, TrackedSubmission>();

connection.On<SubmissionStatusMessage>(eventName, message =>
{
    if (!tracked.TryGetValue(message.SubmissionId, out var item) || !item.Accept(message.UpdatedAt)) return;
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss.fff} UTC");
    if (message.Status == "failed")
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = previous;
    }
    if (message.Status is "graded" or "failed") item.Completion.TrySetResult(message.Status);
});
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};
connection.Reconnected += async id =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {id}");
    foreach (var pair in tracked)
    {
        try
        {
            await connection.InvokeAsync("WatchSubmission", pair.Key.ToString());
            var latest = await ReadLatestAsync(pair.Value.Location);
            Console.WriteLine($"[Recovery] {pair.Key}: REST = {latest.Status}");
            if (pair.Value.Accept(latest.UpdatedAt) && latest.Status is "graded" or "failed")
                pair.Value.Completion.TrySetResult(latest.Status);
        }
        catch (Exception ex) { Console.WriteLine($"[Recovery] {pair.Key}: {ex.Message}"); }
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
    Console.WriteLine("Da ket noi SignalR.");
    using var listResponse = await http.GetAsync("/api/assignments");
    await RequireAsync(listResponse, HttpStatusCode.OK);
    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    var selected = assignments?.FirstOrDefault(a => a.IsOpen)
        ?? throw new InvalidOperationException("Khong co bai tap dang mo.");
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    await CreateAndWatchAsync(studentCode,
        Option("--repo", $"https://example.com/{studentCode}/signalr"));
    if (two) await CreateAndWatchAsync(studentCode + "-02", "http://example.com/failed-repo");
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var allDone = Task.WhenAll(tracked.Values.Select(t => t.Completion.Task));
    if (await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(10))) != allDone)
        Console.WriteLine("Qua thoi gian cho thong bao graded.");

    foreach (var pair in tracked)
    {
        var latest = await ReadLatestAsync(pair.Value.Location);
        Console.WriteLine($"Trang thai doc lai bang REST: {latest.Status}" +
            (two ? $" ({pair.Value.StudentCode}, {pair.Key})" : ""));
        if (two)
        {
            var signal = pair.Value.Completion.Task.IsCompletedSuccessfully
                ? pair.Value.Completion.Task.Result : "missed";
            Console.WriteLine($"{pair.Value.StudentCode}: SignalR = {signal}, REST = {latest.Status}");
        }
        if (latest.Status is not ("graded" or "failed")) Environment.ExitCode = 2;
    }

    async Task CreateAndWatchAsync(string code, string repository)
    {
        using var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
            new CreateSubmissionRequest(code, repository));
        await RequireAsync(response, HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
            ?? throw new InvalidOperationException("API khong tra DTO luot nop.");
        var location = response.Headers.Location
            ?? throw new InvalidOperationException("API khong tra Location.");
        tracked[created.Id] = new TrackedSubmission(code, location);
        Console.WriteLine($"Da tao luot nop: {created.Id}" + (two ? $" ({code})" : ""));
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
        if (watchDelay > 0) await Task.Delay(TimeSpan.FromSeconds(watchDelay));
        await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    }
}
catch (HttpRequestException ex) { Console.WriteLine($"Loi ket noi: {ex.Message}"); Environment.ExitCode = 1; }
catch (HubException ex) { Console.WriteLine($"Loi hub: {ex.Message}"); Environment.ExitCode = 1; }
catch (OperationCanceledException) { Console.WriteLine("Het thoi gian cho API."); Environment.ExitCode = 1; }
catch (Exception ex) when (ex is InvalidOperationException or JsonException)
{
    Console.WriteLine($"Loi: {ex.Message}"); Environment.ExitCode = 1;
}

async Task<SubmissionDto> ReadLatestAsync(Uri location)
{
    using var response = await http.GetAsync(location);
    await RequireAsync(response, HttpStatusCode.OK);
    return await response.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new InvalidOperationException("API khong tra trang thai.");
}

static async Task RequireAsync(HttpResponseMessage response, HttpStatusCode expected)
{
    if (response.StatusCode == expected) return;
    var body = await response.Content.ReadAsStringAsync();
    throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
}

sealed class TrackedSubmission(string studentCode, Uri location)
{
    private readonly object gate = new();
    private DateTimeOffset lastUpdate = DateTimeOffset.MinValue;
    public string StudentCode { get; } = studentCode;
    public Uri Location { get; } = location;
    public TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Accept(DateTimeOffset timestamp)
    {
        lock (gate)
        {
            if (timestamp < lastUpdate) return false;
            lastUpdate = timestamp;
            return true;
        }
    }
}
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
