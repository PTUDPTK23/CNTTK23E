# MediQueue

MediQueue là hệ thống đặt lịch khám và quản lý hàng đợi cho một cơ sở y tế có nhiều chuyên khoa, bác sĩ và phòng khám.

- Sinh viên: Đặng Quốc Trung — `dtc245200016`
- Mã đề tài: 09 — Hệ thống đặt lịch khám và quản lý hàng đợi
- Phạm vi thư mục: Tuần 2 trong lộ trình 9 tuần
- Bốn vai trò: `Patient`, `Doctor`, `Receptionist`, `Admin`

## Phạm vi đã triển khai trong Tuần 2

- Đăng ký tài khoản Patient; đăng nhập chung cho bốn vai trò; đăng xuất, đổi mật khẩu và cập nhật hồ sơ.
- ASP.NET Core Identity, JWT Access Token cho API và Cookie Authentication cho MVC.
- Dashboard và điều hướng theo vai trò.
- Quản lý tài khoản Doctor/Receptionist dành cho Admin.
- Quản lý chuyên khoa, phòng khám, hồ sơ bác sĩ và hồ sơ bệnh nhân.
- Tìm kiếm, lọc, sắp xếp, phân trang, validation, audit và kiểm soát cập nhật đồng thời ở các module đã triển khai.
- Giao diện MVC responsive, có trạng thái loading/empty/error và hỗ trợ reduced motion.

Chức năng lịch làm việc, khung giờ, đặt/hủy lịch, RabbitMQ, Queue Worker và SignalR chưa được triển khai ở Tuần 2. Lộ trình còn lại được ghi tại [docs/roadmap.md](docs/roadmap.md).

## Công nghệ và kiến trúc

| Thành phần | Công nghệ |
|---|---|
| Frontend | ASP.NET Core MVC, Razor Views, HTML/CSS/JavaScript, Bootstrap 5 |
| Backend | ASP.NET Core Web API dùng Controller |
| Dữ liệu | EF Core 10, SQL Server Express, ASP.NET Core Identity |
| Bảo mật | JWT ngắn hạn cho API; Cookie HttpOnly/Secure và token store phía server cho MVC |
| Kiểm thử | xUnit, TestServer/WebApplicationFactory, SQL Server read-only opt-in |
| Runtime | .NET 10 trên Windows; không sử dụng Docker |

Solution có chín project:

```text
Tuan2/
├── src/
│   ├── MediQueue.Appointment.Api/
│   ├── MediQueue.Web/
│   ├── MediQueue.Queue.Worker/
│   ├── MediQueue.Notification.Service/
│   ├── MediQueue.Domain/
│   ├── MediQueue.Infrastructure/
│   └── MediQueue.Contracts/
├── tests/
│   ├── MediQueue.UnitTests/
│   └── MediQueue.IntegrationTests/
├── docs/
└── MediQueue.slnx
```

`MediQueue.Web` gọi backend bằng typed `HttpClient`; Web không tham chiếu `Infrastructure` hoặc `DbContext`. `Infrastructure` triển khai persistence/Identity cho API và các dịch vụ nền. Chi tiết xem [docs/architecture.md](docs/architecture.md).

## Yêu cầu môi trường

- .NET SDK theo `global.json` (`10.0.102` hoặc patch tương thích).
- SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS` và database hiện có `MediQueue` khi chạy chức năng dùng dữ liệu.
- HTTPS development certificate hợp lệ khi chạy luồng đăng nhập MVC.
- Không cần Docker.

## Cấu hình Development

Không lưu secret hoặc connection string nhạy cảm trong source control. Dùng User Secrets hoặc biến môi trường cho API:

```powershell
dotnet user-secrets set "ConnectionStrings:MediQueue" "<connection-string>" --project .\src\MediQueue.Appointment.Api
dotnet user-secrets set "Jwt:SigningKey" "<random-key-at-least-256-bits>" --project .\src\MediQueue.Appointment.Api
```

Web mặc định gọi API tại `https://localhost:7101/`; cấu hình nằm trong `src/MediQueue.Web/appsettings*.json`. Hướng dẫn đầy đủ và cách bootstrap Admin an toàn xem [docs/setup.md](docs/setup.md) và [docs/authentication.md](docs/authentication.md).

Ứng dụng không tự động chạy Migration khi khởi động. Không xóa hoặc tạo lại database `MediQueue`.

## Chạy ứng dụng

```powershell
dotnet restore .\MediQueue.slnx
dotnet build .\MediQueue.slnx --no-restore
dotnet run --project .\src\MediQueue.Appointment.Api --launch-profile https
dotnet run --project .\src\MediQueue.Web --launch-profile https
```

Các địa chỉ mặc định:

| Dịch vụ | HTTPS | Health |
|---|---|---|
| Appointment API | `https://localhost:7101` | `http://localhost:5101/health` |
| MVC Web | `https://localhost:7102` | `http://localhost:5102/health` |
| Notification Service | `https://localhost:7103` | `http://localhost:5103/health` |

Queue Worker và Notification Service hiện mới có nền tảng host/health, chưa có luồng RabbitMQ/SignalR nghiệp vụ.

## Kiểm thử

```powershell
dotnet test .\MediQueue.slnx --no-build
dotnet format .\MediQueue.slnx --verify-no-changes --no-restore
```

Bốn test SQL Server read-only chỉ chạy khi có biến `ConnectionStrings__MediQueue`; nếu không có, chúng được skip có chủ đích. Không dùng database phát triển chính thức cho test ghi dữ liệu.

## Tài liệu chính

- [Đối chiếu Đề tài 09](docs/topic09-requirements-traceability.md)
- [Báo cáo nghiệm thu Tuần 2](docs/week02-acceptance-report.md)
- [Yêu cầu hệ thống](docs/requirements.md)
- [Kiến trúc](docs/architecture.md)
- [Database](docs/database.md) và [kết quả xác minh schema](docs/database-validation.md)
- [API](docs/api.md), [xác thực](docs/authentication.md) và [Actor](docs/actors.md)
- [Design system](docs/ui-design-system.md)
- [Roadmap 9 tuần](docs/roadmap.md) và [tiến độ](docs/progress.md)

## Quy định nộp

Dự án được phát triển trong chín thư mục `Tuan1`–`Tuan9`. Mỗi tuần có một Pull Request và có thể có nhiều commit. Pull Request Tuần 2 dự kiến: `DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung`.
