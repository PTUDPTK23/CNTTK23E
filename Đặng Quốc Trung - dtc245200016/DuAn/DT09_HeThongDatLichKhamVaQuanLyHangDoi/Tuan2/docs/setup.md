# Hướng dẫn thiết lập và chạy local Tuần 2

## 1. Yêu cầu môi trường

- Windows, Git và .NET SDK 10; `global.json` ghim SDK `10.0.102`.
- SQL Server Express `LAPTOP-AQFGUM9Q\SQLEXPRESS` với Windows Authentication.
- `MediQueue` là database Tuần 1 đã áp dụng `InitialCreate` và phải bảo toàn.
- `MediQueue_Tuan2` là database phát triển **đề xuất**, hiện chưa được tạo và chưa được phép cấu hình để ghi.
- Không Docker; Prompt 03.1.1 không cần RabbitMQ/Erlang hoặc SignalR nghiệp vụ.

Kiểm tra môi trường từ `Tuan2`:

```powershell
git branch --show-current
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
dotnet ef --version
dotnet sln .\MediQueue.slnx list
```

## 2. Restore, build, test và format

```powershell
dotnet restore .\MediQueue.slnx
dotnet build .\MediQueue.slnx --no-restore
dotnet test .\MediQueue.slnx --no-build --no-restore
dotnet format .\MediQueue.slnx --verify-no-changes --no-restore
dotnet list .\MediQueue.slnx package --vulnerable --include-transitive --no-restore
```

Trước khi test, kiểm tra `ConnectionStrings__MediQueue`. Nếu biến không tồn tại, `MediQueueDatabaseReadTests` phải skip có thông báo; các test độc lập database vẫn chạy. Không đặt biến trỏ tới `MediQueue` chỉ để biến test skip thành pass.

## 3. Cấu hình database an toàn

Trong Prompt 03.1.1 không chạy lệnh cấu hình connection string. Sau khi sinh viên xác nhận tạo database Tuần 2, connection string dự kiến dùng Windows Authentication và đúng đích `MediQueue_Tuan2`:

```text
Server=LAPTOP-AQFGUM9Q\SQLEXPRESS;Database=MediQueue_Tuan2;Trusted_Connection=True;TrustServerCertificate=True;
```

Quy tắc bắt buộc:

- Không commit connection string, JWT signing key, mật khẩu, token hoặc User Secrets.
- API và Worker dùng `UserSecretsId` riêng của `Tuan2`; không sao chép secret từ `Tuan1`.
- Không có connection string mặc định/fallback trong source.
- Không chạy `database update`, seed, `EnsureCreated`, `EnsureDeleted` hoặc `Database.Migrate` khi chưa được duyệt.
- Write test tương lai phải guard chính xác database `MediQueue_Tuan2`; tuyệt đối không ghi vào `MediQueue`.
- `TrustServerCertificate=True` chỉ dành cho local development; không tự sửa/trust certificate Windows.

## 4. Chạy bốn tiến trình nền tảng

Chỉ chạy khi có cấu hình database Tuần 2 an toàn đã được xác nhận. Mỗi lệnh dùng một terminal:

```powershell
dotnet run --project .\src\MediQueue.Appointment.Api --launch-profile http
dotnet run --project .\src\MediQueue.Web --launch-profile http
dotnet run --project .\src\MediQueue.Notification.Service --launch-profile http
dotnet run --project .\src\MediQueue.Queue.Worker
```

| Dịch vụ | HTTP | Kiểm tra nền tảng |
|---|---:|---|
| Appointment API | 5101 | `GET /health` |
| Web MVC | 5102 | `GET /health`, `/`, `/Home/Error`, CSS/JS |
| Notification Service | 5103 | `GET /health` |
| Queue Worker | Không mở port | Host duy trì running |

Prompt 03.1.1 không yêu cầu RabbitMQ hoạt động. Sau smoke test phải chỉ dừng những process do chính lượt kiểm tra khởi động.

## 5. Baseline database Tuần 1 được kế thừa

Kết quả ngày 18/11/2026 được lưu để truy vết, không phải yêu cầu chạy lại:

- `MediQueue` online trên SQL Server Express, Windows Authentication.
- 21 bảng mô hình + `dbo.__EFMigrationsHistory`.
- Một Migration `20261117142359_InitialCreate` / EF Core `10.0.2`.
- Không seed user, Role hoặc dữ liệu nghiệp vụ.
- Không pending Migration/model change tại thời điểm nghiệm thu Tuần 1.

Chi tiết tại [database-validation.md](database-validation.md). Trong Tuần 2 chỉ được truy vấn metadata read-only sau khi xác minh đúng đích; không chạy lại `InitialCreate.sql`.

## 6. Quy trình EF Core cho giai đoạn sau

Prompt 03.1.1 chỉ được phép kiểm tra model/snapshot không ghi dữ liệu. Mọi Migration mới phải:

1. Có yêu cầu nghiệp vụ/schema rõ ràng và được sinh viên duyệt.
2. Dùng connection string của `MediQueue_Tuan2`, không phải `MediQueue`.
3. Review migration, designer, snapshot và SQL script; không sửa `InitialCreate` đã áp dụng.
4. Khảo sát metadata read-only, guard đúng database rồi mới xin phép áp dụng.
5. Xác minh schema, build/test và ghi bằng chứng thật.

## 7. Authentication dự kiến

- API: ASP.NET Core Identity + JWT Bearer Access Token thời hạn hữu hạn, không Refresh Token v1.
- MVC: Cookie Authentication; JWT lưu trong token/session store phía server.
- Browser: chỉ nhận cookie `HttpOnly`, `Secure`, `SameSite`; form thay đổi trạng thái dùng antiforgery.
- Không đưa JWT vào JavaScript, HTML, `localStorage`, `sessionStorage`, URL hoặc log.
- Các giá trị issuer/audience/lifetime/signing key chưa được cấu hình trong Prompt 03.1.1; xem [week02-plan.md](week02-plan.md).

## 8. Chức năng chưa triển khai

Nền tảng hiện có MVC/API host, Domain entity/invariant, Identity persistence model, DbContext, Fluent API, Migration/script review và test. Chưa có Login/Register/JWT/Cookie flow, dashboard Role, CRUD danh mục, sáu API nghiệp vụ bắt buộc, booking/queue transaction, RabbitMQ business flow hoặc SignalR Hub. Không dùng tài liệu thiết kế làm bằng chứng cho chức năng chưa tồn tại.

