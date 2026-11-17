# MediQueue

**Mã đề tài:** 09  
**Tên đề tài:** Hệ thống đặt lịch khám và quản lý hàng đợi  
**Tên tiếng Anh:** Medical Appointment and Queue Management System  
**Sinh viên:** Đặng Quốc Trung — `dtc245200016`

## Quy định nộp dự án

- Dự án được phát triển trong 9 tuần tại `DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi`.
- Mỗi tuần có một thư mục `Tuan1` đến `Tuan9` và đúng một Pull Request; một Pull Request có thể gồm nhiều commit thực tế.
- Tuần sau kế thừa kết quả tuần trước, không làm lại dự án từ đầu và không tạo lịch sử giả.
- Pull Request Tuần 1: `DuAn-Tuan1 - dtc245200016 - Đặng Quốc Trung`.
- Hạn nộp Tuần 1: **23h59 ngày 17/11/2026**, giờ Việt Nam.
- Sinh viên tự review, commit, push và tạo Pull Request; Codex không tự thực hiện các thao tác này.

## 1. Tổng quan

MediQueue phục vụ một cơ sở y tế có nhiều chuyên khoa, bác sĩ và phòng khám. Hệ thống hỗ trợ Patient tra cứu bác sĩ/slot, đặt hoặc hủy lịch, nhận số thứ tự và theo dõi hàng đợi; Doctor điều phối hàng đợi của mình; Receptionist hỗ trợ lịch hẹn và tiếp nhận; Admin quản lý dữ liệu nền và tài khoản.

Bốn Role duy nhất là `Patient`, `Doctor`, `Receptionist`, `Admin`. Một website sử dụng một form đăng nhập chung và điều hướng giao diện theo Role. Mỗi Doctor thuộc đúng một Specialty; hệ thống không có mô hình đa chi nhánh.

Hai luồng phân tán cốt lõi:

- Appointment thành công tạo yêu cầu cấp số qua transactional outbox/RabbitMQ; `MediQueue.Queue.Worker` xử lý bất đồng bộ, idempotent.
- Thay đổi queue được phát qua RabbitMQ; `MediQueue.Notification.Service` chuyển tiếp tới client đã xác thực bằng SignalR.

QueueNumber tăng riêng theo Doctor/ngày, bắt đầu từ 1 mỗi ngày, không tái sử dụng và hiển thị `001`, `002`, `003`, ... Trạng thái Appointment: `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`.

## 2. Phạm vi Tuần 1 mở rộng

Tuần 1 đã hoàn thành tài liệu phân tích/thiết kế và, theo Prompt 02.1, khởi tạo nền tảng kỹ thuật ngay trong `Tuan1`:

- Solution .NET 10 gồm 7 project ứng dụng/thư viện và 2 project xUnit.
- ProjectReference một chiều, không có vòng phụ thuộc.
- Enum trạng thái và hằng Role nền tảng; chưa có entity/DbContext nghiệp vụ.
- Health endpoint thật cho API, Web và Notification Service; Worker chỉ xác nhận host khởi động.
- Unit test cho trạng thái/Role và integration test cho health API.
- Khảo sát metadata database `MediQueue` ở chế độ read-only; không chạy SQL ghi, Migration hoặc thay đổi database.

Chưa triển khai đăng ký/đăng nhập, JWT/Identity hoàn chỉnh, nghiệp vụ đặt lịch, 6 REST API bắt buộc, DbContext/entity, RabbitMQ consumer/publisher nghiệp vụ hoặc SignalR Hub nghiệp vụ. Không có endpoint giả báo thành công.

## 3. Công nghệ và ràng buộc

