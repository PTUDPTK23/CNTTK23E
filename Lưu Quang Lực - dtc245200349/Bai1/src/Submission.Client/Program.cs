using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
    || (apiUri.Scheme != "http" && apiUri.Scheme != "https")) {
    Console.Error.WriteLine("Dia chi API phai la URL http hoac https hop le.");
    return 1;
}
using var http = new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(10) };
try {
    var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
    Console.WriteLine("Danh sach bai tap:");
    foreach (var assignment in assignments ?? [])
        Console.WriteLine($"- {assignment.Id}: {assignment.Title}");

    // URL vi du: thay bang repository thuc khi da tao tren GitHub.
    var request = new CreateSubmissionRequest(1, "DTC245200349",
        "https://example.com/DTC245200349/EduSubmit");
    using var response = await http.PostAsJsonAsync("/api/submissions", request);
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<SubmissionResult>()
        ?? throw new InvalidOperationException("API tra ve ket qua rong.");
    Console.WriteLine($"Da nop bai. Ma luot nop: {result.Id}");
    Console.WriteLine($"Trang thai: {result.Status}");

    var saved = await http.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}")
        ?? throw new InvalidOperationException("Khong doc duoc luot nop.");
    Console.WriteLine($"Doc lai luot nop: {saved.Id} | {saved.StudentCode} | {saved.Status}");
    return 0;
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"Loi HTTP: {ex.Message}");
    Console.Error.WriteLine($"Kiem tra API dang chay tai {apiBaseUrl}.");
    return 1;
} catch (TaskCanceledException) {
    Console.Error.WriteLine("Qua thoi gian cho API.");
    return 1;
}
// Hop dong JSON duoc khai bao rieng, khong tham chieu project API.
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode,
    string Status, DateTimeOffset ReceivedAt);
