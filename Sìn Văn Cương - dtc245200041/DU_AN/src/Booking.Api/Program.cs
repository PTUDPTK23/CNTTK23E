using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Chương 3 (Mục 3.2.1): Đăng ký dịch vụ SignalR vào container DI
builder.Services.AddSignalR();

var app = builder.Build();

// =========================================================================
// DỮ LIỆU MẪU TRONG BỘ NHỚ (Theo phong cách Chương 2 & 3, chưa dùng DB)
// =========================================================================
var dataLock = new object();

var doctors = new List<DoctorDto>
{
    new(1, "BS. Nguyễn Văn An", "Nội tổng quát", 0),
    new(2, "BS. Trần Thị Bình", "Tai Mũi Họng", 0)
};

var slots = new List<TimeSlotDto>
{
    new(101, 1, "08:00 - 08:30", false),
    new(102, 1, "08:30 - 09:00", false),
    new(201, 2, "09:00 - 09:30", false)
};

var appointments = new List<AppointmentDto>();

// =========================================================================
// CÁC ĐIỂM CUỐI RESTFUL API (Chương 1 & Chương 2)
// =========================================================================

// Kiểm tra dịch vụ đang hoạt động (Mục 1.3.2)
app.MapGet("/", () => Results.Ok(new
{
    service = "QueueAppointment.Api",
    status = "running"
}));

// 1. GET /doctors - Tra cứu danh sách bác sĩ / phòng khám (Mục 2.2.1 & 2.2.2)
app.MapGet("/doctors", (string? specialty) =>
{
    lock (dataLock)
    {
        var result = string.IsNullOrWhiteSpace(specialty)
            ? doctors
            : doctors.Where(d => d.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).ToList();

        return Results.Ok(result);
    }
});

// 2. GET /doctors/{id}/slots - Tra cứu khung giờ còn trống của bác sĩ (Mục 2.2.1)
app.MapGet("/doctors/{id:int}/slots", (int id) =>
{
    lock (dataLock)
    {
        var doctor = doctors.FirstOrDefault(d => d.Id == id);
        if (doctor is null)
        {
            return Results.NotFound(new ApiError("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ hoặc phòng khám."));
        }

        var availableSlots = slots
            .Where(s => s.DoctorId == id && !s.IsBooked)
            .ToList();

        return Results.Ok(availableSlots);
    }
});

// 3. POST /appointments - Đặt lịch khám & mô phỏng cấp số bất đồng bộ (Mục 2.1.1, 2.2.3 & 3.3.1)
app.MapPost("/appointments", (
    CreateAppointmentRequest request,
    IHubContext<QueueHub> hubContext) =>
{
    // Mục 2.2.3: Kiểm tra tính hợp lệ dữ liệu đầu vào tại biên hệ thống
    if (string.IsNullOrWhiteSpace(request.PatientName) ||
        string.IsNullOrWhiteSpace(request.PhoneNumber) ||
        request.DoctorId <= 0 ||
        request.SlotId <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_APPOINTMENT",
            "Vui lòng nhập đầy đủ tên bệnh nhân, số điện thoại, bác sĩ và khung giờ."));
    }

    AppointmentDto newAppointment;

    lock (dataLock)
    {
        var doctor = doctors.FirstOrDefault(d => d.Id == request.DoctorId);
        if (doctor is null)
        {
            return Results.NotFound(new ApiError("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ."));
        }

        var slotIndex = slots.FindIndex(s => s.Id == request.SlotId && s.DoctorId == request.DoctorId);
        if (slotIndex < 0)
        {
            return Results.NotFound(new ApiError("SLOT_NOT_FOUND", "Khung giờ không tồn tại."));
        }

        if (slots[slotIndex].IsBooked)
        {
            return Results.BadRequest(new ApiError("SLOT_ALREADY_BOOKED", "Khung giờ này đã có người đặt."));
        }

        // Đánh dấu khung giờ đã được đặt
        slots[slotIndex] = slots[slotIndex] with { IsBooked = true };

        newAppointment = new AppointmentDto(
            Id: Guid.NewGuid(),
            PatientName: request.PatientName.Trim(),
            PhoneNumber: request.PhoneNumber.Trim(),
            DoctorId: request.DoctorId,
            SlotId: request.SlotId,
            QueueNumber: null, // Chưa có số ngay, chờ tiến trình nền cấp số
            Status: "PendingQueue",
            CreatedAt: DateTimeOffset.UtcNow);

        appointments.Add(newAppointment);
    }

    // Mục 3.3.1: Mô phỏng luồng xử lý nền cấp số (thay cho RabbitMQ Worker ở chương 6)
    // và đẩy thông báo qua SignalR khi cấp số xong
    _ = SimulateQueueAssignmentAsync(newAppointment.Id, appointments, doctors, dataLock, hubContext);

    // Mục 2.1.1: Trả về 202 Accepted vì việc cấp số diễn ra bất đồng bộ, kèm URI tra cứu
    return Results.Accepted($"/appointments/{newAppointment.Id}", newAppointment);
});

