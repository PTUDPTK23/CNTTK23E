using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

try
{
    Console.WriteLine("Danh sach bai tap:");
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");

    foreach (var assignment in assignments ?? [])
    {
        Console.WriteLine($"- {assignment.Id}: {assignment.Title} (open: {assignment.IsOpen})");
    }

    var selectedAssignment = assignments?.FirstOrDefault(item => item.IsOpen);
    if (selectedAssignment is null)
    {
        Console.WriteLine("Khong co bai tap dang mo de nop.");
        return;
    }

    Console.WriteLine($"\nChi tiet bai tap {selectedAssignment.Id}:");
    using var assignmentResponse = await http.GetAsync($"/api/assignments/{selectedAssignment.Id}");
    if (assignmentResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(assignmentResponse);
        return;
    }

    var assignmentDetail = await assignmentResponse.Content.ReadFromJsonAsync<AssignmentDto>();
    Console.WriteLine(assignmentDetail?.Description);

    Console.WriteLine("\nGui bai nop:");
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest("SV001", "https://github.com/example/sv001-rest-api"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc thong tin luot nop.");
        return;
    }

    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai: {created.Status}");

    Console.WriteLine("\nDoc lai luot nop theo Location:");
    var location = createResponse.Headers.Location;
    if (location is null)
    {
        Console.WriteLine("Phan hoi 201 khong co Location.");
        return;
    }

    using var readBackResponse = await http.GetAsync(location);
    if (readBackResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(readBackResponse);
        return;
    }

    var readBack = await readBackResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"{readBack?.StudentCode} - {readBack?.RepositoryUrl}");

    Console.WriteLine("\nThu gui yeu cau loi de xem ApiError:");
    using var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest("", ""));
    Console.WriteLine($"HTTP {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"Loi ket noi toi {apiBaseUrl}: {exception.Message}");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    if (error is not null)
    {
        Console.WriteLine($"{error.Code}: {error.Message}");
    }
    else
    {
        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}; khong co ApiError body.");
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
