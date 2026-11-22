# Tuần 2 - Đề tài 18: Hệ thống Đăng ký Sự kiện và Check-in (DT18_HeThongDangKySuKienVaCheckIn)

**Sinh viên thực hiện:** Phan Quang Tùng  
**Mã sinh viên:** DTC245200250  
**Lớp:** K23.K1.D2.2627.CNTT K23E  
**Học phần:** Phát triển ứng dụng phân tán  
**Đề tài:** 18 – Hệ thống đăng ký sự kiện và check-in  

---

## 1. Giới thiệu nội dung Tuần 2: Tích hợp Giao tiếp Thời gian thực (SignalR)

Nối tiếp từ **Tuần 1 (Bài 1 & 2 - Xây dựng RESTful API cho Quản lý sự kiện, Đăng ký và Check-in)**, ở **Tuần 2**, hệ thống được nâng cấp và tích hợp kiến trúc giao tiếp thời gian thực với **SignalR (dựa trên kiến thức Bài 3)**:

```text
  [Người tham dự / Client]
             │
             │ (1) REST API: Đăng ký / Check-in / Hủy vé
             ▼
     ┌───────────────┐
     │ EventFlow.    │
     │ EventApi      │ (Cổng :5194)
     └───────┬───────┘
             │ (2) Phát sự kiện (RabbitMQ / Direct Webhook)
             ▼
     ┌───────────────┐
     │ EventFlow.    │  SignalR Hub: /hubs/dashboard
     │ Dashboard     │  (Cổng :5024)
     │ Service       │
     └───────┬───────┘
             │ (3) Broadcast song công qua SignalR
             ├──────────────────────────┐
             ▼                          ▼
  ┌───────────────────────┐  ┌───────────────────────┐
  │ EventFlow.            │  │ Dashboard Web Client  │
  │ DashboardClient       │  │ (Admin Dashboard)     │
  │ (Console Client)      │  │                       │
  └───────────────────────┘  └───────────────────────┘
```

### Các thành phần chính trong Tuần 2:
1. **`EventFlow.DashboardService` (SignalR Server):**
   - Đăng ký SignalR Hub tại endpoint: `/hubs/dashboard`.
   - Quản lý vòng đời kết nối client: `OnConnectedAsync`, `OnDisconnectedAsync`.
   - Hỗ trợ phân nhóm theo sự kiện: `JoinEventGroup(eventId)`, `LeaveEventGroup(eventId)`.
   - Dịch vụ trạng thái `DashboardStateService` quản lý số liệu KPI trực tiếp (Tổng sự kiện, Tổng đăng ký, Tổng check-in, Chỗ trống, Danh sách hoạt động gần nhất).
   - Phát sóng tự động qua `IHubContext<DashboardHub>`:
     - `ReceiveDashboardStats`: Cập nhật toàn bộ số liệu thống kê thời gian thực.
     - `ReceiveEventActivity`: Gửi chi tiết sự kiện vừa diễn ra (Đăng ký vé mới, Check-in tại cổng, Hủy vé).
   - Hỗ trợ cả 2 cơ chế tiếp nhận sự kiện: Tiêu thụ thông điệp nền qua RabbitMQ Consumer và tiếp nhận Webhook đồng bộ từ EventApi.

2. **`EventFlow.DashboardClient` (SignalR Client):**
   - Ứng dụng Console .NET 10 sử dụng thư viện `Microsoft.AspNetCore.SignalR.Client`.
   - Kết nối tới `http://localhost:5024/hubs/dashboard` với cơ chế tự động thử lại kết nối (`WithAutomaticReconnect`).
   - Lắng nghe và hiển thị trực quan dữ liệu phát sóng theo thời gian thực (được định dạng màu sắc theo từng loại sự kiện).

3. **`EventFlow.EventApi` (RESTful API):**
   - Kế thừa đầy đủ các API từ Tuần 1.
   - Khi có người tham dự đăng ký vé mới hoặc check-in, API tự động kích hoạt thông báo sang `DashboardService` để đẩy dữ liệu thời gian thực tới tất cả các client SignalR.

---

## 2. Cấu trúc thư mục Tuần 2

