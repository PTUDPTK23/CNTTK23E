using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl),
    Timeout = TimeSpan.FromSeconds(10)
};

try
{
    // Buoc 1: lay danh sach bai tap.
    var assignmentsResponse = await http.GetAsync("/api/assignments");
    if (!assignmentsResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(assignmentsResponse);
        return;
    }

    var assignments = await assignmentsResponse.Content
        .ReadFromJsonAsync<List<AssignmentDto>>();

    Console.WriteLine("Danh sach bai tap:");
    foreach (var assignment in assignments ?? [])
    {
        var state = assignment.IsOpen ? "dang mo" : "da dong";
        Console.WriteLine($"- {assignment.Id}: {assignment.Title} ({state})");
    }

    // Buoc 2: tao mot luot nop cho bai tap dang mo dau tien.
    var selectedAssignmentId = assignments?
        .FirstOrDefault(assignment => assignment.IsOpen)?.Id ?? 1;
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest(
            "SV001",
            "https://example.com/sv001/rest-api"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");

    // Buoc 3: doc lai tai nguyen theo Location ma API tra ve.
    if (createResponse.Headers.Location is not null)
    {
        var detailResponse = await http.GetAsync(createResponse.Headers.Location);
        if (detailResponse.IsSuccessStatusCode)
        {
            var detail = await detailResponse.Content
                .ReadFromJsonAsync<SubmissionDto>();
            Console.WriteLine(
                $"Doc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");
        }
        else
        {
            await PrintApiErrorAsync(detailResponse);
        }
    }

    // Buoc 4: co y gui du lieu sai de kiem tra ApiError 400.
    var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));
    Console.WriteLine(
        $"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"Khong ket noi duoc API tai {apiBaseUrl}.");
    Console.WriteLine($"Loi mang: {exception.Message}");
}
catch (TaskCanceledException)
{
    Console.WriteLine($"API tai {apiBaseUrl} khong phan hoi trong thoi gian cho.");
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
        Console.WriteLine($"{error?.Code}: {error?.Message}");
    }
    catch (Exception exception) when (
        exception is HttpRequestException or NotSupportedException or System.Text.Json.JsonException)
    {
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
        if (!string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine(body);
        }
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
