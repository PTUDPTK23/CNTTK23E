using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var baseAddress = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5250";
var studentCode = args.Length > 1 ? args[1] : "SV001";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};

var assignments = await httpClient.GetFromJsonAsync<List<AssignmentDto>>(
    "/api/assignments");

if (assignments is null)
{
    throw new InvalidOperationException(
        "Khong the doc danh sach bai tap.");
}

var assignment = assignments.Count == 0
    ? null
    : assignments.FirstOrDefault(item => item.Id == 201);

if (assignment is not null)
{
    Console.WriteLine(
        $"Tim thay ma bai tap 201: {assignment.Title}");
}
else
{
    Console.WriteLine(
        "API bai tap hien chua co du lieu; dung ma bai tap 201.");
}

var first = await CreateSubmissionAsync(
    httpClient,
    studentCode);

var firstCompletion = new TaskCompletionSource(
    TaskCreationOptions.RunContinuationsAsynchronously);

await using var firstConnection =
    await CreateSubmissionConnectionAsync(
        baseAddress,
        first.Id,
        firstCompletion);

var completed = await Task.WhenAny(
    firstCompletion.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != firstCompletion.Task)
{
    Console.WriteLine(
        "Chuong graded khong xuat hien trong 10 giay.");
}
else
{
    Console.WriteLine(
        "Da nhan chuong graded bang SignalR.");
}

var latest = await httpClient.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{first.Id}");

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status ?? "unknown"}");

static async Task<SubmissionDto> CreateSubmissionAsync(
    HttpClient httpClient,
    string studentCode)
{
    var request = new CreateSubmissionRequest(
        studentCode,
        $"https://github.com/example/{studentCode}-{Guid.NewGuid():N}");

    using var response = await httpClient.PostAsJsonAsync(
        "/api/assignments/2/submissions",
        request);

    response.EnsureSuccessStatusCode();

    return await response.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new InvalidOperationException(
            "Phan hoi tao submission khong hop le.");
}

static async Task<HubConnection> CreateSubmissionConnectionAsync(
    string baseAddress,
    Guid submissionId,
    TaskCompletionSource completion)
{
    var connection = new HubConnectionBuilder()
        .WithUrl($"{baseAddress}/hubs/submissions")
        .WithAutomaticReconnect()
        .Build();

    connection.On<SubmissionStatusMessage>(
        "SubmissionStatusChanged",
        message =>
        {
            Console.WriteLine(
                $"Thong bao SignalR: {message.Id} -> {message.Status}");

            if (message.Id == submissionId &&
                message.Status == "graded")
            {
                completion.TrySetResult();
            }
        });

    await connection.StartAsync();
    await connection.InvokeAsync(
        "MatchSubmission",
        submissionId.ToString());

    Console.WriteLine(
        $"Da ket noi va theo doi submission {submissionId}");

    return connection;
}

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

record AssignmentDto(
    int Id,
    string Title);
