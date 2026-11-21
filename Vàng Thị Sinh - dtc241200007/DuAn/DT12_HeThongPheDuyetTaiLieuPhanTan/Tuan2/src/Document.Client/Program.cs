using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var submittedBy = args.Length > 1 ? args[1] : "VangThiSinh";
var fileUrl = args.Length > 2 ? args[2] : "https://ictu.edu.vn/docs/de-cuong-dt12.pdf";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/approvals")
    .WithAutomaticReconnect()
    .Build();

var finalDecisionSignal = new TaskCompletionSource();

// Đăng ký nhận thông báo TRƯỚC khi StartAsync
connection.On<DocumentStatusMessage>("DocumentStatusChanged", msg =>
{
    Console.WriteLine($"[SignalR] Tai lieu {msg.DocumentId.ToString()[..8]} -> {msg.Status} ({msg.Note}) luc {msg.UpdatedAt:HH:mm:ss}");
    if (msg.Status is "approved" or "rejected")
    {
        finalDecisionSignal.TrySetResult();
    }
});

connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Mat ket noi SignalR, dang thu ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai SignalR (ConnectionId moi: {newId?[..8]})");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR den ApprovalHub.");

// 1. Gửi yêu cầu phê duyệt tài liệu bằng REST POST
var createReq = new CreateDocumentRequest(
    Title: "Bao cao kien truc he thong phan tan DT12",
    SubmittedBy: submittedBy,
    FileUrl: fileUrl
);

var response = await http.PostAsJsonAsync("/api/documents", createReq);
if (response.StatusCode != HttpStatusCode.Created)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao tai lieu: {error?.Code} - {error?.Message}");
    return;
}

var created = await response.Content.ReadFromJsonAsync<DocumentDto>();
Console.WriteLine($"Da gui tai lieu: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 2. Đăng ký vào nhóm của tài liệu vừa tạo
await connection.InvokeAsync("WatchDocument", created.Id.ToString());
Console.WriteLine("Dang cho thong bao phe duyet thoi gian thuc...");

// 3. Chờ kết quả cuối (approved/rejected) hoặc tối đa 10 giây
var completed = await Task.WhenAny(finalDecisionSignal.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != finalDecisionSignal.Task)
{
    Console.WriteLine("Het thoi gian cho thong bao SignalR.");
}

// 4. Quy tắc vàng: Luôn đọc lại trạng thái cuối cùng bằng REST
var latest = await http.GetFromJsonAsync<DocumentDto>($"/api/documents/{created.Id}");
Console.WriteLine($"Trang thai chot lai bang REST: {latest?.Status} (Cap nhat luc {latest?.UpdatedAt:HH:mm:ss})");

record CreateDocumentRequest(string Title, string SubmittedBy, string? FileUrl);
record DocumentDto(Guid Id, string Title, string SubmittedBy, string FileUrl, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
record DocumentStatusMessage(Guid DocumentId, string Status, string Note, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);