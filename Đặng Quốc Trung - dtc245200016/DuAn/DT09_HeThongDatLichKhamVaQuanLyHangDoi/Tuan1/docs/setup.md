# Hướng dẫn thiết lập và chạy local

## 1. Yêu cầu môi trường

- Windows và .NET SDK 10; repository đang ghim SDK `10.0.102` bằng `global.json`.
- SQL Server Express tại `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue` khi bước phát triển cần dữ liệu.
- Không sử dụng Docker. RabbitMQ/SignalR chưa cần chạy cho refactor MVC Prompt 02.2.1.

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

`TrustServerCertificate=True` chỉ dành cho môi trường phát triển cục bộ. Không hard-code mật khẩu/JWT secret, không commit User Secrets. Appointment API và Queue Worker đăng ký `MediQueueDbContext`; nếu thiếu cấu hình, host dừng với lỗi rõ ràng thay vì âm thầm dùng database khác.

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
| Web MVC | 5102 | 7102 | `GET http://localhost:5102/health`, `/` và `/Home/Error` |
| Notification Service | 5103 | 7103 | `GET http://localhost:5103/health` |
| Queue Worker | Không mở port | Không mở port | Log khởi động host |

Profile HTTP không tự chuyển hướng HTTPS trong Development để hỗ trợ smoke test terminal. Profile HTTPS dùng chứng chỉ phát triển của .NET. Kiểm tra ngày 17/11/2026 bằng `dotnet dev-certs https --check` trả về `No valid certificate found`; các port HTTPS đã cấu hình nhưng chưa smoke test được. Người dùng có thể chủ động tạo/trust chứng chỉ bằng quy trình .NET phù hợp khi cần; Prompt 02.1 không tự thay đổi kho chứng chỉ Windows.

## 5. Trạng thái database đã xác minh ngày 18/11/2026

- Windows Authentication kết nối thành công tới `LAPTOP-AQFGUM9Q\SQLEXPRESS` và truy cập được `MediQueue`.
- SQL Server `16.0.1000.6`, RTM, Express Edition (64-bit).
- Migration `20261117142359_InitialCreate` đã áp dụng bằng EF Core CLI sau khi tiền kiểm database.
- Có 21 bảng mô hình và `dbo.__EFMigrationsHistory`; không seed user, Role hay dữ liệu nghiệp vụ.
- `__EFMigrationsHistory` có một dòng `20261117142359_InitialCreate` / `10.0.2`.
- `docs/sql/VerifyDatabase.sql` đã chạy read-only thành công; xem chi tiết trong `docs/database-validation.md`.

## 6. Lệnh EF Core và quy trình review Migration

Kiểm tra công cụ:

```powershell
dotnet ef --version
```

Các lệnh dưới đây phải chạy từ `Tuan1` với biến môi trường chỉ trong phiên terminal:

```powershell
$env:ConnectionStrings__MediQueue = "Server=LAPTOP-AQFGUM9Q\SQLEXPRESS;Database=MediQueue;Trusted_Connection=True;TrustServerCertificate=True;"

dotnet ef migrations has-pending-model-changes `
  --project .\src\MediQueue.Infrastructure\MediQueue.Infrastructure.csproj `
  --startup-project .\src\MediQueue.Appointment.Api\MediQueue.Appointment.Api.csproj `
  --context MediQueueDbContext

dotnet ef migrations script 0 InitialCreate --idempotent `
  --project .\src\MediQueue.Infrastructure\MediQueue.Infrastructure.csproj `
  --startup-project .\src\MediQueue.Appointment.Api\MediQueue.Appointment.Api.csproj `
  --context MediQueueDbContext `
  --output .\docs\sql\InitialCreate.sql
```

`InitialCreate` đã tồn tại và đã áp dụng; không chạy `migrations add InitialCreate`, không sửa Migration History và không chạy lại script. Khi review Migration/script mới cần kiểm tra:

1. Không có `CREATE DATABASE`, `DROP DATABASE` hoặc seed tài khoản/mật khẩu.
2. Đúng bảng Identity và nghiệp vụ, PK/FK, nullability, kiểu dữ liệu.
3. Có unique index QueueNumber Doctor/ngày, filtered unique index một Doctor/một Serving và index Outbox/Inbox.
4. Có 12 check constraint, rowversion và delete behavior không cascade lịch sử nghiệp vụ.
5. `Down` tháo quan hệ theo thứ tự an toàn.

Mọi `dotnet ef database update` tiếp theo phải có phê duyệt mới và khảo sát database read-only. Không thêm `EnsureCreated`, `EnsureDeleted` hoặc `Database.Migrate` vào runtime.

## 7. Kiểm tra schema trong SSMS

1. Mở SQL Server Management Studio và kết nối bằng Windows Authentication tới `LAPTOP-AQFGUM9Q\SQLEXPRESS`.
2. Mở `Databases > MediQueue > Tables`, chọn `Refresh`; phải thấy 22 bảng tính cả `dbo.__EFMigrationsHistory`.
3. Mở `dbo.QueueTickets > Indexes > UX_QueueTickets_Doctor_Serving > Properties` và xác nhận unique/filtered index đang bật.
4. Chạy `docs/sql/VerifyDatabase.sql` trong query window đang chọn `MediQueue`; kết quả đầu phải là `ValidationStatus = OK`.
5. Kiểm tra result set Migration History có đúng `20261117142359_InitialCreate` và summary có `22` total table, `21` model table, `0` invalid FK/check.
6. Nếu cần nộp minh chứng, chụp ảnh thật Object Explorer, Migration History và result grid; không chụp/tạo minh chứng giả.

## 8. Chức năng chưa triển khai

Nền tảng hiện có MVC host/trang nền tảng, Domain entity/invariant, Identity persistence model, DbContext, Fluent API, Migration/script review và test metadata. Chưa có Login/Register/JWT, sáu API nghiệp vụ bắt buộc, transaction booking/queue thực tế, RabbitMQ business flow, SignalR Hub hoặc giao diện chức năng theo Role. Không dùng model/Migration làm bằng chứng cho các luồng nghiệp vụ chưa tồn tại.
