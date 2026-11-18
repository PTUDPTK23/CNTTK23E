using System.Net;
using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh hoặc dùng mặc định cổng 5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

Console.WriteLine($"=== DANG KET NOI TOI MAY CHU: {apiBaseUrl} ===");

try
{
    // Nấc 1: Lấy danh sách tài liệu hiện có trên hệ thống
    var documents = await http.GetFromJsonAsync<List<DocumentDto>>("/documents");
    Console.WriteLine("\n[Buoc 1] Danh sach tai lieu hien co tren Server:");
    foreach (var d in documents ?? [])
    {
        Console.WriteLine($"- [{d.Id}] {d.Title} ({d.Creator}) ---> Trang thai: {d.Status}");
    }

    // Nấc 2: Tạo tài liệu mới (POST /documents)
    Console.WriteLine("\n[Buoc 2] Dang gui yeu cau tao tai lieu moi...");
    var createRequest = new CreateDocumentRequest("De an phan tan", "Xay dung he thong phe duyet", "Lý Thị Hà");
    var createResponse = await http.PostAsJsonAsync("/documents", createRequest);

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Console.WriteLine($"-> Tao thanh cong! ID moi la: [{created?.Id}] | Trang thai: {created?.Status}");
        
        int newId = created!.Id;

        // Nấc 3: Gửi tài liệu vào quy trình phê duyệt (POST /documents/{id}/submit)
        Console.WriteLine($"\n[Buoc 3] Gui tai lieu ID = {newId} di phe duyet (Submit)...");
        var submitResponse = await http.PostAsync($"/documents/{newId}/submit", null);
        
        if (submitResponse.IsSuccessStatusCode)
        {
            var submittedDoc = await submitResponse.Content.ReadFromJsonAsync<DocumentDto>();
            Console.WriteLine($"-> Da gui thanh cong! Trang thai moi: {submittedDoc?.Status}");
        }
        else
        {
            await PrintApiErrorAsync(submitResponse);
        }
    }
    else
    {
        await PrintApiErrorAsync(createResponse);
    }

    // Nấc 4: Cố ý gửi dữ liệu sai để quan sát lỗi 400 Bad Request
    Console.WriteLine("\n[Buoc 4] Thu gui du lieu loi (tieu de trong) de kiem tra co che xu ly loi...");
    var invalidResponse = await http.PostAsJsonAsync("/documents", new CreateDocumentRequest("", "", "Lý Thị Hà"));
    Console.WriteLine($"Nhan duoc ma trang thai tu Server: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LOI MANG] Khong the ket noi toi may chu: {ex.Message}");
    Console.WriteLine("Hay chac chan rang 'Document.Api' da duoc khoi dong va dang lang nghe o cong 5000!");
}

// Hàm hỗ trợ đọc thông điệp lỗi chuẩn (ApiError) từ máy chủ trả về
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    try
    {
        var error = await r.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Chi tiet loi tu Server -> Ma: {error?.Code} | Thong diep: {error?.Message}");
    }
    catch
    {
        var rawText = await r.Content.ReadAsStringAsync();
        Console.WriteLine($"Noi dung phan hoi loi tho: {rawText}");
    }
}

// Khai báo bản sao DTO ở máy khách để khớp hợp đồng dữ liệu với máy chủ
record DocumentDto(int Id, string Title, string Description, string Creator, string Status, DateTimeOffset CreatedAt);
record CreateDocumentRequest(string Title, string Description, string Creator);
record ApiError(string Code, string Message);