# Hướng dẫn thiết lập và chạy local

## 1. Yêu cầu môi trường

- Windows và .NET SDK 10; repository đang ghim SDK `10.0.102` bằng `global.json`.
- SQL Server Express tại `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue` khi bước phát triển cần dữ liệu.
- Không sử dụng Docker. RabbitMQ/SignalR chưa cần chạy cho nền tảng Prompt 02.1.

Xác nhận môi trường:

```powershell
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
dotnet sln .\MediQueue.slnx list
```

## 2. Restore, build và test

Chạy từ thư mục `Tuan1`:

```powershell
dotnet restore .\MediQueue.slnx
dotnet build .\MediQueue.slnx --no-restore
dotnet test .\MediQueue.slnx --no-build
dotnet list .\MediQueue.slnx package --vulnerable --include-transitive --no-restore
```

## 3. Cấu hình SQL Server an toàn

Connection string phát triển dùng Windows Authentication:

```text
Server=LAPTOP-AQFGUM9Q\SQLEXPRESS;Database=MediQueue;Trusted_Connection=True;TrustServerCertificate=True;
```

Không ghi connection string hoặc secret vào `appsettings*.json` đã commit. Khi DbContext được triển khai, cấu hình local bằng một trong hai cách:

```powershell
dotnet user-secrets set "ConnectionStrings:MediQueue" "Server=LAPTOP-AQFGUM9Q\SQLEXPRESS;Database=MediQueue;Trusted_Connection=True;TrustServerCertificate=True;" --project .\src\MediQueue.Appointment.Api
```

hoặc biến môi trường phiên PowerShell:

```powershell
$env:ConnectionStrings__MediQueue = "Server=LAPTOP-AQFGUM9Q\SQLEXPRESS;Database=MediQueue;Trusted_Connection=True;TrustServerCertificate=True;"
```

`TrustServerCertificate=True` chỉ dành cho môi trường phát triển cục bộ. Không hard-code mật khẩu/JWT secret, không commit User Secrets. Prompt 02.1 chưa đọc connection string trong ứng dụng vì chưa có DbContext.

## 4. Chạy các tiến trình độc lập

Mở mỗi lệnh trong một cửa sổ terminal riêng:

```powershell
dotnet run --project .\src\MediQueue.Appointment.Api --launch-profile http
dotnet run --project .\src\MediQueue.Web --launch-profile http
dotnet run --project .\src\MediQueue.Notification.Service --launch-profile http
dotnet run --project .\src\MediQueue.Queue.Worker
```

| Dịch vụ | HTTP | HTTPS | Kiểm tra |
|---|---:|---:|---|
| Appointment API | 5101 | 7101 | `GET http://localhost:5101/health` |
| Web | 5102 | 7102 | `GET http://localhost:5102/health` và trang `/` |
| Notification Service | 5103 | 7103 | `GET http://localhost:5103/health` |
| Queue Worker | Không mở port | Không mở port | Log khởi động host |

Profile HTTP không tự chuyển hướng HTTPS trong Development để hỗ trợ smoke test terminal. Profile HTTPS dùng chứng chỉ phát triển của .NET. Kiểm tra ngày 17/11/2026 bằng `dotnet dev-certs https --check` trả về `No valid certificate found`; các port HTTPS đã cấu hình nhưng chưa smoke test được. Người dùng có thể chủ động tạo/trust chứng chỉ bằng quy trình .NET phù hợp khi cần; Prompt 02.1 không tự thay đổi kho chứng chỉ Windows.

## 5. Kết quả khảo sát database read-only ngày 17/11/2026

- Windows Authentication kết nối thành công tới `LAPTOP-AQFGUM9Q\SQLEXPRESS` và truy cập được `MediQueue`.
- SQL Server `16.0.1000.6`, RTM, Express Edition (64-bit).
- Database chỉ có các schema hệ thống/role cùng `dbo`, `guest`, `INFORMATION_SCHEMA`, `sys`.
- Không có user table; vì vậy chưa có cột, primary key, foreign key, unique constraint hoặc index người dùng để đối chiếu.
- Không tồn tại `dbo.__EFMigrationsHistory`.
- Chỉ truy vấn metadata; không đọc dữ liệu cá nhân, không thực thi lệnh ghi và không tạo/áp dụng Migration.

## 6. Chức năng chưa triển khai

Nền tảng hiện chỉ xác nhận cấu trúc solution, dependency, host và health endpoint. Chưa có DbContext/entity, Identity/JWT, sáu API nghiệp vụ bắt buộc, RabbitMQ business flow, SignalR Hub hoặc giao diện theo Role. Không dùng health endpoint làm bằng chứng cho các chức năng chưa tồn tại.
