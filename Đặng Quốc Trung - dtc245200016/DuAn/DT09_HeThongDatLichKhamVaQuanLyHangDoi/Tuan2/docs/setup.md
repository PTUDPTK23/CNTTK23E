# Hướng dẫn thiết lập và chạy local Tuần 2

## 1. Yêu cầu môi trường

- Windows, Git và .NET SDK 10; `global.json` ghim SDK `10.0.102`.
- SQL Server Express `LAPTOP-AQFGUM9Q\SQLEXPRESS` với Windows Authentication.
- `MediQueue` đã áp dụng `InitialCreate` và là database duy nhất của dự án trong 9 tuần.
- Không tạo database theo tuần; không xóa/tạo lại `MediQueue`.
- Development HTTPS certificate hợp lệ/trusted cho `localhost`.
- Không Docker; Prompt 03.3.1 không cần RabbitMQ/Erlang hoặc SignalR nghiệp vụ.

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

Connection string local dùng Windows Authentication và đúng database chính thức `MediQueue`:

```text
Server=LAPTOP-AQFGUM9Q\SQLEXPRESS;Database=MediQueue;Trusted_Connection=True;TrustServerCertificate=True;
```

Quy tắc bắt buộc:

- Không commit connection string, JWT signing key, mật khẩu, token hoặc User Secrets.
- API và Worker dùng `UserSecretsId` riêng của `Tuan2`; không sao chép secret từ `Tuan1`.
- Không có connection string mặc định/fallback trong source.
- Không chạy `database update`, seed, `EnsureCreated`, `EnsureDeleted` hoặc `Database.Migrate` khi chưa được duyệt.
- Write integration test phải dùng database test cô lập được cấu hình riêng; tuyệt đối không ghi dữ liệu test vào `MediQueue`.
- `TrustServerCertificate=True` chỉ dành cho local development; không tự sửa/trust certificate Windows.

## 4. Chạy bốn tiến trình nền tảng

Chỉ chạy khi đã cấu hình User Secrets an toàn. Mỗi lệnh dùng một terminal:

```powershell
dotnet dev-certs https --check --trust
dotnet run --project .\src\MediQueue.Appointment.Api --launch-profile https
dotnet run --project .\src\MediQueue.Web --launch-profile https
dotnet run --project .\src\MediQueue.Notification.Service --launch-profile http
dotnet run --project .\src\MediQueue.Queue.Worker
```

| Dịch vụ | HTTPS | Kiểm tra nền tảng |
|---|---:|---|
| Appointment API | 7101 | `GET /health`, `/api/account/me` |
| Web MVC | 7102 | `GET /health`, `/`, `/account/login`, CSS/JS/font/SVG |
| Notification Service | 7103 | `GET /health` |
| Queue Worker | Không mở port | Host duy trì running |

`BackendApi:BaseAddress` mặc định là `https://localhost:7101/`; không thay bằng HTTP và không bypass certificate validation. Prompt 03.3.1 không yêu cầu RabbitMQ hoạt động. Sau smoke test phải chỉ dừng những process do chính lượt kiểm tra khởi động.

## 5. Baseline database Tuần 1 được kế thừa

Kết quả ngày 18/11/2026 được lưu để truy vết, không phải yêu cầu chạy lại:

- `MediQueue` online trên SQL Server Express, Windows Authentication.
- 21 bảng mô hình + `dbo.__EFMigrationsHistory`.
- Một Migration `20261117142359_InitialCreate` / EF Core `10.0.2`.
- Không seed user, Role hoặc dữ liệu nghiệp vụ.
- Không pending Migration/model change tại thời điểm nghiệm thu Tuần 1.

Chi tiết tại [database-validation.md](database-validation.md). Trong Tuần 2 chỉ được truy vấn metadata read-only sau khi xác minh đúng đích; không chạy lại `InitialCreate.sql`.

## 6. Quy trình EF Core cho giai đoạn sau

Prompt 03.2 không cần thay đổi schema hay Migration. Mọi Migration mới trong tương lai phải:

1. Có yêu cầu nghiệp vụ/schema rõ ràng và được sinh viên duyệt.
2. Khảo sát read-only `MediQueue`, review thay đổi và xin phép trước khi apply.
3. Review migration, designer, snapshot và SQL script; không sửa `InitialCreate` đã áp dụng.
4. Khảo sát metadata read-only, guard đúng database rồi mới xin phép áp dụng.
5. Xác minh schema, build/test và ghi bằng chứng thật.

## 7. Authentication đã triển khai

- API: ASP.NET Core Identity + JWT Bearer Access Token thời hạn hữu hạn, không Refresh Token v1.
- MVC: Cookie Authentication; JWT lưu trong token/session store phía server.
- Browser: chỉ nhận cookie `HttpOnly`, `Secure`, `SameSite`; form thay đổi trạng thái dùng antiforgery.
- Không đưa JWT vào JavaScript, HTML, `localStorage`, `sessionStorage`, URL hoặc log.
- Issuer `MediQueue.Appointment.Api`, audience `MediQueue.Web`, lifetime 30 phút, HS256 và clock skew tối đa 30 giây; signing key chỉ ở User Secrets/biến môi trường.
- Xem lệnh cấu hình, bootstrap và kiểm tra tại [authentication.md](authentication.md).

## 8. Chức năng chưa triển khai

Dashboard theo bốn Role đã triển khai tại `/dashboard` và API `/api/dashboard/me`. Chưa triển khai CRUD tài khoản/danh mục, lịch làm việc, lịch hẹn, hàng đợi, RabbitMQ business flow hoặc SignalR nghiệp vụ; giao diện không tạo liên kết giả cho các module này.

### Kiểm tra Dashboard thủ công

1. Chạy API và Web bằng profile `https`; không bypass chứng chỉ.
2. Đăng nhập tại `https://localhost:7102/account/login` bằng tài khoản hợp lệ; không đưa mật khẩu vào script/log.
3. Xác minh redirect `/dashboard`, nội dung/nhãn Role, sidebar responsive, Profile/ChangePassword và Logout.
4. SQL Server vẫn là `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue`; không chạy Migration hoặc seed cho Dashboard.

Nền tảng hiện có MVC/API host, Domain/EF, Identity, backend JWT/account management, MVC Cookie/token store/UI account và Dashboard theo bốn Role. Chưa có CRUD danh mục, sáu API nghiệp vụ bắt buộc, booking/queue transaction, RabbitMQ business flow hoặc SignalR Hub.

## 9. Giới hạn vận hành phiên MVC

- Token store là bộ nhớ của một Web process; restart Web yêu cầu đăng nhập lại.
- Cookie `.MediQueue.Auth` luôn Secure nên trình duyệt phải dùng `https://localhost:7102`.
- Development dùng Data Protection ephemeral và token store in-memory nên restart làm mất phiên. Non-Development dùng `.data-protection` (đã ignore) + Windows DPAPI; không xóa/regenerate key tùy tiện. Môi trường nhiều instance cần key ring/cache dùng chung trước khi scale-out.
- Không đặt `Jwt:SigningKey`, connection string hoặc token trong `appsettings*.json`.