// 4. GET /appointments/{id} - Tra cứu chi tiết lịch hẹn và số thứ tự (Mục 2.2.1 & 2.3.1)
app.MapGet("/appointments/{id:guid}", (Guid id) =>
{
    lock (dataLock)
    {
        var appointment = appointments.FirstOrDefault(a => a.Id == id);
        return appointment is null
            ? Results.NotFound(new ApiError("APPOINTMENT_NOT_FOUND", "Không tìm thấy lịch hẹn."))
            : Results.Ok(appointment);
    }
});

// 5. DELETE /appointments/{id} - Hủy lịch khám (Mục 2.1.2 & 2.2.4)
app.MapDelete("/appointments/{id:guid}", (Guid id) =>
{
    lock (dataLock)
    {
        var index = appointments.FindIndex(a => a.Id == id);
        if (index < 0)
        {
            return Results.NotFound(new ApiError("APPOINTMENT_NOT_FOUND", "Không tìm thấy lịch hẹn để hủy."));
        }

        var current = appointments[index];
        if (current.Status is "Completed" or "Cancelled")
        {
            return Results.BadRequest(new ApiError("CANNOT_CANCEL", "Không thể hủy lịch hẹn ở trạng thái hiện tại."));
        }

        // Cập nhật trạng thái hủy và giải phóng khung giờ
        appointments[index] = current with { Status = "Cancelled" };

        var slotIndex = slots.FindIndex(s => s.Id == current.SlotId);
        if (slotIndex >= 0)
        {
            slots[slotIndex] = slots[slotIndex] with { IsBooked = false };
        }

        return Results.NoContent(); // 204 No Content cho thao tác xóa thành công (Mục 2.1.3)
    }
});

// 6. GET /queues/{doctorId} - Tra cứu trạng thái hàng đợi hiện tại (Mục 2.2.1)
app.MapGet("/queues/{doctorId:int}", (int doctorId) =>
{
    lock (dataLock)
    {
        var doctor = doctors.FirstOrDefault(d => d.Id == doctorId);
        if (doctor is null)
        {
            return Results.NotFound(new ApiError("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ hoặc phòng khám."));
        }

        var waitingList = appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Queued" && a.QueueNumber.HasValue)
            .OrderBy(a => a.QueueNumber)
            .Select(a => new QueueItemDto(a.Id, a.QueueNumber!.Value, a.PatientName))
            .ToList();

        var queueStatus = new QueueStatusDto(
            DoctorId: doctor.Id,
            DoctorName: doctor.FullName,
            CurrentServingNumber: doctor.CurrentServingNumber,
            TotalWaiting: waitingList.Count,
            WaitingList: waitingList);

        return Results.Ok(queueStatus);
    }
});

// Chức năng 5 mở rộng: Bác sĩ gọi lượt tiếp theo và phát cập nhật thời gian thực qua SignalR
app.MapPost("/queues/{doctorId:int}/next", async (int doctorId, IHubContext<QueueHub> hubContext) =>
{
    QueueUpdatedMessage? pushMessage = null;

    lock (dataLock)
    {
        var docIndex = doctors.FindIndex(d => d.Id == doctorId);
        if (docIndex < 0)
        {
            return Results.NotFound(new ApiError("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ."));
        }

        var nextAppIndex = appointments
            .Select((appItem, idx) => new { appItem, idx })
            .Where(x => x.appItem.DoctorId == doctorId && x.appItem.Status == "Queued" && x.appItem.QueueNumber.HasValue)
            .OrderBy(x => x.appItem.QueueNumber)
            .Select(x => x.idx)
            .FirstOrDefault(-1);

        if (nextAppIndex < 0)
        {
            return Results.BadRequest(new ApiError("QUEUE_EMPTY", "Không còn bệnh nhân nào đang chờ trong hàng đợi."));
        }

        var nextApp = appointments[nextAppIndex];
        appointments[nextAppIndex] = nextApp with { Status = "Serving" };

        var updatedDoctor = doctors[docIndex] with { CurrentServingNumber = nextApp.QueueNumber!.Value };
        doctors[docIndex] = updatedDoctor;

        pushMessage = new QueueUpdatedMessage(
            DoctorId: doctorId,
            CurrentServingNumber: updatedDoctor.CurrentServingNumber,
            ServingPatientName: nextApp.PatientName,
            UpdatedAt: DateTimeOffset.UtcNow);
    }

    // Mục 3.2.4 & 3.3.1: Đẩy thông báo tới đúng nhóm client đang theo dõi bác sĩ này
    await hubContext.Clients
        .Group(QueueGroups.ForDoctor(doctorId))
        .SendAsync("QueueProgressUpdated", pushMessage);

    return Results.Ok(pushMessage);
});