| Hạng mục | Công nghệ |
|---|---|
| Nền tảng | C#, .NET 10 (`net10.0`) |
| Backend | ASP.NET Core Web API dùng Controller |
| Frontend | ASP.NET Core Razor Pages, Bootstrap 5, JavaScript |
| Dữ liệu | SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue` |
| ORM / tài khoản | EF Core 10, ASP.NET Core Identity |
| Bảo mật | JWT Access Token có thời hạn, Role-based Authorization |
| Phân tán | RabbitMQ, .NET Worker Service, SignalR |
| Kiểm thử | xUnit, ASP.NET Core integration testing, REST Client |

Không sử dụng Docker. Không thay SQL Server bằng SQLite/MySQL/PostgreSQL/InMemory trong ứng dụng thực tế. Không hard-code secret hoặc connection string bí mật.

## 4. Cấu trúc solution

```text
Tuan1/
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
- `UnitTests` → `Domain`, `Contracts`; `IntegrationTests` → `Appointment.Api`, `Infrastructure`.

Web không tham chiếu Infrastructure/DbContext và giao tiếp backend qua HTTP API.

## 5. Chạy local

Yêu cầu: .NET SDK `10.0.102` hoặc patch tương thích theo `global.json`, SQL Server Express đang chạy nếu cần kết nối database.

```powershell
dotnet restore .\MediQueue.slnx
dotnet build .\MediQueue.slnx --no-restore
dotnet test .\MediQueue.slnx --no-build
dotnet run --project .\src\MediQueue.Appointment.Api --launch-profile http
dotnet run --project .\src\MediQueue.Web --launch-profile http
dotnet run --project .\src\MediQueue.Notification.Service --launch-profile http
dotnet run --project .\src\MediQueue.Queue.Worker
```

| Tiến trình | HTTP | HTTPS | Health |
|---|---:|---:|---|
| Appointment API | 5101 | 7101 | `http://localhost:5101/health` |
| Web | 5102 | 7102 | `http://localhost:5102/health` |
| Notification Service | 5103 | 7103 | `http://localhost:5103/health` |

Smoke test hiện dùng HTTP. Máy chưa có development HTTPS certificate hợp lệ; các port HTTPS đã cấu hình nhưng cần người dùng chủ động tạo/trust certificate trước khi chạy. Chi tiết cấu hình SQL Server/User Secrets và kiểm tra môi trường xem [docs/setup.md](docs/setup.md).

## 6. Tài liệu

| Tệp | Nội dung |
|---|---|
| [docs/requirements.md](docs/requirements.md) | Yêu cầu và ma trận truy vết đề tài 09 |
| [docs/actors.md](docs/actors.md) | Actor và phân quyền |
| [docs/use-cases.md](docs/use-cases.md) | Use Case và sơ đồ luồng |
| [docs/business-rules.md](docs/business-rules.md) | Quy tắc nghiệp vụ và state machine |
| [docs/architecture.md](docs/architecture.md) | Kiến trúc logic và cấu trúc solution thực tế |
| [docs/database.md](docs/database.md) | ERD dự kiến và kết quả khảo sát schema read-only |
| [docs/api.md](docs/api.md) | Thiết kế API và quy chuẩn Controller |
| [docs/roadmap.md](docs/roadmap.md) | Lộ trình 9 tuần đã điều chỉnh |
| [docs/progress.md](docs/progress.md) | Nhật ký tiến độ thực tế |
| [docs/setup.md](docs/setup.md) | Hướng dẫn restore/build/test/run và cấu hình local |
| [AGENTS.md](AGENTS.md) | Quy tắc cho các lần phát triển tiếp theo |

## 7. Trạng thái

- Nhánh: `duan-dtc245200016`.
- Solution nền tảng: đã khởi tạo trong Tuần 1 theo Prompt 02.1.
- Restore/build/test/smoke test: đã chạy thực tế ngày 17/11/2026; xem `docs/progress.md`.
- Database: kết nối Windows Authentication thành công, hiện chưa có bảng người dùng; không thay đổi schema/data.
- Pull Request Tuần 1: chưa tạo.
- Bước tiếp theo chỉ thực hiện sau khi người dùng review và giao Prompt 02.2.
