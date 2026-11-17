# MediQueue

**Mã đề tài:** 09  
**Tên đề tài:** Hệ thống đặt lịch khám và quản lý hàng đợi  
**Tên tiếng Anh:** Medical Appointment and Queue Management System  
**Sinh viên:** Đặng Quốc Trung — `dtc245200016`

## Quy định nộp dự án

- Dự án được phát triển xuyên suốt **9 tuần** trong thư mục `DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi`.
- Mỗi tuần có một thư mục độc lập: `Tuan1`, `Tuan2`, ..., `Tuan9`.
- Mỗi tuần chỉ tạo **một Pull Request** cho phần việc của tuần đó; trong Pull Request được phép có nhiều commit phản ánh quá trình phát triển thực tế.
- Không gộp sản phẩm của nhiều tuần vào cùng một Pull Request và không tạo commit giả để đủ số lượng.
- Tài liệu hiện tại là sản phẩm Tuần 1 tại `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1`.
- Tiêu đề Pull Request Tuần 1: `DuAn-Tuan1 - dtc245200016 - Đặng Quốc Trung`.
- Hạn nộp Tuần 1: **23h59 ngày 17/11/2026** theo giờ Việt Nam.

Việc commit, push và tạo Pull Request do sinh viên thực hiện sau khi kiểm tra; Codex không tự động thực hiện các thao tác này.

## 1. Tổng quan

MediQueue là hệ thống phân tán hỗ trợ bệnh nhân tra cứu bác sĩ, xem khung giờ còn chỗ, đặt hoặc hủy lịch khám, nhận số thứ tự và theo dõi hàng đợi theo thời gian thực. Bác sĩ điều phối lượt khám; lễ tân hỗ trợ bệnh nhân và lịch hẹn; quản trị viên quản lý dữ liệu nền và tài khoản.

Phiên bản đầu phục vụ **một cơ sở y tế** có nhiều chuyên khoa, Doctor và Room; không có mô hình chi nhánh. Mỗi Doctor thuộc đúng một Specialty, một Specialty có nhiều Doctor. Trạng thái Appointment thống nhất là `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`.

Hai luồng thể hiện bản chất phân tán của đề tài:

- Yêu cầu cấp số được đưa qua RabbitMQ và xử lý bất đồng bộ bởi `MediQueue.Queue.Worker`.
- Mọi thay đổi số thứ tự hoặc lượt đang phục vụ được phát qua RabbitMQ; `MediQueue.Notification.Service` nhận sự kiện và đẩy đến client bằng SignalR.
- QueueNumber tăng riêng theo từng Doctor/ngày, không tái sử dụng và hiển thị tối thiểu ba chữ số (`001`, `002`, `003`, ...).

## 2. Phạm vi giai đoạn hiện tại

Đây là giai đoạn **phân tích và thiết kế nền tảng**. Repository hiện chỉ chứa tài liệu; chưa khởi tạo solution/project .NET, chưa cài package, chưa chạy SQL/Migration và chưa thay đổi database `MediQueue`.

### Yêu cầu bắt buộc của đề tài 09

1. Tra cứu bác sĩ/phòng khám và khung giờ còn chỗ.
2. Đặt, hủy và tra cứu lịch khám.
3. Cấp số bất đồng bộ qua RabbitMQ.
4. Queue Worker sinh số và cập nhật trạng thái hàng đợi.
5. Bác sĩ cập nhật lượt đang phục vụ.
6. Phát sự kiện thay đổi hàng đợi qua RabbitMQ.
7. Notification Service cập nhật client bằng SignalR.
8. Tra cứu trạng thái hàng đợi hiện tại.

### Chức năng mở rộng trong phạm vi sản phẩm

- Tự đăng ký tài khoản Patient và quản lý hồ sơ cá nhân.
- Admin quản lý tài khoản, bác sĩ, chuyên khoa, phòng khám, khung giờ và xem thống kê.
- Receptionist hỗ trợ tiếp nhận, đặt/hủy lịch và theo dõi hàng đợi.
- Lưu lịch sử lịch hẹn và nhật ký chuyển trạng thái để truy vết.
- Receptionist ghi nhận check-in như thông tin tiếp nhận; check-in không phải điều kiện cấp QueueTicket.

