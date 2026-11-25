using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

// 1. Khởi tạo HttpClient để gọi RESTful API (Mục 2.2.5)
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// 2. Thiết lập kết nối SignalR và bật tự động kết nối lại (Mục 3.2.3 & 3.2.5)
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/queues")
    .WithAutomaticReconnect()
    .Build();

var numberAssignedTcs = new TaskCompletionSource<int>();

// Đăng ký lắng nghe sự kiện được cấp số thứ tự (Mục 3.2.3)
connection.On<QueueAssignedMessage>("QueueNumberAssigned", msg =>
{
    Console.WriteLine($"[SignalR] Lịch hẹn {msg.AppointmentId} đã được cấp số thứ tự: #{msg.QueueNumber} (Trạng thái: {msg.Status})");
    numberAssignedTcs.TrySetResult(msg.QueueNumber);
});

// Đăng ký lắng nghe sự kiện hàng đợi của bác sĩ thay đổi
connection.On<QueueUpdatedMessage>("QueueProgressUpdated", msg =>
{
    Console.WriteLine($"[SignalR] Phòng khám BS #{msg.DoctorId} đang mời số: #{msg.CurrentServingNumber} ({msg.ServingPatientName})");
});

await connection.StartAsync();
Console.WriteLine("Đã kết nối kênh thời gian thực SignalR.\n");

// 3. Gọi GET /doctors để xem danh sách bác sĩ
var doctors = await http.GetFromJsonAsync<List<DoctorDto>>("/doctors");
Console.WriteLine("Danh sách bác sĩ:");
foreach (var d in doctors ?? [])
{
    Console.WriteLine($"- [{d.Id}] {d.FullName} ({d.Specialty}) - Đang khám số: {d.CurrentServingNumber}");
}

// 4. Gọi GET /doctors/1/slots để lấy khung giờ trống
var slots = await http.GetFromJsonAsync<List<TimeSlotDto>>("/doctors/1/slots");
var selectedSlot = slots?.FirstOrDefault();
if (selectedSlot is null)
{
    Console.WriteLine("Không còn khung giờ trống.");
    return;
}

// 5. Gọi POST /appointments để đặt lịch khám
var createRequest = new CreateAppointmentRequest("Lê Minh Tâm", "0912345678", 1, selectedSlot.Id);
var createResponse = await http.PostAsJsonAsync("/appointments", createRequest);

// Mục 2.2.5 & 2.3.2: Luôn kiểm tra mã trạng thái HTTP trước khi đọc dữ liệu
if (createResponse.StatusCode != HttpStatusCode.Accepted)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var createdAppointment = await createResponse.Content.ReadFromJsonAsync<AppointmentDto>();
Console.WriteLine($"\nĐã gửi yêu cầu đặt lịch. Mã lịch hẹn: {createdAppointment!.Id}, Trạng thái ban đầu: {createdAppointment.Status}");

// 6. Tham gia nhóm SignalR để nhận thông báo riêng cho lịch hẹn và hàng đợi bác sĩ (Mục 3.2.4)
await connection.InvokeAsync("WatchAppointment", createdAppointment.Id.ToString());
await connection.InvokeAsync("WatchDoctorQueue", 1);

// Chờ nhận số thứ tự từ SignalR
await Task.WhenAny(numberAssignedTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

// 7. Gọi GET /queues/1 để kiểm tra trạng thái hàng đợi hiện tại
var queueInfo = await http.GetFromJsonAsync<QueueStatusDto>("/queues/1");
Console.WriteLine($"\nTrạng thái hàng đợi {queueInfo?.DoctorName}: Đang khám #{queueInfo?.CurrentServingNumber}, Đang chờ: {queueInfo?.TotalWaiting} người.");

// 8. Mô phỏng bác sĩ bấm gọi bệnh nhân tiếp theo
Console.WriteLine("\n--- Bác sĩ gọi lượt tiếp theo ---");
await http.PostAsync("/queues/1/next", null);
await Task.Delay(500);

// Hàm tiện ích đọc DTO lỗi ApiError (Mục 2.3.2)
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Lỗi {(int)response.StatusCode} ({response.StatusCode}): [{error?.Code}] {error?.Message}");
}

// Các record DTO khớp hợp đồng dữ liệu với Server
record DoctorDto(int Id, string FullName, string Specialty, int CurrentServingNumber);
record TimeSlotDto(int Id, int DoctorId, string TimeRange, bool IsBooked);
record CreateAppointmentRequest(string PatientName, string PhoneNumber, int DoctorId, int SlotId);
record AppointmentDto(Guid Id, string PatientName, string PhoneNumber, int DoctorId, int SlotId, int? QueueNumber, string Status, DateTimeOffset CreatedAt);
record QueueItemDto(Guid AppointmentId, int QueueNumber, string PatientName);
record QueueStatusDto(int DoctorId, string DoctorName, int CurrentServingNumber, int TotalWaiting, List<QueueItemDto> WaitingList);
record QueueAssignedMessage(Guid AppointmentId, int QueueNumber, string Status);
record QueueUpdatedMessage(int DoctorId, int CurrentServingNumber, string ServingPatientName, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);