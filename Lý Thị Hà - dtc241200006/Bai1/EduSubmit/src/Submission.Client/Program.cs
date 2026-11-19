using System.Net.Http.Json;

string apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using HttpClient client = new() {
    BaseAddress = new Uri(apiBaseUrl)
};

try {
    Console.WriteLine($"Đang kết nối tới API tại: {apiBaseUrl}...\n");

    // 1. Lấy danh sách bài tập
    var assignments = await client.GetFromJsonAsync<List<Assignment>>("/api/assignments");
    Console.WriteLine("Danh sách bài tập:");
    if (assignments != null) {
        foreach (var a in assignments) {
            Console.WriteLine($"  - {a.Id}: {a.Title} ({a.Description})");
        }
    }

    // 2. Chuẩn bị dữ liệu gửi bài nộp
    var newSubmission = new CreateSubmissionRequest(
        AssignmentId: 1,
        StudentCode: "dtc241200006",
        RepositoryUrl: "https://github.com/lyha8876-cmd/EduSubmit"
    );

    // 3. Gọi POST để nộp bài
    HttpResponseMessage response = await client.PostAsJsonAsync("/api/submissions", newSubmission);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
    
    if (result != null) {
        Console.WriteLine("\n[POST] Nộp bài thành công!");
        Console.WriteLine($"  - Mã lượt nộp (Guid): {result.Id}");
        Console.WriteLine($"  - Trạng thái: {result.Status}");

        // [BỔ SUNG BƯỚC 12] Gọi GET bằng ID vừa nhận được để đọc lại lượt nộp từ Server
        Console.WriteLine($"\nĐang gọi GET /api/submissions/{result.Id} để kiểm tra lại...");
        var fetchedSubmission = await client.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
        
        if (fetchedSubmission != null) {
            Console.WriteLine("[GET Thành công] Đã đọc lại lượt nộp từ API:");
            Console.WriteLine($"  - ID: {fetchedSubmission.Id}");
            Console.WriteLine($"  - Mã sinh viên: {fetchedSubmission.StudentCode}");
            Console.WriteLine($"  - Trạng thái: {fetchedSubmission.Status}");
            Console.WriteLine($"  - Thời gian nhận: {fetchedSubmission.ReceivedAt}");
        }
    }
}
catch (Exception ex) {
    Console.WriteLine($"[LỖI]: {ex.Message}");
}

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);