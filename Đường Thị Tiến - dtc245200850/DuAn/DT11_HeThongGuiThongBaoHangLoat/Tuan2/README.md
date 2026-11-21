# ĐỀ TÀI 11: HỆ THỐNG GỬI THÔNG BÁO HÀNG LOẠT (BULK NOTIFICATION SYSTEM)
**Sinh viên thực hiện:** Đường Thị Tiến  
**Mã sinh viên:** DTC245200850  
**Lớp:** CNTTK23E  
**Nền tảng:** C# .NET 10 | PostgreSQL 16 | RabbitMQ 3.13 (Management) | Docker Compose  

---

## 📖 1. Giới Thiệu Dự Án
Hệ thống gửi thông báo hàng loạt (**Bulk Notification System**) là giải pháp phần mềm hướng dịch vụ cho phép tạo, quản lý và phân phối thông báo đến số lượng lớn người nhận qua nhiều kênh khác nhau (Email, SMS, App Push notification).

Hệ thống được thiết kế theo kiến trúc phi tập trung, sử dụng Message Broker (**RabbitMQ**) làm bộ đệm hàng đợi thông điệp để tách biệt giữa tầng tiếp nhận yêu cầu (**Campaign.API**) và tầng xử lý gửi tin ngầm (**Notification.Worker**), đảm bảo khả năng chịu tải cao, không làm nghẽn hệ thống và tin cậy tuyệt đối.

---

## 🏗️ 2. Cấu Trúc Dự Án (Tuần 2)

```text
DT11_HeThongGuiThongBaoHangLoat/Tuan2/
├── BulkNotificationSystem.sln         # Solution .NET 10 quản lý toàn bộ 4 project
├── BulkNotificationSystem.slnx        # Solution XML format (.NET 10)
├── docker-compose.yml                 # Khởi chạy đồng thời PostgreSQL và RabbitMQ
├── nuget.config                       # Cấu hình NuGet package sources
├── README.md                          # Tài liệu hướng dẫn & báo cáo kết quả
├── scripts/
│   └── init_database.sql              # Script khởi tạo 3 bảng CSDL & dữ liệu mẫu
└── src/
    ├── Shared.Contracts/             # Thư viện dùng chung (Models, DTOs, Enums, DbContext)
    │   ├── Data/
    │   │   └── NotificationDbContext.cs
    │   ├── DTOs/
    │   │   ├── CampaignDtos.cs
    │   │   ├── NotificationDeliveryDto.cs
    │   │   ├── NotificationJobMessage.cs
    │   │   └── RecipientDtos.cs
    │   ├── Enums/
    │   │   └── Enums.cs
    │   └── Models/
    │       ├── Campaign.cs
    │       ├── NotificationDelivery.cs
    │       └── Recipient.cs
    │
    ├── Campaign.API/                 # Minimal API tiếp nhận chiến dịch & RabbitMQ Publisher
    │   ├── Services/
    │   │   └── RabbitMqPublisher.cs   # Dịch vụ phát message vào queue notification_queue
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Program.cs
    │
    ├── Notification.Worker/          # Dịch vụ Background Worker xử lý gửi tin
    │   ├── NotificationWorker.cs
    │   ├── WorkerSettings.cs
    │   ├── appsettings.json
    │   └── Program.cs
    │
    └── Admin.Client/                 # Ứng dụng Console quản trị tương tác với API
        ├── appsettings.json
        └── Program.cs
```

---

## 🗄️ 3. Thiết Kế Cơ Sở Dữ Liệu PostgreSQL (EF Core)

Hệ thống gồm 3 bảng dữ liệu chính theo đúng yêu cầu đề tài:

1. **`Campaigns`**: Lưu trữ thông tin chiến dịch thông báo.
   - `Id` (UUID, PK), `Name`, `Title`, `Content`, `Status` (Draft=0, Pending=1, Processing=2, Completed=3, Failed=4), `CreatedAt`, `CompletedAt`.
