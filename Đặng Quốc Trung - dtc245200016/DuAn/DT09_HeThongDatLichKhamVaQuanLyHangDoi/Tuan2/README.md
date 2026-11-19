# MediQueue

**Mã đề tài:** 09  
**Tên đề tài:** Hệ thống đặt lịch khám và quản lý hàng đợi  
**Tên tiếng Anh:** Medical Appointment and Queue Management System  
**Sinh viên:** Đặng Quốc Trung — `dtc245200016`

## Quy định nộp dự án

- Dự án được phát triển trong 9 tuần tại `DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi`.
- Mỗi tuần có một thư mục `Tuan1` đến `Tuan9` và đúng một Pull Request; một Pull Request có thể gồm nhiều commit thực tế.
- Tuần sau kế thừa kết quả tuần trước, không làm lại dự án từ đầu và không tạo lịch sử giả.
- Tuần 1 được kế thừa từ commit `dcee864`; không sửa lịch sử hoặc thư mục `Tuan1`.
- Pull Request Tuần 2 dự kiến: `DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung`; hạn nộp Tuần 2 chưa được cung cấp.
- Sinh viên tự review, commit, push và tạo Pull Request; Codex không tự thực hiện các thao tác này.

## 1. Tổng quan

MediQueue phục vụ một cơ sở y tế có nhiều chuyên khoa, bác sĩ và phòng khám. Hệ thống hỗ trợ Patient tra cứu bác sĩ/slot, đặt hoặc hủy lịch, nhận số thứ tự và theo dõi hàng đợi; Doctor điều phối hàng đợi của mình; Receptionist hỗ trợ lịch hẹn và tiếp nhận; Admin quản lý dữ liệu nền và tài khoản.

Bốn Role duy nhất là `Patient`, `Doctor`, `Receptionist`, `Admin`. Một website sử dụng một form đăng nhập chung và điều hướng giao diện theo Role. Mỗi Doctor thuộc đúng một Department (chuyên khoa); hệ thống không có mô hình đa chi nhánh.

Hai luồng phân tán cốt lõi:

- Appointment thành công tạo yêu cầu cấp số qua transactional outbox/RabbitMQ; `MediQueue.Queue.Worker` xử lý bất đồng bộ, idempotent.
- Thay đổi queue được phát qua RabbitMQ; `MediQueue.Notification.Service` chuyển tiếp tới client đã xác thực bằng SignalR.

QueueNumber tăng riêng theo Doctor/ngày, bắt đầu từ 1 mỗi ngày, không tái sử dụng và hiển thị `001`, `002`, `003`, ... Trạng thái Appointment: `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`.

## 2. Nền tảng kế thừa và phạm vi Tuần 2

`Tuan2` kế thừa chính xác 177 tracked file từ commit Tuần 1 `dcee864` bằng `git archive`. Nền tảng được bảo toàn:

- Solution .NET 10 gồm 7 project ứng dụng/thư viện và 2 project xUnit.
- ProjectReference một chiều, không có vòng phụ thuộc.
- Enum trạng thái và hằng Role nền tảng; chưa có entity/DbContext nghiệp vụ.
- Health endpoint thật cho API, Web và Notification Service; Worker chỉ xác nhận host khởi động.
- Unit test cho trạng thái/Role và integration test cho health API.
- Khảo sát metadata database `MediQueue` read-only trước khi áp dụng Migration.
- Xây dựng Domain entity/invariant, ASP.NET Core Identity model, `MediQueueDbContext`, Fluent API và kiểm thử metadata EF Core.
- Sửa lỗi filtered index SQL Server 10611, sinh lại [SQL script review](docs/sql/InitialCreate.sql), áp dụng và [xác minh `InitialCreate`](docs/database-validation.md) trên SQL Server thật.
- Refactor `MediQueue.Web` từ Razor Pages sang ASP.NET Core MVC; trang chủ, trang lỗi, Layout và static assets đã được chuyển sang Controller/View.
- Chuẩn hóa Controller và Views theo nghiệp vụ; menu/giao diện sẽ hiển thị theo Role sau khi dùng chung một form đăng nhập.

