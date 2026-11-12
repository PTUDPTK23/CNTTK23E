using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

try
{
    // 1. Xem các bài tập đang mở.
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var assignment in assignments?.Where(item => item.IsOpen) ?? [])
    {
        Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
    }

    var selectedAssignmentId = assignments?.FirstOrDefault(item => item.IsOpen)?.Id;
    if (selectedAssignmentId is null)
    {
        Console.WriteLine("Hien khong co bai tap dang mo.");
        return;
    }

    // 2. Nộp bài và kiểm tra status trước khi đọc body.
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("May chu tra ve body rong khi tao luot nop.");
        return;
    }

    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");

    // 3. Đọc lại qua tài nguyên /api/submissions/{id} lấy từ Location.
    var submissionUri = createResponse.Headers.Location;
    if (submissionUri is null)
    {
        Console.WriteLine("Phan hoi 201 thieu header Location.");
        return;
    }

    var readBackPath = submissionUri.IsAbsoluteUri
        ? submissionUri.PathAndQuery
        : submissionUri.ToString();
    var readBack = await http.GetFromJsonAsync<SubmissionDto>(readBackPath);
    Console.WriteLine($"Doc lai luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");

    // 4. Gửi dữ liệu không hợp lệ và in ApiError thay vì làm client dừng đột ngột.
    using var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));
    Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"Loi ket noi API ({apiBaseUrl}): {exception.Message}");
    Environment.ExitCode = 1;
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
    {
        return;
    }

    try
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null)
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
            return;
        }
    }
    catch (System.Text.Json.JsonException)
    {
        // Framework errors (for example malformed JSON) need not use ApiError.
    }

    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine(string.IsNullOrWhiteSpace(body) ? "Khong co noi dung loi." : body);
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
