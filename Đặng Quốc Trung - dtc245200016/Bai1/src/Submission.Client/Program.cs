
using System.Net.Http.Json;

// Bai 1 - EduSubmit
// Submission.Client - Chuong trinh may khach
// Sinh vien: Dang Quoc Trung
// MSSV: DTC245200016

// 1. Lay dia chi API tu tham so dong lenh
var apiUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

// 2. Khoi tao HttpClient
using var client = new HttpClient
{
    BaseAddress = new Uri(apiUrl)
};

try
{
    // 3. Lay danh sach bai tap
    var assignments =
        await client.GetFromJsonAsync<List<Assignment>>(
            "/api/assignments"
        );

    Console.WriteLine("Danh sach bai tap:");

    if (assignments != null)
    {
        foreach (var item in assignments)
        {
            Console.WriteLine($"- {item.Id}: {item.Title}");
        }
    }

    // 4. Tao du lieu nop bai
    var request = new CreateSubmissionRequest(
        1,
        "DTC245200016",
        "https://github.com/trungdtt123-cmd/EduSubmit-DTC245200016"
    );

    // 5. Gui POST nop bai
    using var response = await client.PostAsJsonAsync(
        "/api/submissions",
        request
    );

    response.EnsureSuccessStatusCode();

    var result = await response.Content
        .ReadFromJsonAsync<SubmissionResult>();

    if (result != null)
    {
        Console.WriteLine("\nDa nop bai thanh cong!");
        Console.WriteLine($"Ma luot nop: {result.Id}");
        Console.WriteLine($"Trang thai: {result.Status}");

        // 6. Chuc nang mo rong:
        // Tra cuu luot nop vua tao theo GUID
        Console.WriteLine("\nDang tra cuu luot nop...");

        var saved = await client
            .GetFromJsonAsync<SubmissionResult>(
                $"/api/submissions/{result.Id}"
            );

        if (saved != null)
        {
            Console.WriteLine("Tra cuu thanh cong!");
            Console.WriteLine($"ID: {saved.Id}");
            Console.WriteLine($"MSSV: {saved.StudentCode}");
            Console.WriteLine($"Trang thai: {saved.Status}");
            Console.WriteLine($"Thoi gian: {saved.ReceivedAt}");
        }
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi HTTP: {ex.Message}");
}

// 7. Khai bao hop dong du lieu
record Assignment(
    int Id,
    string Title,
    string Description
);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl
);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt
);
