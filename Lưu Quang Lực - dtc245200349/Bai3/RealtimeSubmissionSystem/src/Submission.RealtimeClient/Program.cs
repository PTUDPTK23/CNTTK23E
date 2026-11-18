using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
string Option(string key, string fallback)
{
    var index = Array.IndexOf(args, key);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
}
if (!int.TryParse(Option("--watch-delay-ms", "0"), out var watchDelay) || watchDelay < 0 ||
    !int.TryParse(Option("--count", "1"), out var count) || count is < 1 or > 2)
{
    Console.Error.WriteLine("--watch-delay-ms phai >= 0; --count chi nhan 1 hoac 2.");
    return 1;
}
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}{Option("--hub", "/hubs/submissions")}")
    .WithAutomaticReconnect().Build();
var watched = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
void Complete(Guid id, string status)
{
    if ((status is "graded" or "failed") && watched.TryGetValue(id, out var completion))
        completion.TrySetResult(status);
}
connection.On<SubmissionStatusMessage>(Option("--event", "SubmissionStatusChanged"), message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status == "failed")
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = previous;
    }
    Complete(message.SubmissionId, message.Status);
});
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};
connection.Reconnected += async id =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {id}");
    foreach (var submissionId in watched.Keys)
    {
        try
        {
            await connection.InvokeAsync("WatchSubmission", submissionId.ToString());
            var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submissionId}");
            if (latest is not null)
            {
                Console.WriteLine($"[REST sau noi lai] {submissionId}: {latest.Status}");
                Complete(submissionId, latest.Status);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        { Console.Error.WriteLine($"[Loi dong bo] {ex.Message}"); }
    }
};
try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null) { Console.Error.WriteLine("Khong co bai tap dang mo."); return 1; }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
    for (var index = 0; index < count; index++)
    {
        var code = index == 0 ? studentCode : $"{studentCode}-2";
        var repository = index == 0
            ? Option("--repository", $"https://example.com/{code}/signalr")
            : Option("--second-repository", $"https://example.com/{code}/signalr");
        using var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
            new CreateSubmissionRequest(code, repository));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            Console.Error.WriteLine($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
            ?? throw new InvalidOperationException("POST khong tra ve luot nop.");
        Console.WriteLine($"Da tao luot nop: {created.Id}");
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
        watched[created.Id] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (watchDelay > 0) await Task.Delay(watchDelay);
        await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    }
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var notifications = Task.WhenAll(watched.Values.Select(c => c.Task));
    if (await Task.WhenAny(notifications, Task.Delay(TimeSpan.FromSeconds(10))) != notifications)
        Console.WriteLine("Qua thoi gian cho thong bao graded/failed.");
    var allGraded = true;
    foreach (var id in watched.Keys.Order())
    {
        var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{id}");
        Console.WriteLine(count == 1 ? $"Trang thai doc lai bang REST: {latest?.Status}"
            : $"Trang thai doc lai bang REST: {id} -> {latest?.Status}");
        allGraded &= latest?.Status == "graded";
    }
    return allGraded ? 0 : 2;
}
catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
{
    Console.Error.WriteLine($"[Loi] {ex.Message}");
    return 1;
}

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
