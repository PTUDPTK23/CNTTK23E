# BulkNotificationSystem - Hệ Thống Gửi Thông Báo Hàng Loạt (.NET 10)

Hệ thống gửi thông báo hàng loạt được xây dựng trên nền tảng **C# / .NET 10**, sử dụng kiến trúc phân tán dạng Microservices/Worker với **PostgreSQL** và **Entity Framework Core**.

---

## 🏗️ Cấu Trúc Solution & Các Dự Án Con

Dự án bao gồm Solution `BulkNotificationSystem.sln` và thư mục `src/` chứa 4 dự án độc lập:

```text
d:\DuAn\
├── BulkNotificationSystem.sln
├── docker-compose.yml              # Tiện ích khởi chạy PostgreSQL nhanh bằng Docker
├── scripts/
│   └── init_database.sql           # Script khởi tạo bảng & dữ liệu mẫu PostgreSQL
└── src/
    ├── Shared.Contracts/          # [Thư viện dùng chung] Chứa Models, Enums, DTOs, DbContext
    │   ├── Data/
    │   │   └── NotificationDbContext.cs
    │   ├── DTOs/
    │   │   ├── CampaignDtos.cs
    │   │   ├── RecipientDtos.cs
    │   │   ├── NotificationDeliveryDto.cs
    │   │   └── NotificationJobMessage.cs
    │   ├── Enums/
    │   │   └── Enums.cs
    │   └── Models/
    │       ├── Campaign.cs
    │       ├── Recipient.cs
    │       └── NotificationDelivery.cs
    │
    ├── Campaign.API/              # [Minimal API] Dịch vụ tạo và quản lý chiến dịch thông báo
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Program.cs
    │
    ├── Notification.Worker/       # [Worker Service] Dịch vụ chạy ngầm nhận việc từ hàng đợi & gửi
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── NotificationWorker.cs
    │   ├── WorkerSettings.cs
    │   └── Program.cs
    │
    └── Admin.Client/              # [Console Client] Ứng dụng điều khiển cho người quản trị
        ├── appsettings.json
        └── Program.cs
```

---

## 🗄️ Thiết Kế Cơ Sở Dữ Liệu PostgreSQL (Entity Framework Core)

Hệ thống bao gồm 3 bảng dữ liệu chính theo đúng yêu cầu:

### 1. Bảng `Campaigns` (Thông tin chiến dịch)
| Cột | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Id` | UUID (PK) | Mã định danh chiến dịch |
| `Name` | VARCHAR(200) | Tên chiến dịch |
| `Title` | VARCHAR(250) | Tiêu đề thông báo |
| `Content` | TEXT | Nội dung thông báo |
| `Status` | VARCHAR(50) | Trạng thái: `Draft`, `Pending`, `Processing`, `Completed`, `Failed` |
| `CreatedAt` | TIMESTAMPTZ | Ngày và giờ tạo chiến dịch (UTC) |
| `CompletedAt` | TIMESTAMPTZ | Thời điểm chiến dịch hoàn tất |

### 2. Bảng `Recipients` (Danh sách người nhận)
| Cột | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Id` | UUID (PK) | Mã người nhận |
| `FullName` | VARCHAR(150) | Họ và tên người nhận |
| `Email` | VARCHAR(255) | Địa chỉ Email |
| `PhoneNumber` | VARCHAR(30) | Số điện thoại nhận tin SMS |
| `CreatedAt` | TIMESTAMPTZ | Ngày thêm người nhận |

### 3. Bảng `NotificationDeliveries` (Chi tiết từng lượt gửi)
| Cột | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `JobId` | UUID (PK) | Mã tác vụ (job) gửi tin |
| `CampaignId` | UUID (FK) | Mã chiến dịch liên kết (`Campaigns.Id`) |
| `RecipientId` | UUID (FK) | Mã người nhận (`Recipients.Id`) |
| `Status` | VARCHAR(50) | Trạng thái: `Pending`, `Sent`, `Failed` |
| `RetryCount` | INT | Số lần đã thử lại (mặc định: 0) |
| `ErrorMessage` | TEXT | Nội dung thông báo lỗi chi tiết khi gửi thất bại |
| `CreatedAt` | TIMESTAMPTZ | Ngày tạo tác vụ |
| `ProcessedAt` | TIMESTAMPTZ | Thời điểm thực hiện xử lý gần nhất |

---

## ⚙️ Cấu Hình Kết Nối Cơ Sở Dữ Liệu (Zero Hardcoding)

Toàn bộ chuỗi kết nối (Connection String) được tách riêng độc lập ra file cấu hình `appsettings.json` và hỗ trợ biến môi trường, tuyệt đối không viết cứng trong mã nguồn:

### Trong `appsettings.json` của Campaign.API và Notification.Worker:
```json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Port=5432;Database=BulkNotificationDb;Username=postgres;Password=postgres"
  }
}
```

### Hỗ trợ ghi đè qua biến môi trường (Environment Variable):
```bash
# Windows PowerShell
$env:ConnectionStrings__PostgreSql="Host=my-prod-db;Port=5432;Database=BulkNotificationDb;Username=admin;Password=secret"

# Linux / Docker
export ConnectionStrings__PostgreSql="Host=my-prod-db;Port=5432;Database=BulkNotificationDb;Username=admin;Password=secret"
```

---

## 🚀 Hướng Dẫn Khởi Chạy

### Bước 1: Khởi động PostgreSQL
Bạn có thể dùng Docker Compose có sẵn:
```bash
docker compose up -d
```
*(Hoặc dùng PostgreSQL đã cài đặt sẵn trên máy với Port 5432).*

### Bước 2: Chạy dịch vụ API (`Campaign.API`)
Mở cửa sổ dòng lệnh thứ nhất:
```bash
cd src/Campaign.API
dotnet run
```
- API sẽ lắng nghe tại: `http://localhost:5000`
- Giao diện Swagger UI: Mở trình duyệt tại `http://localhost:5000`
- API tự động kiểm tra và khởi tạo các bảng CSDL cùng người nhận mẫu khi khởi chạy.

### Bước 3: Chạy dịch vụ nền (`Notification.Worker`)
Mở cửa sổ dòng lệnh thứ hai:
```bash
cd src/Notification.Worker
dotnet run
```
- Worker sẽ tự động quét các tác vụ `Pending` từ bảng `NotificationDeliveries`.
- Giả lập độ trễ mạng, tỷ lệ thành công/thất bại và cơ chế tự động thử lại (Retry) tối đa 3 lần.

### Bước 4: Chạy giao diện quản trị (`Admin.Client`)
Mở cửa sổ dòng lệnh thứ ba:
```bash
cd src/Admin.Client
dotnet run
```
Giao diện Console tương tác sẽ hiển thị menu:
1. Xem danh sách chiến dịch và số liệu thống kê (`Total`, `Sent`, `Failed`, `Pending`).
2. Tạo mới chiến dịch thông báo gửi hàng loạt.
3. Xem chi tiết chiến dịch và danh sách từng lượt gửi kèm lỗi và số lần Retry.
4. Xem danh sách người nhận.
5. Thêm người nhận mới.
6. Kích hoạt phát chiến dịch.
7. Giám sát tiến độ trực tiếp thời gian thực (`Live Monitor`).
8. Kiểm tra tình trạng kết nối API & CSDL (`Health Check`).
