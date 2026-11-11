
using System.Net;
using System.Net.Http.Json;

var baseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

try
{
    // 1. Lay danh sach bai tap
    Console.WriteLine("Danh sach bai tap dang mo:");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments");

    foreach (var item in assignments ?? new List<AssignmentDto>())
    {
        if (item.IsOpen)
            Console.WriteLine($"- {item.Id}: {item.Title}");
    }

    // 2. Nop bai
    var selectedAssignmentId = assignments?
        .FirstOrDefault(a => a.IsOpen)?.Id ?? 1;

    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest(
            "dtc245200107",
            "https://github.com/hoangthikieuanh5-debug/CNTTK23E"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();

    if (created is null)
    {
        Console.WriteLine("Khong doc duoc bai nop.");
        return;
    }

    Console.WriteLine($"\nDa tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");

    // 3. Doc lai bai nop
    var saved = await http.GetFromJsonAsync<SubmissionDto>(
        $"/api/submissions/{created.Id}");

    if (saved is not null)
    {
        Console.WriteLine(
            $"Doc lai luot nop: {saved.StudentCode} - {saved.RepositoryUrl}");
    }

    // 4. Thu gui yeu cau khong hop le
    var badResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest(
            "",
            ""));

    Console.WriteLine(
        $"\nThu gui yeu cau loi: {(int)badResponse.StatusCode} {badResponse.StatusCode}");

    await PrintApiErrorAsync(badResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi HTTP: {ex.Message}");
}
catch (TaskCanceledException ex)
{
    Console.WriteLine($"Yeu cau bi huy hoac het thoi gian: {ex.Message}");
}

// Ham xu ly ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    var error = await response.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine($"{error?.Code}: {error?.Message}");
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
    DateTimeOffset ReceivedAt);

record ApiError(
    string Code,
    string Message);