// Chương 3 (Mục 3.2.1): Ánh xạ SignalR Hub vào tuyến /hubs/queues
app.MapHub<QueueHub>("/hubs/queues");

app.Run();

// =========================================================================
// HÀM MÔ PHỎNG XỬ LÝ NỀN CẤP SỐ THỨ TỰ (Theo mẫu Mục 3.3.1)
// =========================================================================
static async Task SimulateQueueAssignmentAsync(
    Guid appointmentId,
    List<AppointmentDto> appointments,
    List<DoctorDto> doctors,
    object dataLock,
    IHubContext<QueueHub> hubContext)
{
    // Giả lập độ trễ xử lý bất đồng bộ
    await Task.Delay(TimeSpan.FromSeconds(1));

    AppointmentDto? updatedApp = null;

    lock (dataLock)
    {
        var index = appointments.FindIndex(a => a.Id == appointmentId);
        if (index < 0 || appointments[index].Status == "Cancelled") return;

        var current = appointments[index];

        // Sinh số thứ tự kế tiếp cho bác sĩ đó
        var maxNumber = appointments
            .Where(a => a.DoctorId == current.DoctorId && a.QueueNumber.HasValue)
            .Select(a => a.QueueNumber!.Value)
            .DefaultIfEmpty(0)
            .Max();

        updatedApp = current with
        {
            QueueNumber = maxNumber + 1,
            Status = "Queued"
        };
        appointments[index] = updatedApp;
    }

    // Đẩy thông báo đã có số thứ tự về đúng bệnh nhân đang theo dõi lịch hẹn
    if (updatedApp is not null)
    {
        await hubContext.Clients
            .Group(QueueGroups.ForAppointment(appointmentId))
            .SendAsync("QueueNumberAssigned", new QueueAssignedMessage(
                updatedApp.Id,
                updatedApp.QueueNumber!.Value,
                updatedApp.Status));
    }
}

// =========================================================================
// SIGNALR HUB & QUẢN LÝ NHÓM (Chương 3 - Mục 3.2.2, 3.2.4 & 3.3.1)
// =========================================================================
sealed class QueueHub : Hub
{
    // Bệnh nhân đăng ký theo dõi kết quả cấp số cho lịch hẹn của mình
    public Task WatchAppointment(string appointmentId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, QueueGroups.ForAppointment(appointmentId));
    }

    // Bệnh nhân đăng ký theo dõi tiến độ hàng đợi của bác sĩ / phòng khám
    public Task WatchDoctorQueue(int doctorId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, QueueGroups.ForDoctor(doctorId));
    }
}

static class QueueGroups
{
    public static string ForAppointment(Guid id) => $"appointment:{id}";
    public static string ForAppointment(string id) => $"appointment:{id}";
    public static string ForDoctor(int doctorId) => $"doctor-queue:{doctorId}";
}

// =========================================================================
// CÁC DTO SỬ DỤNG RECORD (Chương 2 - Mục 2.1.4 & 2.2.3)
// =========================================================================
record DoctorDto(int Id, string FullName, string Specialty, int CurrentServingNumber);

record TimeSlotDto(int Id, int DoctorId, string TimeRange, bool IsBooked);

record CreateAppointmentRequest(
    string PatientName,
    string PhoneNumber,
    int DoctorId,
    int SlotId);

record AppointmentDto(
    Guid Id,
    string PatientName,
    string PhoneNumber,
    int DoctorId,
    int SlotId,
    int? QueueNumber,
    string Status,
    DateTimeOffset CreatedAt);

record QueueItemDto(Guid AppointmentId, int QueueNumber, string PatientName);

record QueueStatusDto(
    int DoctorId,
    string DoctorName,
    int CurrentServingNumber,
    int TotalWaiting,
    List<QueueItemDto> WaitingList);

record QueueAssignedMessage(Guid AppointmentId, int QueueNumber, string Status);

record QueueUpdatedMessage(int DoctorId, int CurrentServingNumber, string ServingPatientName, DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);