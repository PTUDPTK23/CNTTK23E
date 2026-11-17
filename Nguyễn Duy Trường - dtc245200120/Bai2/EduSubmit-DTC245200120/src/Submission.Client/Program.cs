
using System.Net;
using System.Net.Http.Json;

// ==========================================
// 1. KET NOI API
// ==========================================

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // ==========================================
    // 2. LAY DANH SACH BAI TAP
    // ==========================================

    var assignments = await http
        .GetFromJsonAsync<List<AssignmentDto>>(
            "/api/assignments");

    Console.WriteLine("Danh sach bai tap dang mo:");

    foreach (var a in assignments ?? [])
    {
        if (a.IsOpen)
            Console.WriteLine($"- {a.Id}: {a.Title}");
    }

    // ==========================================
    // 3. NOP BAI TAP
    // ==========================================

    var selectedAssignmentId =
        assignments?.FirstOrDefault(a => a.IsOpen)?.Id;

    if (selectedAssignmentId is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    var createRequest = new CreateSubmissionRequest(
        "DTC245200120",
        "https://github.com/truong256/EduSubmit-DTC245200120"
    );

    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        createRequest);

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();

    if (created is null)
    {
        Console.WriteLine("Khong doc duoc ket qua nop bai.");
        return;
    }

    Console.WriteLine($"\nDa tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");

    // ==========================================
    // 4. DOC LAI LUOT NOP
    // ==========================================

    var location = createResponse.Headers.Location?.ToString()
        ?? $"/api/submissions/{created.Id}";

    var detailResponse = await http.GetAsync(location);

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

    // ==========================================
    // 5. THU GUI DU LIEU SAI - 400
    // ==========================================

    var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine(
        $"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi API: {ex.Message}");
}

// ==========================================
// 6. HAM XU LY LOI HTTP
// ==========================================

static async Task PrintApiErrorAsync(
    HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    var error = await response.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"{error?.Code}: {error?.Message}");
}

// ==========================================
// 7. KHAI BAO DTO
// ==========================================

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