```text
Phan Quang Tùng - dtc245200250/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan2/
├── .gitignore
├── EventFlow.slnx                               # Solution bao gồm cả 4 dự án
├── README.md                                    # Tài liệu báo cáo Tuần 2
├── requests.http                                # File kiểm thử HTTP & SignalR
└── src/
    ├── EventFlow.DashboardClient/              # SignalR Client (Console)
    │   ├── EventFlow.DashboardClient.csproj
    │   └── Program.cs
    ├── EventFlow.DashboardService/             # SignalR Server & Hub
    │   ├── EventFlow.DashboardService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── Properties/launchSettings.json
    │   ├── Hubs/
    │   │   └── DashboardHub.cs
    │   └── Services/
    │       ├── DashboardRabbitMqConsumer.cs
    │       ├── DashboardStateService.cs
    │       └── IDashboardStateService.cs
    ├── EventFlow.EventApi/                     # RESTful API (Tuần 1)
    │   ├── EventFlow.EventApi.csproj
    │   ├── Program.cs
    │   ├── Data/EventFlowDbContext.cs
    │   ├── Entities/
    │   └── Services/
    └── EventFlow.Shared/                       # Thư viện dùng chung
        ├── EventFlow.Shared.csproj
        ├── Messaging/
        └── Models/
```

---

## 3. Danh sách Endpoint và SignalR Hub

| Dịch vụ | Giao thức / Endpoint | Chức năng |
|---|---|---|
| **DashboardService** | `POST /hubs/dashboard/negotiate?negotiateVersion=1` | Bắt tay kết nối SignalR (Handshake) |
| **DashboardService** | `WS/SSE /hubs/dashboard` | Kênh kết nối thời gian thực SignalR Hub |
| **DashboardService** | `GET /api/dashboard/stats` | Lấy số liệu KPI tổng hợp dạng JSON |
| **DashboardService** | `POST /api/dashboard/simulate` | Kích hoạt mô phỏng sự kiện thời gian thực |
| **DashboardService** | `POST /api/dashboard/notify/registration` | Nhận thông báo đăng ký vé và phát sóng |
| **DashboardService** | `POST /api/dashboard/notify/checkin` | Nhận thông báo check-in và phát sóng |
| **DashboardService** | `POST /api/dashboard/notify/cancel` | Nhận thông báo hủy vé và phát sóng |
| **DashboardService** | `GET /health` | Kiểm tra trạng thái Dashboard Service |
| **EventApi** | `GET /events` | Tra cứu danh sách sự kiện |
| **EventApi** | `POST /events/{id}/registrations` | Đăng ký tham dự sự kiện (tự động phát SignalR) |
| **EventApi** | `POST /registrations/{id}/checkin` | Check-in tại sự kiện (tự động phát SignalR) |

---

## 4. Hướng dẫn biên dịch và kiểm thử

### Bước 1: Di chuyển vào thư mục Tuần 2
```powershell
cd "Phan Quang Tùng - dtc245200250\DuAn\DT18_HeThongDangKySuKienVaCheckIn\Tuan2"
```

### Bước 2: Build toàn bộ Solution
```powershell
dotnet build EventFlow.slnx
```

### Bước 3: Chạy SignalR Dashboard Service (Cổng 5024)
Mở cửa sổ terminal thứ nhất:
```powershell
dotnet run --project src/EventFlow.DashboardService
```

### Bước 4: Chạy SignalR Client
Mở cửa sổ terminal thứ hai:
```powershell
dotnet run --project src/EventFlow.DashboardClient
```
*Client sẽ kết nối tới `http://localhost:5024/hubs/dashboard` và thông báo sẵn sàng lắng nghe sự kiện.*

### Bước 5: Chạy Event API Service (Cổng 5194)
Mở cửa sổ terminal thứ ba:
```powershell
dotnet run --project src/EventFlow.EventApi
```

### Bước 6: Kiểm thử truyền nhận thời gian thực
- Mở file [`requests.http`](requests.http) trong VS Code / Visual Studio.
- Chạy request `POST {{dashboardUrl}}/api/dashboard/simulate`: Cửa sổ `DashboardClient` sẽ ngay lập tức nhận được thông báo sự kiện và số liệu dashboard được cập nhật.
- Chạy request đăng ký vé `POST {{apiBaseUrl}}/events/{{eventId}}/registrations`: Cửa sổ `DashboardClient` sẽ hiển thị thông báo người tham dự vừa đăng ký thành công theo thời gian thực!
