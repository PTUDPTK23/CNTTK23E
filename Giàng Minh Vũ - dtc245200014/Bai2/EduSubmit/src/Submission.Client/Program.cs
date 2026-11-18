using System.Net;
using System.Net.Http.Json;

var apiUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseUri))
{
    Console.WriteLine("URL may chu khong hop le. Hay dung mot URL tuyet doi, vi du http://localhost:5000.");
    return;
}

using var client = new HttpClient { BaseAddress = baseUri };

try
{
    Console.WriteLine($"API: {baseUri}");
    Console.WriteLine("BUOC 1: Lay danh sach bai tap dang mo");
    var assignments = await client.GetFromJsonAsync<List<AssignmentDto>>("api/assignments") ?? [];
    var openAssignments = assignments.Where(item => item.IsOpen).ToList();
    if (openAssignments.Count == 0)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var item in openAssignments)
    {
        Console.WriteLine($"- {item.Id}: {item.Title}");
    }

    var selected = openAssignments[0];
    Console.WriteLine($"\nBUOC 2: Nop bai tap {selected.Id}");
    using var createResponse = await client.PostAsJsonAsync(
        $"api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("May chu tra ve phan hoi tao bai nop rong.");
        return;
    }

    Console.WriteLine($"Id: {created.Id}");
    Console.WriteLine($"Status: {created.Status}");
    Console.WriteLine($"ReceivedAt: {created.ReceivedAt:O}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");

    Console.WriteLine("\nBUOC 3: Doc lai luot nop");
    using var readResponse = await client.GetAsync($"api/submissions/{created.Id}");
    if (readResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(readResponse);
        return;
    }

    var reread = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (reread is null)
    {
        Console.WriteLine("May chu tra ve luot nop rong.");
        return;
    }

    Console.WriteLine($"StudentCode: {reread.StudentCode}");
    Console.WriteLine($"RepositoryUrl: {reread.RepositoryUrl}");
    Console.WriteLine($"Status: {reread.Status}");

    Console.WriteLine("\nBUOC 4: Gui yeu cau co du lieu rong");
    using var invalidResponse = await client.PostAsJsonAsync(
        $"api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("", ""));
    if (invalidResponse.StatusCode != HttpStatusCode.BadRequest)
    {
        Console.WriteLine($"Mong doi 400 Bad Request, nhan {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}.");
        await PrintApiErrorAsync(invalidResponse);
        return;
    }

    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException exception) when (exception.StatusCode is not null)
{
    Console.WriteLine($"May chu tra loi loi HTTP {(int)exception.StatusCode.Value} {exception.StatusCode.Value}.");
}
catch (HttpRequestException)
{
    Console.WriteLine("Khong ket noi duoc may chu. Hay kiem tra API va URL/cang dang chay.");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Yeu cau bi huy hoac het thoi gian cho. Hay kiem tra ket noi may chu.");
}
catch (System.Text.Json.JsonException)
{
    Console.WriteLine("Phan hoi JSON tu may chu khong dung dinh dang mong doi.");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    if (error is not null)
    {
        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}: {error.Code} - {error.Message}");
        return;
    }

    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
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