Prompt 03.2 đã triển khai backend Identity/JWT; Prompt 03.3.1 nối Web MVC với API; Prompt 03.4 bổ sung workspace và Dashboard theo Role. Prompt 03.5 triển khai [quản lý người dùng và tài khoản nhân sự](docs/user-management.md) dành riêng cho Admin. Migration `StaffFullName` đã được sinh viên apply; Prompt 03.5.1 đã xác minh read-only và sửa lỗi query runtime của `/Users`. JWT không xuất hiện trong HTML/JavaScript/browser storage. CRUD danh mục, RabbitMQ và SignalR nghiệp vụ vẫn thuộc các prompt sau.

## 3. Công nghệ và ràng buộc

| Hạng mục | Công nghệ |
|---|---|
| Nền tảng | C#, .NET 10 (`net10.0`) |
| Backend | ASP.NET Core Web API dùng Controller |
| Frontend | ASP.NET Core MVC, Bootstrap 5, HTML5/CSS3/JavaScript |
| Dữ liệu | SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS`; duy nhất database `MediQueue` xuyên suốt 9 tuần |
| ORM / tài khoản | EF Core 10, ASP.NET Core Identity |
| Bảo mật | API JWT Access Token; MVC Cookie + token server-side; Role/ownership Authorization |
| Phân tán | RabbitMQ, .NET Worker Service, SignalR |
| Kiểm thử | xUnit, ASP.NET Core integration testing, REST Client |

Không sử dụng Docker. Không thay SQL Server bằng SQLite/MySQL/PostgreSQL/InMemory trong ứng dụng thực tế. Không hard-code secret hoặc connection string bí mật.

## 4. Cấu trúc solution

```text
Tuan2/
├── MediQueue.slnx
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
└── docs/
```

Quan hệ tham chiếu:

- `Domain`, `Contracts`: độc lập.
- `Infrastructure` → `Domain`, `Contracts`.
- `Appointment.Api`, `Queue.Worker` → `Domain`, `Infrastructure`, `Contracts`.
- `Notification.Service`, `Web` → `Contracts`.
- `UnitTests` → `Domain`, `Contracts`, `Web` (kiểm thử presentation thuần); `IntegrationTests` → `Appointment.Api`, `Web`, `Infrastructure`.

Web không tham chiếu Infrastructure/DbContext và giao tiếp backend qua HTTP API.

## 5. Chạy local

Yêu cầu: .NET SDK `10.0.102` hoặc patch tương thích theo `global.json`, SQL Server Express đang chạy nếu cần kết nối database.

```powershell
dotnet restore .\MediQueue.slnx
dotnet build .\MediQueue.slnx --no-restore
dotnet test .\MediQueue.slnx --no-build
dotnet run --project .\src\MediQueue.Appointment.Api --launch-profile https
dotnet run --project .\src\MediQueue.Web --launch-profile https
dotnet run --project .\src\MediQueue.Notification.Service --launch-profile http
dotnet run --project .\src\MediQueue.Queue.Worker
```

Appointment API và Queue Worker cần `ConnectionStrings:MediQueue` từ User Secrets hoặc `ConnectionStrings__MediQueue` từ biến môi trường. API còn yêu cầu `Jwt:SigningKey` ngẫu nhiên tối thiểu 256 bit; xem [hướng dẫn Authentication](docs/authentication.md). Ứng dụng không tự chạy Migration khi khởi động.

| Tiến trình | HTTP | HTTPS | Health |
|---|---:|---:|---|
| Appointment API | 5101 | 7101 | `http://localhost:5101/health` |
| Web | 5102 | 7102 | `http://localhost:5102/health` |
| Notification Service | 5103 | 7103 | `http://localhost:5103/health` |