2. **`Recipients`**: Lưu danh bạ người nhận tin.
   - `Id` (UUID, PK), `FullName`, `Email`, `PhoneNumber`, `CreatedAt`.
3. **`NotificationDeliveries`**: Chi tiết từng lượt gửi thông báo cụ thể cho từng người nhận.
   - `JobId` (UUID, PK), `CampaignId` (FK), `RecipientId` (FK), `Status` (Pending=0, Sent=1, Failed=2), `RetryCount`, `ErrorMessage`, `CreatedAt`, `ProcessedAt`, `PublishedAt` (ghi nhận thời điểm message được xuất bản vào RabbitMQ).

> **Lưu ý cấu hình kết nối:** Toàn bộ chuỗi kết nối (`DefaultConnection` và `RabbitMQ`) được cấu hình qua file `appsettings.json` và biến môi trường, tuyệt đối không hardcode trong mã nguồn.

---

## 🚀 4. Báo Cáo Hoàn Thành Nhiệm Vụ Tuần 2: Tích Hợp RabbitMQ Vào Campaign.API

### 4.1. Cấu hình RabbitMQ trong `docker-compose.yml`
- Sử dụng image chính thức `rabbitmq:3-management`.
- Mở cổng `5672` (giao thức AMQP giao tiếp ứng dụng) và cổng `15672` (RabbitMQ Management Dashboard).
- Thiết lập tài khoản và mật khẩu thông qua biến môi trường: `RABBITMQ_DEFAULT_USER=guest`, `RABBITMQ_DEFAULT_PASS=guest`.
- Sử dụng volume `rabbitmq_data` để đảm bảo dữ liệu hàng đợi không bị mất khi container khởi động lại.

### 4.2. Tích hợp RabbitMQ Publisher vào `Campaign.API`
- Sử dụng thư viện `RabbitMQ.Client` phiên bản mới nhất (7.2.2) với toàn bộ phương thức bất đồng bộ (`async`/`await`).
- Triển khai interface `IRabbitMqPublisher` với class `RabbitMqPublisher`:
  - Khai báo hàng đợi bền vững: `channel.QueueDeclareAsync(queue: "notification_queue", durable: true, ...)`
  - Thiết lập tin nhắn bền vững: `BasicProperties { Persistent = true }` (tương đương `delivery_mode = 2`).
  - Gửi tin nhắn dạng JSON mã hóa UTF-8.

### 4.3. Endpoint Kích Hoạt Chiến Dịch (`POST /campaigns/{id}/start`)
- Hỗ trợ cả 2 đường dẫn: `POST /campaigns/{id}/start` và `POST /api/campaigns/{id}/start`.
- Khi được gọi:
  1. Kiểm tra tính sẵn sàng của RabbitMQ broker (`CanConnectAsync`). Nếu broker mất kết nối, trả về mã lỗi HTTP `503 Service Unavailable`.
  2. Lấy danh sách `NotificationDelivery` của chiến dịch có trạng thái `Pending` và chưa được xuất bản (`PublishedAt == null`).
  3. Tạo đối tượng `NotificationJobMessage` cho từng lượt gửi và đưa vào hàng đợi `notification_queue`.
  4. Cập nhật `PublishedAt = DateTime.UtcNow` cho từng lượt gửi thành công trong CSDL.
  5. Cập nhật trạng thái chiến dịch thành `Processing` (Status = 2). **Không** đánh dấu là `Completed` chỉ vì đã đẩy message vào queue (việc hoàn thành thuộc trách nhiệm của Worker sau khi gửi thực tế).

### 4.4. Cấu trúc Message DTO (`NotificationJobMessage`)
DTO trong `Shared.Contracts` được thiết kế tối ưu, bao gồm:
- `JobId`: Mã định danh duy nhất của lượt gửi.
- `CampaignId`: Mã chiến dịch tương ứng.
- `RecipientId`: Mã người nhận tin.
- `RecipientName`: Tên người nhận (để cá nhân hóa nội dung thông báo).
- `Destination`: Địa chỉ đích (Email hoặc Số điện thoại).
- `Title`: Tiêu đề thông báo.
- `Content`: Nội dung thông báo.
- `RetryCount`: Số lần thử lại ban đầu (= 0).
- `QueuedAt`: Thời điểm message được tạo và đưa vào hàng đợi.
- **Bảo mật:** Tuyệt đối không chứa mật khẩu, connection string hoặc thông tin bí mật trong payload.

