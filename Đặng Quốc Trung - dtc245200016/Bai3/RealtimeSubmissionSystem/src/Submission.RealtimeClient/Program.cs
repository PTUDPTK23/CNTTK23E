using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var positionalArgs = args
    .Where(argument => !argument.StartsWith("--", StringComparison.Ordinal))
    .ToArray();

var apiBaseUrl = positionalArgs.Length > 0
    ? positionalArgs[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = positionalArgs.Length > 1
    ? positionalArgs[1]
    : "SV001";

var repositoryUrl = GetOptionValue(args, "--repository")
    ?? (positionalArgs.Length > 2 ? positionalArgs[2] : null);

var trackTwoSubmissions = args.Any(argument =>
    argument.Equals("--two", StringComparison.OrdinalIgnoreCase));

try
{
    await RunAsync(
        apiBaseUrl,
        studentCode,
        repositoryUrl,
        trackTwoSubmissions);
}
catch (Exception exception) when (
    exception is HttpRequestException or TaskCanceledException)
{
    WriteInColor(
        $"Khong the ket noi den may chu: {exception.Message}",
        ConsoleColor.Red,
        useErrorStream: true);

    Environment.ExitCode = 1;
}

static async Task RunAsync(
    string apiBaseUrl,
    string studentCode,
    string? repositoryUrl,
    bool trackTwoSubmissions)
{
    using var http = new HttpClient
    {
        BaseAddress = new Uri(apiBaseUrl)
    };

    await using var connection = new HubConnectionBuilder()
        .WithUrl($"{apiBaseUrl}/hubs/submissions")
        .WithAutomaticReconnect()
        .Build();

    // Moi luot nop co mot TaskCompletionSource rieng, tra cuu an toan theo Guid.
    var completionSources =
        new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

    connection.On<SubmissionStatusMessage>(
        "SubmissionStatusChanged", message =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {message.Id} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");

        if (message.Status == "failed")
        {
            WriteInColor(
                $"CANH BAO: Luot nop {message.Id} cham that bai do RepositoryUrl khong bat dau bang https://.",
                ConsoleColor.Red);
        }

        if (message.Status is "graded" or "failed" &&
            completionSources.TryGetValue(message.Id, out var completionSource))
        {
            completionSource.TrySetResult(message.Status);
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments =
        await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");

    var selected = assignments?.FirstOrDefault();

    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap.");
        return;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    int submissionCount = trackTwoSubmissions ? 2 : 1;
    var trackedSubmissions = new List<TrackedSubmission>(submissionCount);

    for (int index = 0; index < submissionCount; index++)
    {
        string currentStudentCode = trackTwoSubmissions
            ? $"{studentCode}-{index + 1}"
            : studentCode;

        string currentRepositoryUrl = repositoryUrl
            ?? $"https://example.com/{currentStudentCode}/signalr";

        var createResponse = await http.PostAsJsonAsync(
            $"/api/assignments/{selected.Id}/submissions",
            new CreateSubmissionRequest(
                currentStudentCode,
                currentRepositoryUrl));

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var error = await createResponse.Content
                .ReadFromJsonAsync<ApiError>();

            Console.WriteLine($"Loi: {error?.Code} - {error?.Message}");
            return;
        }

        var created = await createResponse.Content
            .ReadFromJsonAsync<SubmissionDto>();

        if (created is null)
        {
            Console.WriteLine("Khong doc duoc luot nop.");
            return;
        }

        var completionSource = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (!completionSources.TryAdd(created.Id, completionSource))
        {
            throw new InvalidOperationException(
                $"Luot nop {created.Id} da duoc theo doi.");
        }

        trackedSubmissions.Add(new TrackedSubmission(created, completionSource));

        Console.WriteLine($"Da tao luot nop: {created.Id}");
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

        await connection.InvokeAsync(
            "WatchSubmission", created.Id.ToString());
    }

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

    var allTerminalNotifications = Task.WhenAll(
        trackedSubmissions.Select(item => item.CompletionSource.Task));

    var completed = await Task.WhenAny(
        allTerminalNotifications,
        Task.Delay(TimeSpan.FromSeconds(10)));

    if (completed != allTerminalNotifications)
    {
        Console.WriteLine("Qua thoi gian cho thong bao ket thuc.");
    }

    foreach (var tracked in trackedSubmissions)
    {
        var latest = await http.GetFromJsonAsync<SubmissionDto>(
            $"/api/submissions/{tracked.Submission.Id}");

        Console.WriteLine(
            $"Trang thai doc lai bang REST: {latest?.Status}");

        completionSources.TryRemove(tracked.Submission.Id, out _);
    }
}

static string? GetOptionValue(string[] arguments, string optionName)
{
    string prefix = $"{optionName}=";

    return arguments.FirstOrDefault(argument =>
        argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        ?[prefix.Length..];
}

static void WriteInColor(
    string message,
    ConsoleColor color,
    bool useErrorStream = false)
{
    var originalColor = Console.ForegroundColor;

    try
    {
        Console.ForegroundColor = color;

        if (useErrorStream)
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
    finally
    {
        Console.ForegroundColor = originalColor;
    }
}

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

record TrackedSubmission(
    SubmissionDto Submission,
    TaskCompletionSource<string> CompletionSource);
