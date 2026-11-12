using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

try
{
    // 1. Liet ke bai tap va chon bai dang mo dau tien.
    using var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }

    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
    Console.WriteLine("Danh sach bai tap:");
    foreach (var assignment in assignments)
        Console.WriteLine($" - {assignment.Id}: {assignment.Title} ({(assignment.IsOpen ? "open" : "closed")})");

    var selectedAssignment = assignments.FirstOrDefault(item => item.IsOpen);
    if (selectedAssignment is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    // 2. Tao mot luot nop.
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("API khong tra ve du lieu luot nop.");
        return;
    }
    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");

    // 3. Doc lai tai nguyen theo URI do server tao.
    var location = createResponse.Headers.Location;
    if (location is null)
    {
        Console.WriteLine("Phan hoi 201 thieu header Location.");
        return;
    }

    using var readResponse = await http.GetAsync(location);
    if (!readResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(readResponse);
        return;
    }

    var readBack = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Doc lai luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");

    // 4. Gui du lieu loi de minh hoa client doc ApiError thay vi bi sap.
    using var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest("", ""));
    Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Loi ket noi toi API ({apiBaseUrl}): {ex.Message}");
    Environment.ExitCode = 1;
}
catch (TaskCanceledException ex)
{
    Console.Error.WriteLine($"Yeu cau het thoi gian cho: {ex.Message}");
    Environment.ExitCode = 1;
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine(error is null
        ? $"HTTP {(int)response.StatusCode} {response.StatusCode}: phan hoi loi khong co ApiError." 
        : $"{error.Code}: {error.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