Các chức năng mở rộng không thay thế hoặc làm chậm luồng bắt buộc.

## 3. Công nghệ đã thống nhất

| Hạng mục | Công nghệ |
|---|---|
| Ngôn ngữ / nền tảng | C#, .NET 10 |
| Backend | ASP.NET Core Web API |
| Frontend | ASP.NET Core Razor Pages, Bootstrap 5, JavaScript |
| Dữ liệu | SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue` |
| ORM | Entity Framework Core |
| Xác thực / phân quyền | ASP.NET Core Identity, JWT, Role-based Authorization |
| Message broker | RabbitMQ cài trực tiếp trên Windows |
| Xử lý nền | .NET Worker Service |
| Thời gian thực | SignalR |
| Kiểm thử | xUnit, REST Client |
| Quản lý mã nguồn | Git, GitHub |

Không sử dụng Docker, Dockerfile hoặc Docker Compose.

JWT Access Token có thời hạn; phiên bản đầu chưa có Refresh Token. Client không lưu token bền vững trong `localStorage`/`sessionStorage`; SignalR bắt buộc xác thực và chỉ tham gia group sau khi backend kiểm tra quyền.

## 4. Kiến trúc logic

```mermaid
flowchart LR
    Browser[Trình duyệt<br/>4 Role] --> Web[MediQueue.Web<br/>Razor Pages]
    Web --> Api[MediQueue.Appointment.Api]
    Browser <-->|SignalR| Notify[MediQueue.Notification.Service]
    Api --> Db[(SQL Server<br/>MediQueue)]
    Api -->|TicketRequested| Rabbit[(RabbitMQ)]
    Rabbit --> Worker[MediQueue.Queue.Worker]
    Worker --> Db
    Worker -->|QueueChanged| Rabbit
    Api -->|QueueChanged| Rabbit
    Rabbit --> Notify
```

Các project dùng chung mô hình nghiệp vụ và hợp đồng thông qua `MediQueue.Domain`, `MediQueue.Infrastructure` và `MediQueue.Contracts`; chi tiết xem [docs/architecture.md](docs/architecture.md).

## 5. Tài liệu

| Tệp | Nội dung |
|---|---|
| [docs/requirements.md](docs/requirements.md) | Yêu cầu chức năng, phi chức năng và truy vết đề tài |
| [docs/actors.md](docs/actors.md) | Actor, phạm vi dữ liệu và ma trận phân quyền |
| [docs/use-cases.md](docs/use-cases.md) | Danh sách, đặc tả và sơ đồ Use Case |
| [docs/business-rules.md](docs/business-rules.md) | Quy tắc nghiệp vụ và state machine |
| [docs/architecture.md](docs/architecture.md) | Thành phần, luồng phân tán, RabbitMQ và SignalR |
| [docs/database.md](docs/database.md) | Thiết kế dữ liệu dự kiến và ERD |
| [docs/api.md](docs/api.md) | Thiết kế RESTful API dự kiến |
| [docs/roadmap.md](docs/roadmap.md) | Lộ trình phát triển và nộp bài trong 9 tuần |
| [docs/progress.md](docs/progress.md) | Nhật ký tiến độ thực tế |
| [AGENTS.md](AGENTS.md) | Quy tắc cho các lần làm việc tiếp theo với Codex |

## 6. Trạng thái

- Nhánh dự án: `duan-dtc245200016`.
- Tuần nộp hiện tại: `Tuan1` — phân tích và thiết kế nền tảng.
- Pull Request Tuần 1: chưa được Codex tạo; tiêu đề phải là `DuAn-Tuan1 - dtc245200016 - Đặng Quốc Trung`.
- Các quyết định nghiệp vụ cốt lõi đã được chuẩn hóa theo xác nhận ngày 2026-11-17.
- Mã nguồn ứng dụng: chưa triển khai.
- Database có sẵn: chỉ được khảo sát hoặc thay đổi khi có yêu cầu rõ ràng ở giai đoạn sau.

