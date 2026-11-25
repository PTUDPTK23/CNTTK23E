
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

// Dia chi API
var baseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

// Tao ket noi SignalR
var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Tin hieu bao khi nhan duoc trang thai graded
var gradedNotification =
    new TaskCompletionSource<bool>(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

// Dang ky nhan su kien truoc khi ket noi
hubConnection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine();
        Console.WriteLine("=== NHAN THONG BAO SIGNALR ===");
        Console.WriteLine($"Submission ID: {message.SubmissionId}");
        Console.WriteLine($"Trang thai: {message.Status}");
        Console.WriteLine($"Cap nhat luc: {message.UpdatedAt}");

        if (message.Status == "graded")
        {
            gradedNotification.TrySetResult(true);
        }
    }
);

try
{
    Console.WriteLine("=== EDUSUBMIT REALTIME CLIENT ===");
    Console.WriteLine($"API: {baseUrl}");

    // Lay danh sach bai tap
    var assignments = await httpClient
        .GetFromJsonAsync<List<Assignment>>("/api/assignments");

    if (assignments == null || assignments.Count == 0)
    {
        Console.WriteLine("Khong co bai tap.");
        return;
    }

    Console.WriteLine("\nDanh sach bai tap:");

    foreach (var assignment in assignments)
    {
        Console.WriteLine(
            $"{assignment.Id}. {assignment.Title} - {assignment.Description}"
        );
    }

    // Nhap thong tin bai nop
    Console.Write("\nNhap ma bai tap: ");
    if (!int.TryParse(Console.ReadLine(), out int assignmentId))
    {
        Console.WriteLine("Ma bai tap khong hop le.");
        return;
    }

    Console.Write("Nhap ma sinh vien: ");
    var studentCode = Console.ReadLine() ?? "";

    Console.Write("Nhap Repository URL: ");
    var repositoryUrl = Console.ReadLine() ?? "";

    var request = new CreateSubmissionRequest(
        studentCode,
        repositoryUrl
    );

    // Ket noi SignalR truoc khi gui bai
    await hubConnection.StartAsync();

    Console.WriteLine("\nDa ket noi SignalR.");

    // Gui bai nop bang REST API
    var response = await httpClient.PostAsJsonAsync(
        $"/api/assignments/{assignmentId}/submissions",
        request
    );

    Console.WriteLine($"HTTP Status: {(int)response.StatusCode}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(await response.Content.ReadAsStringAsync());
        return;
    }

    var submission = await response.Content
        .ReadFromJsonAsync<SubmissionDto>();

    if (submission == null)
    {
        Console.WriteLine("Khong doc duoc thong tin bai nop.");
        return;
    }

    Console.WriteLine("\nDa gui bai nop thanh cong!");
    Console.WriteLine($"Submission ID: {submission.Id}");
    Console.WriteLine($"Trang thai ban dau: {submission.Status}");

    // Dang ky theo doi rieng bai nop nay
    await hubConnection.InvokeAsync(
        "WatchSubmission",
        submission.Id
    );

    Console.WriteLine("Dang theo doi trang thai bai nop...");

    // Cho thong bao graded, toi da 10 giay
    var completedTask = await Task.WhenAny(
        gradedNotification.Task,
        Task.Delay(TimeSpan.FromSeconds(10))
    );

    if (completedTask == gradedNotification.Task)
    {
        Console.WriteLine("\nDa nhan thong bao cham bai hoan tat!");

        // Dung REST API de lay trang thai cuoi cung
        var finalSubmission = await httpClient
            .GetFromJsonAsync<SubmissionDto>(
                $"/api/submissions/{submission.Id}"
            );

        if (finalSubmission != null)
        {
            Console.WriteLine("\n=== KET QUA CUOI CUNG ===");
            Console.WriteLine($"Ma sinh vien: {finalSubmission.StudentCode}");
            Console.WriteLine($"Trang thai: {finalSubmission.Status}");
            Console.WriteLine($"Cap nhat luc: {finalSubmission.UpdatedAt}");
        }
    }
    else
    {
        Console.WriteLine(
            "\nHet thoi gian cho thong bao SignalR."
        );

        // Van co the tra cuu trang thai bang REST
        var currentSubmission = await httpClient
            .GetFromJsonAsync<SubmissionDto>(
                $"/api/submissions/{submission.Id}"
            );

        Console.WriteLine(
            $"Trang thai hien tai: {currentSubmission?.Status ?? "Khong tim thay"}"
        );
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Khong the ket noi API: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Loi: {ex.Message}");
}
finally
{
    await hubConnection.DisposeAsync();
}

record Assignment(
    int Id,
    string Title,
    string Description
);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);
