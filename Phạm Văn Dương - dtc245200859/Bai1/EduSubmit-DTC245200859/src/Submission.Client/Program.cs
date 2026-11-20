using System.Net.Http.Json;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"[EduSubmit Client] Dang ket noi toi: {baseUrl}\n");

// 1. Lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("--- DANH SACH BAI TAP ---");
if (assignments != null)
{
    foreach (var a in assignments)
    {
        Console.WriteLine($"[{a.Id}] {a.Title}: {a.Description}");
    }
}

// 2. Gui bai nop
var request = new CreateSubmissionRequest(1, "DTC245200859", "https://github.com/phamzduong/EduSubmit");
var response = await http.PostAsJsonAsync("/api/submissions", request);

if (response.IsSuccessStatusCode)
{
    var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
    Console.WriteLine("\n--- KET QUA NOP BAI ---");
    Console.WriteLine($"Ma luot nop (ID): {result?.Id}");
    Console.WriteLine($"Ma sinh vien   : {result?.StudentCode}");
    Console.WriteLine($"Trang thai     : {result?.Status}");
    Console.WriteLine($"Thoi gian nhan : {result?.ReceivedAt}");

    // 3. BƯỚC 12: Tra cứu lại lượt nộp vừa tạo qua API GET /api/submissions/{id}
    if (result != null)
    {
        Console.WriteLine("\n--- KIEM TRA TRA CUU LUOT NOP (GET BY ID) ---");
        var checkResponse = await http.GetAsync($"/api/submissions/{result.Id}");
        if (checkResponse.IsSuccessStatusCode)
        {
            var fetched = await checkResponse.Content.ReadFromJsonAsync<SubmissionResult>();
            Console.WriteLine($"[Thanh cong] Tra cuu ID {fetched?.Id} - SV: {fetched?.StudentCode} - Trang thai: {fetched?.Status}");
        }
        else
        {
            Console.WriteLine($"[That bai] Khong tra cuu duoc luot nop ID: {result.Id}");
        }
    }
}
else
{
    Console.WriteLine($"\nNop bai that bai. Ma loi: {response.StatusCode}");
}

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);