Web gọi API tại `https://localhost:7101/`; vì cookie luôn `Secure`, luồng đăng nhập MVC phải chạy bằng profile HTTPS tại `https://localhost:7102/`. Development certificate cần hợp lệ và trusted; chỉ kiểm tra bằng `dotnet dev-certs https --check --trust`, không dùng `--clean`. Chi tiết cấu hình SQL Server/User Secrets và kiểm tra môi trường xem [docs/setup.md](docs/setup.md).

## 6. Tài liệu

| Tệp | Nội dung |
|---|---|
| [docs/requirements.md](docs/requirements.md) | Yêu cầu và ma trận truy vết đề tài 09 |
| [docs/actors.md](docs/actors.md) | Actor và phân quyền |
| [docs/use-cases.md](docs/use-cases.md) | Use Case và sơ đồ luồng |
| [docs/business-rules.md](docs/business-rules.md) | Quy tắc nghiệp vụ và state machine |
| [docs/architecture.md](docs/architecture.md) | Kiến trúc logic và cấu trúc solution thực tế |
| [docs/database.md](docs/database.md) | ERD, Data Dictionary và ràng buộc Code First |
| [docs/database-validation.md](docs/database-validation.md) | Bằng chứng áp dụng và kiểm tra schema thực tế |
| [docs/sql/VerifyDatabase.sql](docs/sql/VerifyDatabase.sql) | Script kiểm tra database read-only |
| [docs/api.md](docs/api.md) | Thiết kế API và quy chuẩn Controller |
| [docs/roadmap.md](docs/roadmap.md) | Lộ trình 9 tuần đã điều chỉnh |
| [docs/progress.md](docs/progress.md) | Nhật ký tiến độ thực tế |
| [docs/setup.md](docs/setup.md) | Hướng dẫn restore/build/test/run và cấu hình local |
| [docs/week02-plan.md](docs/week02-plan.md) | Kiến trúc Authentication, phân quyền, database, UI/UX và kế hoạch Prompt Tuần 2 |
| [docs/authentication.md](docs/authentication.md) | Cấu hình, API, thu hồi token và quy trình bootstrap Role/Admin |
| [docs/ui-design-system.md](docs/ui-design-system.md) | Design token, component, font, responsive và accessibility |
| [docs/dashboard.md](docs/dashboard.md) | Kiến trúc, dữ liệu, bảo mật, UI và kiểm thử Dashboard theo Role |
| [docs/user-management.md](docs/user-management.md) | Quản lý người dùng, tài khoản staff, bảo mật và trạng thái migration |
| [AGENTS.md](AGENTS.md) | Quy tắc cho các lần phát triển tiếp theo |

## 7. Trạng thái

- Nhánh: `duan-tuan2-dtc245200016`; commit nguồn kế thừa: `dcee864`.
- Solution, Domain và nền tảng EF Core Code First: đã hoàn thành trong Tuần 1 theo Prompt 02.2.
- Frontend MVC và kiến trúc Controller theo nghiệp vụ: đã chuẩn hóa theo Prompt 02.2.1; Web không tham chiếu DbContext/Infrastructure.
- Prompt 03.4: Dashboard API/MVC, shared layout/sidebar/header và bốn View theo Role đã triển khai; không thay đổi schema hoặc dữ liệu. Kết quả quality gate hiện hành được ghi tại `docs/progress.md`.
- Prompt 03.5/03.5.1: module quản lý người dùng Admin-only đã triển khai; query EF Core, error state MVC và regression test đã được hoàn thiện.
- Database thực tế: có `InitialCreate` và `AddStaffFullNameToApplicationUser`; cả hai đã apply, không có pending migration tại lần xác minh read-only ngày 19/11/2026.
- Không tạo database theo tuần. `MediQueue` là database chính thức; Prompt 03.2 không thay đổi schema và không tạo Migration.
- Chờ người dùng review và commit thủ công; không tự chuyển sang prompt tiếp theo.
