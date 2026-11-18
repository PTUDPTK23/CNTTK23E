using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
{
    Console.WriteLine("Dia chi API khong hop le. Vi du: http://localhost:5000");
    return;
}

using var http = new HttpClient { BaseAddress = baseUri };

try
{
    // NAC 1: Lay danh sach bai tap.
    Console.WriteLine("=== NAC 1: LAY DANH SACH BAI TAP ===");
    using var assignmentsResponse = await http.GetAsync("/api/assignments");
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
        Console.WriteLine($"- {assignment.Id}: {assignment.Title} | Mo: {assignment.IsOpen}");
    }

    var selectedAssignment = assignments?.FirstOrDefault(item => item.IsOpen);
    if (selectedAssignment is null)
    {
        Console.WriteLine("Khong co bai tap dang mo de nop.");
        return;
    }

    // NAC 2: Tao luot nop va kiem tra 201 Created.
    Console.WriteLine("\n=== NAC 2: TAO LUOT NOP (201 CREATED) ===");
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest(
            "DTC245200002",
            "https://github.com/MINH282006/CNTTK23E"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var createdSubmission = await createResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();

    Console.WriteLine($"Da tao luot nop: {createdSubmission?.Id}");
    Console.WriteLine($"Trang thai ban dau: {createdSubmission?.Status}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");

    if (createResponse.Headers.Location is null)
    {
        Console.WriteLine("Phan hoi 201 khong co header Location.");
        return;
    }

    // NAC 3: Dung dia chi Location de doc lai tai nguyen vua tao.
    Console.WriteLine("\n=== NAC 3: DOC LAI LUOT NOP ===");
    using var readBackResponse = await http.GetAsync(createResponse.Headers.Location);
    if (!readBackResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(readBackResponse);
        return;
    }

    var readBack = await readBackResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Doc lai luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");
    Console.WriteLine($"Ma luot nop: {readBack?.Id}");

    // NAC 4: Gui du lieu khong hop le de kiem tra 400 va ApiError.
    Console.WriteLine("\n=== NAC 4: THU GUI YEU CAU LOI (400) ===");
    using var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine($"HTTP: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    // Loi mang/ket noi: khong co phan hoi HTTP tu may chu.
    Console.WriteLine("LOI KET NOI: khong nhan duoc phan hoi HTTP tu API.");
    Console.WriteLine(ex.Message);
}
catch (TaskCanceledException ex)
{
    Console.WriteLine("YEU CAU BI HUY HOAC QUA THOI GIAN CHO.");
    Console.WriteLine(ex.Message);
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
    {
        return;
    }

    // Doc body mot lan; thu phan tich ApiError, neu khong phai JSON thi in body goc.
    var rawBody = await response.Content.ReadAsStringAsync();
    ApiError? error = null;
    try
    {
        error = JsonSerializer.Deserialize<ApiError>(
            rawBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    catch (JsonException)
    {
        // Loi do framework co the tra body khong phai ApiError JSON.
    }

    if (error is not null)
    {
        Console.WriteLine($"{error.Code}: {error.Message}");
        return;
    }

    // Mot so loi do framework tra ve co the khong dung cau truc ApiError.
    Console.WriteLine(string.IsNullOrWhiteSpace(rawBody)
        ? "Phan hoi loi khong co body ApiError (loi do framework)."
        : rawBody);
}

// DTO rieng cua Client; chi can JSON contract tuong thich voi API.
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
