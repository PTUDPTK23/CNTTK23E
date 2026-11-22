using System.Net.Http.Json;

var apiUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
Console.WriteLine($"Ket noi toi API tai: {apiUrl}");

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

try
{
    // 1. Lay danh sach bai tap (GET /api/assignments)
    var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
    Console.WriteLine("Danh sach bai tap:");
    if (assignments is not null)
    {
        foreach (var a in assignments)
        {
            Console.WriteLine($"- {a.Id}: {a.Title} ({a.Description})");
        }
    }

    // 2. Gui bai nop (POST /api/submissions)
    var submissionRequest = new CreateSubmissionRequest(
        AssignmentId: 1,
        StudentCode: "DTC245200250",
        RepositoryUrl: "https://github.com/ambrose-mas/EduSubmit-DTC245200250.git"
    );

    var response = await http.PostAsJsonAsync("/api/submissions", submissionRequest);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
    Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
    Console.WriteLine($"Trang thai: {result?.Status}");

    // 3. Bai mo rong (Buoc 12): Doc lai luot nop vua tao qua GET /api/submissions/{id}
    if (result is not null)
    {
        var verifyResponse = await http.GetAsync($"/api/submissions/{result.Id}");
        if (verifyResponse.IsSuccessStatusCode)
        {
            var verified = await verifyResponse.Content.ReadFromJsonAsync<SubmissionResult>();
            Console.WriteLine($"[Kiem tra lai luot nop] Id: {verified?.Id} | Ma SV: {verified?.StudentCode} | Thoi gian: {verified?.ReceivedAt:yyyy-MM-dd HH:mm:ss}");
        }
    }
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[Loi ket noi] Khong the ket noi toi may chu: {ex.Message}");
    Console.ResetColor();
}

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);
