
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// Dia chi API
var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

try
{
    // BUOC 1: Lay danh sach bai tap
    Console.WriteLine("===== DANH SACH BAI TAP =====");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments", jsonOptions);

    if (assignments != null)
    {
        foreach (var a in assignments)
        {
            Console.WriteLine(
                $"[{a.Id}] {a.Title} - Trang thai: {(a.IsOpen ? "Dang mo" : "Da dong")}");
        }
    }

    // BUOC 2: Nop bai tap
    Console.WriteLine("\n===== GUI BAI NOP =====");

    var createRequest = new CreateSubmissionRequest(
        "SV001",
        "https://github.com/SV001/EduSubmit"
    );

    var createResponse = await http.PostAsJsonAsync(
        "/api/assignments/1/submissions",
        createRequest
    );

    Guid submissionId = Guid.Empty;

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content
            .ReadFromJsonAsync<SubmissionDto>(jsonOptions);

        if (created != null)
        {
            submissionId = created.Id;

            Console.WriteLine("Nop bai thanh cong!");
            Console.WriteLine($"Ma bai nop: {created.Id}");
            Console.WriteLine($"Ma sinh vien: {created.StudentCode}");
            Console.WriteLine($"Trang thai: {created.Status}");
            Console.WriteLine($"Thoi gian: {created.ReceivedAt}");
        }
    }
    else
    {
        await ShowError(createResponse);
    }

    // BUOC 3: Doc lai bai nop
    if (submissionId != Guid.Empty)
    {
        Console.WriteLine("\n===== DOC LAI BAI NOP =====");

        var detailResponse = await http.GetAsync(
            $"/api/submissions/{submissionId}"
        );

        if (detailResponse.IsSuccessStatusCode)
        {
            var detail = await detailResponse.Content
                .ReadFromJsonAsync<SubmissionDto>(jsonOptions);

            Console.WriteLine($"Ma bai nop: {detail?.Id}");
            Console.WriteLine($"Ma bai tap: {detail?.AssignmentId}");
            Console.WriteLine($"Ma sinh vien: {detail?.StudentCode}");
            Console.WriteLine($"Repository: {detail?.RepositoryUrl}");
            Console.WriteLine($"Trang thai: {detail?.Status}");
        }
        else
        {
            await ShowError(detailResponse);
        }
    }

    // BUOC 4: Gui du lieu sai de kiem tra loi 400
    Console.WriteLine("\n===== KIEM TRA DU LIEU SAI =====");

    var invalidRequest = new CreateSubmissionRequest(
        "",
        "https://github.com/SV001/EduSubmit"
    );

    var invalidResponse = await http.PostAsJsonAsync(
        "/api/assignments/1/submissions",
        invalidRequest
    );

    Console.WriteLine(
        $"Trang thai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    if (!invalidResponse.IsSuccessStatusCode)
    {
        await ShowError(invalidResponse);
    }

    // BUOC 5: Kiem tra bai tap da dong
    Console.WriteLine("\n===== KIEM TRA BAI TAP DA DONG =====");

    var closedRequest = new CreateSubmissionRequest(
        "SV002",
        "https://github.com/SV002/EduSubmit"
    );

    var closedResponse = await http.PostAsJsonAsync(
        "/api/assignments/3/submissions",
        closedRequest
    );

    Console.WriteLine(
        $"Trang thai: {(int)closedResponse.StatusCode} {closedResponse.StatusCode}");

    if (!closedResponse.IsSuccessStatusCode)
    {
        await ShowError(closedResponse);
    }

    Console.WriteLine("\n===== HOAN THANH CLIENT =====");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Khong ket noi duoc API!");
    Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine("Co loi xay ra:");
    Console.WriteLine(ex.Message);
}

// HAM HIEN THI LOI
static async Task ShowError(HttpResponseMessage response)
{
    var content = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"Ma loi: {(int)response.StatusCode}");

    if (string.IsNullOrWhiteSpace(content))
    {
        Console.WriteLine("Khong co noi dung loi.");
        return;
    }

    try
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (root.TryGetProperty("code", out var code))
            Console.WriteLine($"Code: {code}");

        if (root.TryGetProperty("message", out var message))
            Console.WriteLine($"Message: {message}");
    }
    catch (JsonException)
    {
        Console.WriteLine(content);
    }
}

// DTO
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen
);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt
);
