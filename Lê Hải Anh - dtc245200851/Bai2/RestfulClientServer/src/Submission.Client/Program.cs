// Le Hai Anh - DTC245200851 - CNTT K23E
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri)
    || (baseUri.Scheme != "http" && baseUri.Scheme != "https"))
{
    Console.Error.WriteLine("Dia chi API phai la URL http hoac https hop le.");
    Environment.ExitCode = 1;
    return;
}
using var http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };
try
{
    // 1. Xem danh sach, chon bai tap con mo.
    using var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode) { await PrintApiErrorAsync(listResponse); return; }
    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var a in (assignments ?? []).Where(a => a.IsOpen))
        Console.WriteLine($"- {a.Id}: {a.Title}");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    // 2. Nop bai; chi doc SubmissionDto khi nhan 201.
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("DTC245200851", "https://github.com/dtc245200851/PTUDPT_HaiAnh"));
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }
    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

    // 3. Dung Location de doc lai, khong tu ghep id.
    var location = createResponse.Headers.Location;
    if (location is null)
    {
        Console.Error.WriteLine("Phan hoi 201 thieu Location.");
        Environment.ExitCode = 1;
        return;
    }
    using var detailResponse = await http.GetAsync(location);
    if (!detailResponse.IsSuccessStatusCode) { await PrintApiErrorAsync(detailResponse); return; }
    var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Doc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");

    // 4. Gui sai co y va doc ApiError.
    using var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions", new CreateSubmissionRequest("", ""));
    Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    if (invalidResponse.StatusCode != HttpStatusCode.BadRequest) Environment.ExitCode = 1;
    await PrintApiErrorAsync(invalidResponse, expected: true);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Loi ket noi den {apiBaseUrl}: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine("API khong phan hoi trong 10 giay.");
    Environment.ExitCode = 1;
}

static async Task PrintApiErrorAsync(HttpResponseMessage response, bool expected = false)
{
    if (response.IsSuccessStatusCode) return;
    if (!expected) Environment.ExitCode = 1;
    var body = await response.Content.ReadAsStringAsync();
    try
    {
        var error = JsonSerializer.Deserialize<ApiError>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (error is not null && !string.IsNullOrEmpty(error.Code))
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
            return;
        }
    }
    catch (JsonException) { }
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}: khong co ApiError hop le.");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
