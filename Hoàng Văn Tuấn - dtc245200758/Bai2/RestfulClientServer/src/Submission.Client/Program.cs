using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/")
};

try
{
    // NAC 1: Xem danh sach bai tap
    Console.WriteLine("=== NAC 1: DANH SACH BAI TAP ===");

    var listResponse = await http.GetAsync("api/assignments");
    if (!listResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }

    var assignments =
        await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();

    foreach (var item in assignments ?? [])
        Console.WriteLine($"- {item.Id}: {item.Title} | Open: {item.IsOpen}");

    var selected = assignments?.FirstOrDefault(item => item.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    // NAC 2: Nop bai
    Console.WriteLine("\n=== NAC 2: NOP BAI ===");

    var createResponse = await http.PostAsJsonAsync(
        $"api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(
            "SV001",
            "https://github.com/SV001/rest-api"
        ));

    Console.WriteLine($"HTTP: {(int)createResponse.StatusCode}");

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created =
        await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();

    if (created is null)
    {
        Console.WriteLine("Khong doc duoc ket qua nop bai.");
        return;
    }

    Console.WriteLine($"Ma luot nop: {created.Id}");
    Console.WriteLine($"Trang thai: {created.Status}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");

    // NAC 3: Doc lai luot nop theo Location
    Console.WriteLine("\n=== NAC 3: DOC LAI LUOT NOP ===");

    var location = createResponse.Headers.Location
        ?? new Uri($"api/submissions/{created.Id}", UriKind.Relative);

    var readResponse = await http.GetAsync(location);

    if (readResponse.IsSuccessStatusCode)
    {
        var readSubmission =
            await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();

        Console.WriteLine($"Sinh vien: {readSubmission?.StudentCode}");
        Console.WriteLine($"Repository: {readSubmission?.RepositoryUrl}");
        Console.WriteLine($"Trang thai: {readSubmission?.Status}");
    }
    else
    {
        await PrintApiErrorAsync(readResponse);
    }

    // NAC 4: Thu gui du lieu sai
    Console.WriteLine("\n=== NAC 4: THU GUI DU LIEU SAI ===");

    var invalidResponse = await http.PostAsJsonAsync(
        $"api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("", "")
    );

    Console.WriteLine($"HTTP: {(int)invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine("LOI KET NOI API: " + ex.Message);
    Console.WriteLine("Hay kiem tra may chu va dia chi API.");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    var error = await response.Content.ReadFromJsonAsync<ApiError>();

    if (error is not null)
        Console.WriteLine($"{error.Code}: {error.Message}");
    else
        Console.WriteLine("HTTP Error: " + (int)response.StatusCode);
}

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

record ApiError(string Code, string Message);
