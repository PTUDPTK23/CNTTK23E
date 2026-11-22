using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

// Kênh 1: REST Client thông thường
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR HubConnection kết nối tới đường dẫn Hub của server[cite: 59]
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/documents")
    .WithAutomaticReconnect()
    .Build();

// Tạo "cái chuông" (TaskCompletionSource) để chờ tín hiệu kết quả phê duyệt thời gian thực[cite: 59]
var approvalNotification = new TaskCompletionSource();

connection.On<DocumentStatusMessage>("DocumentStatusChanged", m => {
    Console.WriteLine($"\n[Realtime Push] -> Tài liệu ID: {m.DocumentId} | Trạng thái mới: {m.Status} (Lúc: {m.UpdatedAt:HH:mm:ss})");
    
    // Nếu trạng thái đạt mốc kết thúc (Approved hoặc Rejected), đánh dấu hoàn thành chuông chờ[cite: 59]
    if (m.Status == "Approved" || m.Status == "Rejected")
    {
        approvalNotification.TrySetResult();
    }
});

// Bắt đầu mở kết nối SignalR (thực hiện bắt tay negotiate và mở WebSocket)[cite: 59]
await connection.StartAsync();
Console.WriteLine($"=== KET NOI THANH CONG (REST + SignalR) TOI: {apiBaseUrl} ===");

try
{
    // Bước 1: Lấy danh sách tài liệu hiện có qua REST
    var documents = await http.GetFromJsonAsync<List<DocumentDto>>("/documents");
    Console.WriteLine("\n[Buoc 1] Danh sach tai lieu hien co tren Server:");
    foreach (var d in documents ?? [])
    {
        Console.WriteLine($"- [{d.Id}] {d.Title} ({d.Creator}) ---> Trang thai: {d.Status}");
    }

    // Bước 2: Tạo tài liệu mới qua REST (POST /documents)
    Console.WriteLine("\n[Buoc 2] Dang gui yeu cau tao tai lieu moi...");
    var createRequest = new CreateDocumentRequest("De an phan tan", "Xay dung he thong phe duyet", "Lý Thị Hà");
    var createResponse = await http.PostAsJsonAsync("/documents", createRequest);

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Console.WriteLine($"-> Tao thanh cong! ID moi la: [{created?.Id}] | Trang thai: {created?.Status}");
        
        int newId = created!.Id;

        // QUAN TRỌNG: Gọi Hub để đưa kết nối này vào nhóm theo dõi riêng cho tài liệu vừa tạo (WatchDocument)[cite: 60]
        await connection.InvokeAsync("WatchDocument", newId.ToString());
        Console.WriteLine($"-> Da dang ky theo doi thoi gian thuc (WatchDocument) cho tai lieu ID = {newId}");

        // Bước 3: Gửi tài liệu đi phê duyệt (POST /documents/{id}/submit)
        Console.WriteLine($"\n[Buoc 3] Gui tai lieu ID = {newId} di phe duyet (Submit)...");
        var submitResponse = await http.PostAsync($"/documents/{newId}/submit", null);
        
        if (submitResponse.IsSuccessStatusCode)
        {
            var submittedDoc = await submitResponse.Content.ReadFromJsonAsync<DocumentDto>();
            Console.WriteLine($"-> Da gui thanh cong! Trang thai hien tai: {submittedDoc?.Status}");
            
            Console.WriteLine("\n[Dang cho thong bao Realtime tu Server...]");
            
            // Chờ thông báo đẩy về từ server qua chuông (tối đa 10 giây)[cite: 60]
            var completedTask = await Task.WhenAny(approvalNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            if (completedTask != approvalNotification.Task)
            {
                Console.WriteLine("Qua thoi gian cho thong bao realtime tu server.");
            }
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

    // Quy tắc vàng: Trạng thái cuối cùng luôn chốt lại bằng REST (Nguồn sự thật)[cite: 16, 60]
    Console.WriteLine("\n[Buoc 4] Doc lai trang thai cuoi cung bang REST...");
    var latestDoc = await http.GetFromJsonAsync<DocumentDto>($"/documents/{createResponse.Headers.Location?.Segments.Last()}");
    // Hoặc lấy danh sách tổng hợp
    var latestDocs = await http.GetFromJsonAsync<List<DocumentDto>>("/documents");
    foreach (var d in latestDocs ?? [])
    {
        Console.WriteLine($"- [{d.Id}] {d.Title} ---> Trang thai chot cuoi: {d.Status}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LOI MANG] Khong the ket noi toi may chu: {ex.Message}");
}

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    try
    {
        var error = await r.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Chi tiet loi tu Server -> Ma: {error?.Code} | Thong diep: {error?.Message}");
    }
    catch
    {
        var rawText = await r.Content.ReadAsStringAsync();
        Console.WriteLine($"Noi dung loi tho: {rawText}");
    }
}

// Khai báo DTO và Record phía Client
record DocumentDto(int Id, string Title, string Description, string Creator, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
record CreateDocumentRequest(string Title, string Description, string Creator);
record DocumentStatusMessage(int DocumentId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);