### 4.5. Cơ chế Xử lý Lỗi & Chống Trùng Lặp (Idempotency)
- **Cơ chế Idempotency:** Nhờ cột `PublishedAt`, nếu API `/start` được gọi nhiều lần, hệ thống nhận diện các job đã được xuất bản (`unPublishedDeliveries.Count == 0`) và trả về `newlyPublishedCount: 0`, không đưa thêm message trùng lặp vào RabbitMQ.
- **Khả năng phục hồi sự cố từng phần:** Nếu trong quá trình xuất bản một lô message mà RabbitMQ gặp sự cố giữa chừng, những message đã gửi thành công trước đó đã được cập nhật `PublishedAt`. Khi hệ thống phục hồi và người dùng gọi lại `/start`, API sẽ tiếp tục phát các message còn lại mà không gây trùng lặp các message đã gửi.

### 4.6. Tuân thủ Phạm vi Yêu cầu
- Chưa chuyển `Notification.Worker` sang RabbitMQ Consumer ở tuần này (vẫn giữ nguyên Database Polling của Tuần 1 để kiểm tra độc lập).
- Không làm ảnh hưởng đến các chức năng nền tảng đã hoàn thành ở Tuần 1.

---

## 🧪 5. Hướng Dẫn Chạy & Kiểm Thử Thực Tế

### Bước 1: Khởi động PostgreSQL và RabbitMQ qua Docker
```powershell
docker-compose up -d
```
Kiểm tra container đang chạy:
```powershell
docker ps
```
- PostgreSQL: `localhost:5432`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ Management UI: [http://localhost:15672](http://localhost:15672) (User: `guest` / Pass: `guest`)

### Bước 2: Build Solution
```powershell
dotnet build BulkNotificationSystem.sln
```

### Bước 3: Khởi chạy Campaign.API
```powershell
dotnet run --project src/Campaign.API/Campaign.API.csproj --urls "http://localhost:5000"
```
Truy cập Swagger UI để kiểm tra tài liệu API: [http://localhost:5000/swagger](http://localhost:5000/swagger)  
Kiểm tra Health Check: [http://localhost:5000/health](http://localhost:5000/health)

### Bước 4: Tạo chiến dịch và Kích hoạt đưa Message vào RabbitMQ
1. **Tạo chiến dịch** (ví dụ qua PowerShell hoặc Swagger):
   ```powershell
   $body = @{
       name = "Chiến dịch Tri ân Khách hàng Tuần 2"
       title = "Thông báo quan trọng từ Hệ thống"
       content = "Xin chào {Name}, chúc bạn một ngày làm việc hiệu quả!"
       sendImmediately = $false
   } | ConvertTo-Json
   $campaign = Invoke-RestMethod -Uri "http://localhost:5000/campaigns" -Method Post -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -ContentType "application/json; charset=utf-8"
   ```

2. **Kích hoạt phát Message vào RabbitMQ**:
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:5000/campaigns/$($campaign.id)/start" -Method Post
   ```

3. **Kiểm tra trên RabbitMQ Management UI**:
   - Truy cập tab **Queues** -> chọn hàng đợi `notification_queue`.
   - Xác nhận số lượng message trong hàng đợi tương ứng với số người nhận của chiến dịch.
   - Mục **Get messages**: bấm **Get Message(s)** để xem nội dung JSON payload đã được xuất bản thành công với thuộc tính `delivery_mode: 2` (Persistent).

4. **Kiểm tra chống trùng lặp**:
   - Gọi lại lệnh Start ở trên một lần nữa.
   - Xác nhận API trả về `newlyPublishedCount: 0` và số lượng message trong `notification_queue` không bị tăng thêm.
