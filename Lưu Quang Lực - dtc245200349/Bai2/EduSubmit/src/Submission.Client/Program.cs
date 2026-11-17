using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) ||
    (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
{
    Console.Error.WriteLine("Dia chi API phai la URL HTTP hoac HTTPS hop le.");
    return 1;
}

using var http = new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(10) };
try
{
    using var assignmentsResponse = await http.GetAsync("/api/assignments");
    if (assignmentsResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(assignmentsResponse);
        return 1;
    }

    var assignments = await assignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
    var openAssignments = assignments.Where(assignment => assignment.IsOpen).ToList();
    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var assignment in openAssignments)
        Console.WriteLine($"- {assignment.Id}: {assignment.Title}");

    if (openAssignments.Count == 0)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return 0;
    }

    var submissionPath = $"/api/assignments/{openAssignments[0].Id}/submissions";
    var request = new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api");
    using var createResponse = await http.PostAsJsonAsync(submissionPath, request);
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return 1;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    var location = createResponse.Headers.Location;
    if (created is null || location is null)
    {
        Console.Error.WriteLine("Phan hoi 201 thieu du lieu luot nop hoac header Location.");
        return 1;
    }

    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");
    Console.WriteLine($"Location: {location}");

    using var detailResponse = await http.GetAsync(location);
    if (detailResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(detailResponse);
        return 1;
    }

    var saved = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (saved is null)
    {
        Console.Error.WriteLine("API tra ve luot nop rong.");
        return 1;
    }

    Console.WriteLine($"Doc lai luot nop: {saved.StudentCode} - {saved.RepositoryUrl}");

    using var invalidResponse = await http.PostAsJsonAsync(
        submissionPath, new CreateSubmissionRequest("", ""));
    Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
    return invalidResponse.StatusCode == HttpStatusCode.BadRequest ? 0 : 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Loi ket noi toi {apiBaseUrl}: {ex.Message}");
    return 1;
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine($"Qua thoi gian cho API tai {apiBaseUrl}.");
    return 1;
}
catch (System.Text.Json.JsonException ex)
{
    Console.Error.WriteLine($"Phan hoi JSON khong hop le: {ex.Message}");
    return 1;
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    if (response.Content.Headers.ContentType?.MediaType == "application/json" &&
        await response.Content.ReadFromJsonAsync<ApiError>() is { } error)
    {
        Console.Error.WriteLine($"{error.Code}: {error.Message}");
        return;
    }

    Console.Error.WriteLine($"Loi HTTP {(int)response.StatusCode} {response.StatusCode}.");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
