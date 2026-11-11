using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using var http = new HttpClient { BaseAddress = new Uri(args.FirstOrDefault() ?? "http://localhost:5000"), Timeout = TimeSpan.FromSeconds(10) };
try
{
    using var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode) { await PrintApiErrorAsync(listResponse); return; }
    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    Console.WriteLine("1. Danh sach bai tap dang mo:");
    foreach (var a in assignments?.Where(a => a.IsOpen) ?? []) Console.WriteLine($"- {a.Id}: {a.Title}");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null) { Console.WriteLine("Khong co bai tap dang mo."); return; }
    using var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));
    if (response.StatusCode != HttpStatusCode.Created) { await PrintApiErrorAsync(response); return; }
    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"2. Da tao luot nop: {created?.Id}; trang thai: {created?.Status}");
    Console.WriteLine($"Location: {response.Headers.Location}");
    if (response.Headers.Location is not { } location) throw new InvalidOperationException("API khong tra Location.");
    using var detailResponse = await http.GetAsync(location);
    if (!detailResponse.IsSuccessStatusCode) { await PrintApiErrorAsync(detailResponse); return; }
    var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"3. Doc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");
    using var invalidResponse = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions", new CreateSubmissionRequest("", ""));
    Console.WriteLine("4. Thu gui yeu cau loi:");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex) { Console.WriteLine($"Loi ket noi: {ex.Message}"); Environment.ExitCode = 1; }
catch (TaskCanceledException) { Console.WriteLine("Het thoi gian cho API."); Environment.ExitCode = 1; }

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
    var body = await response.Content.ReadAsStringAsync();
    try
    {
        var error = JsonSerializer.Deserialize<ApiError>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Console.WriteLine($"{error?.Code}: {error?.Message}");
    }
    catch (JsonException) { Console.WriteLine(string.IsNullOrWhiteSpace(body) ? "Phan hoi khong co body JSON." : body); }
}
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
