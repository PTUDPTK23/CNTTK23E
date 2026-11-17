# Nhật ký tiến độ MediQueue

## 1. Trạng thái tổng quan

| Thuộc tính | Giá trị |
|---|---|
| Nhánh | `duan-dtc245200016` |
| Thư mục nộp | `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1` |
| Tuần | Tuần 1 — Phân tích, solution và EF Core Code First theo Prompt 02.2 |
| Pull Request | Chưa tạo; tiêu đề bắt buộc `DuAn-Tuan1 - dtc245200016 - Đặng Quốc Trung` |
| Hạn nộp | 23h59 ngày 17/11/2026 |
| Trạng thái mã nguồn | Đã có Domain, Identity persistence, DbContext, Fluent API, Migration và test nền tảng; chưa có API nghiệp vụ |
| Trạng thái database | `InitialCreate`/SQL chỉ để review; chưa áp dụng, schema/data không đổi |
| Trạng thái Git | Baseline Prompt 02.1 ở commit `c694cab`; thay đổi Prompt 02.2 chưa commit/stage |

## 2. Nhật ký thực tế

### 2026-11-17 — Phân tích và thiết kế nền tảng

**Đã thực hiện:**

- Xác nhận working directory dự án thuộc `DuAn`, nhánh `duan-dtc245200016` và working tree ban đầu sạch.
- Tổng hợp phạm vi, Actor, yêu cầu chức năng/phi chức năng và ma trận truy vết đề tài 09.
- Đặc tả Use Case, quy tắc nghiệp vụ, state machine và các tình huống cạnh tranh.
- Thiết kế kiến trúc phân tán với Appointment API, Queue Worker, RabbitMQ, Notification Service và SignalR.
- Thiết kế transactional outbox/inbox, idempotency, retry/DLQ và queue version.
- Thiết kế logic database/ERD, transaction boundaries và constraint dự kiến.
- Thiết kế sáu API tối thiểu cùng các API queue/auth/admin cần thiết.
- Lập roadmap 9 tuần và hướng dẫn làm việc cho các lượt Codex tiếp theo.

**Không thực hiện:**

- Không tạo solution/project hoặc mã nguồn ứng dụng.
- Không cài package, không chạy SQL/Migration và không thay đổi database `MediQueue`.
- Không cài/chỉnh RabbitMQ/Erlang.
- Không commit, push hoặc tạo Pull Request.

**Trạng thái review:** Người dùng đã review Prompt 01 và cung cấp quyết định thiết kế chính thức; bộ tài liệu đang chờ review chuẩn hóa lần cuối trước commit đầu tiên.

### 2026-11-17 — Chuẩn hóa theo quyết định thiết kế chính thức

- Rà soát toàn bộ 11 tệp tài liệu và loại bỏ mô hình trạng thái cũ `Called/Booked/Confirmed/InProgress/Skipped`.
- Thống nhất Appointment: `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`.
- Chốt QueueNumber theo Doctor/ngày, bắt đầu 1, định dạng hiển thị tối thiểu ba chữ số và không tái sử dụng.
- Chốt Worker cấp QueueTicket ngay sau đặt lịch; check-in chỉ là metadata tiếp nhận.
- Chốt `call-next` chuyển thẳng `Waiting → Serving`; chỉ Doctor phụ trách được gọi/complete/no-show.
- Chốt hạn hủy trước `Slot.StartAt`, queue yêu cầu đăng nhập, một cơ sở y tế và mỗi Doctor thuộc một chuyên khoa (nay chuẩn hóa tên entity là `Department`).
- Chốt JWT Access Token có thời hạn, không Refresh Token ở v1; SignalR xác thực và kiểm tra group.
- Cập nhật ERD, API, Use Case, Actor, authorization, business rules, state diagram, architecture và roadmap tương ứng.
- Không tạo mã nguồn, không chạy SQL/Migration và không thay đổi database.

### 2026-11-17 — Chuẩn hóa cấu trúc nộp Tuần 1

- Xác nhận đúng nhánh `duan-dtc245200016` và working tree sạch trước khi di chuyển.
- Tạo `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1` theo README repository lớp.
- Di chuyển nguyên vẹn `README.md`, `AGENTS.md` và 9 tài liệu trong `docs` vào `Tuan1`; không phát hiện tệp đích trùng.
- Cập nhật quy định 9 tuần: mỗi tuần một thư mục, một Pull Request và có thể có nhiều commit thực tế.
- Ghi nhận tiêu đề PR Tuần 1 và hạn nộp 23h59 ngày 17/11/2026.
- Không commit, push hoặc tạo Pull Request.

### 2026-11-17 — Rà soát chất lượng hồ sơ Tuần 1

- Xác nhận cấu trúc `Tuan1` đã đúng nên không di chuyển hoặc tạo bản sao lần nữa.
- Rà soát đủ bài toán, lý do xây dựng, mục tiêu, phạm vi, 4 Actor, phân quyền, requirement, Use Case và business rule.
- Xác nhận có Architecture, ERD, Activity, Sequence, State Diagram cùng thiết kế RabbitMQ/SignalR.
- Bổ sung README với bài toán thực tế, mục tiêu, Actor, kết quả Tuần 1 và kế hoạch Tuần 2; không tạo tiến độ hoặc kết quả kiểm thử giả.
- Bổ sung nguyên tắc tuần sau kế thừa tuần trước, bảo toàn tuần đã nộp và tiếp tục dùng đúng PR nếu giảng viên yêu cầu sửa.
- Không tạo mã nguồn, solution, package, SQL/Migration, commit, push hoặc Pull Request.

### 2026-11-17 — Prompt 02.1: khởi tạo solution và nền tảng kiến trúc

- Xác nhận working directory, nhánh `duan-dtc245200016`, remote và working tree ban đầu sạch; máy có .NET SDK `10.0.102` và runtime .NET `10.0.2`.
- Tạo `MediQueue.slnx`, 7 project trong `src`, 2 project xUnit trong `tests`; tất cả target `net10.0` và có ProjectReference đúng hướng, không vòng phụ thuộc.
- Thêm cấu hình `global.json`, `Directory.Build.props`, `.editorconfig`, `.gitignore`; bật Nullable, ImplicitUsings và deterministic build.
- Cài package ổn định cho EF Core SQL Server/Design, Identity EF Core, JWT Bearer và ASP.NET Core integration testing; rà soát không còn package có advisory lỗ hổng theo NuGet source hiện tại.
- Loại bỏ WeatherForecast/code/test mẫu. Thêm enum trạng thái Appointment/Queue, bốn Role, health contract, health endpoint và ba kiểm thử nền tảng có ý nghĩa.
- Chỉ triển khai `HealthController`; các controller/API nghiệp vụ chưa được tạo để tránh giả trạng thái hoàn thành.
- Khảo sát metadata SQL Server read-only: kết nối Windows Authentication thành công; SQL Server `16.0.1000.6` Express; database hiện không có user table và không có `__EFMigrationsHistory`.
- `dotnet restore` thành công; build thành công `0` warning, `0` error; test đạt `3/3` (2 unit, 1 integration).
- Smoke test đạt: API `/health` 200 tại 5101; Web `/health` và `/` 200 tại 5102; Notification `/health` 200 tại 5103; Worker host khởi động đúng thông báo chưa có consumer nghiệp vụ. Các tiến trình smoke test đã dừng.
- `dotnet dev-certs https --check` báo chưa có development certificate hợp lệ; chưa tạo/trust chứng chỉ và chưa smoke test HTTPS. Launch profile/port HTTPS đã được cấu hình để hoàn thiện sau khi người dùng chủ động chuẩn bị certificate.
- Không tạo hoặc áp dụng Migration, không thực thi SQL ghi, không sửa database, không cài/chỉnh RabbitMQ, không commit/push/tạo Pull Request.

### 2026-11-17 — Prompt 02.2: EF Core Code First và Migration review

- Xác nhận nhánh đúng, working tree ban đầu sạch và Prompt 02.1 đã được lưu tại commit `c694cab`.
- Chốt Controller backend theo nghiệp vụ; loại bỏ phương án controller riêng cho Patient/Doctor/Receptionist/Admin. Frontend vẫn chia khu vực theo bốn Role.
- Chốt một thực thể `Department` cho chuyên khoa, không tạo đồng thời `Specialty`; dùng khóa `Guid` nhất quán.
- Tạo Domain entity: Patient, Doctor, Department, Room, DoctorSchedule, TimeSlot, Appointment, DailyQueue, QueueTicket, QueueTransition; thêm invariant capacity, thời gian, state transition, counter và ngày nghiệp vụ Việt Nam.
- Tạo `ApplicationUser : IdentityUser<Guid>`; không tạo bảng tài khoản riêng, không seed user/password/Role.
- Tạo `MediQueueDbContext`, design-time factory, DI cho API/Worker và các `IEntityTypeConfiguration` riêng.
- Tạo các entity persistence OutboxMessage, InboxMessage, IdempotencyRequest, AuditLog; chuẩn bị retry, deduplication, idempotency và audit.
- Ánh xạ PK/FK, required/max length, enum string, 12 check constraint, unique/filtered index, delete behavior và rowversion.
- Bổ sung unit test invariant và test EF metadata; vòng kiểm tra cuối đạt 24/24 test (15 unit, 9 integration/model), build `0` warning và `0` error.
- Sinh Migration `InitialCreate`, Designer, ModelSnapshot và script idempotent `docs/sql/InitialCreate.sql`; `has-pending-model-changes` xác nhận snapshot khớp model.
- Kiểm tra metadata SQL Server sau cùng xác nhận `UserTableCount = 0`, `HasMigrationHistory = 0`.
- Không chạy `database update`, không thực thi script, không dùng EnsureCreated/EnsureDeleted/Migrate và không thay đổi database `MediQueue`.

## 3. Mức độ đáp ứng yêu cầu đề tài

| Hạng mục | Trạng thái hiện tại |
|---|---|
| Doctor/phòng khám và slot còn chỗ | Có Domain/EF model, constraint/index; API chưa triển khai |
| Đặt/hủy Appointment | Có entity/invariant/mapping; transaction/service/API chưa triển khai |
| Cấp số qua RabbitMQ | Có DailyQueue/QueueTicket/Outbox/Inbox schema; publisher/consumer chưa triển khai |
| Worker sinh số/cập nhật queue | Có counter invariant và constraint; Worker nghiệp vụ chưa triển khai |
| Doctor cập nhật lượt phục vụ | Có state machine và filtered index; command API chưa triển khai |
| QueueChanged qua RabbitMQ | Có Outbox persistence; event/publisher chưa triển khai |
| Notification Service + SignalR | Mới ở mức thiết kế; chưa có consumer/Hub |
| Tra cứu Appointment/queue | Có model/index; query API và authorization chưa triển khai |

Chỉ nền tảng Domain/persistence/Migration đã triển khai. Không khẳng định các luồng API, transaction SQL Server thực tế, RabbitMQ hoặc SignalR đã hoạt động.

## 4. Quyết định thiết kế chính thức

1. QueueNumber riêng theo Doctor/ngày, bắt đầu 1, hiển thị `001...`, không tái sử dụng.
2. QueueTicket được Worker cấp qua RabbitMQ ngay sau đặt lịch, không đợi check-in.
3. V1 không có `Called`; gọi lượt chuyển trực tiếp `Waiting → Serving`.
4. Receptionist hỗ trợ lịch/check-in/theo dõi; chỉ Doctor phụ trách gọi, hoàn thành và NoShow.
5. Hủy trước `Slot.StartAt` nếu chưa phục vụ; NoShow là kết thúc và v1 không gọi lại.
6. Queue yêu cầu đăng nhập; không công khai dữ liệu Patient khác.
7. Một cơ sở y tế; mỗi Doctor thuộc đúng một Department (chuyên khoa).
8. Identity + JWT Access Token có thời hạn; v1 không Refresh Token; SignalR kiểm tra quyền group.
9. SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue`; khảo sát read-only trước Migration, không xóa/tạo lại.
10. Giữ transactional outbox/inbox, idempotency, concurrency control; SQL là source of truth và event có `QueueVersion`.
11. Timestamp lưu UTC; mọi quyết định nghiệp vụ dùng `Asia/Ho_Chi_Minh`.

## 5. Tham số triển khai còn phải chốt

- Database đã được khảo sát read-only và hiện không có user table; schema nghiệp vụ/mapping vẫn phải được review trước Migration đầu tiên.
- Kiểu khóa chính/enum, Capacity mặc định, độ dài slot và thuật toán sinh slot.
- Thời hạn JWT Access Token, CORS và chính sách chứng chỉ HTTPS/WSS; port phát triển đã chốt là API `5101/7101`, Web `5102/7102`, Notification `5103/7103`.
- Số lần retry/backoff, ngưỡng DLQ và thời hạn lưu Inbox/Outbox/Idempotency/Audit.
- `CancellationReason` bắt buộc hay tùy chọn; giới hạn khoảng ngày tra cứu slot.

## 6. Bước tiếp theo đề xuất

1. Người dùng review Domain model, Migration `InitialCreate`, ModelSnapshot và `docs/sql/InitialCreate.sql`.
2. Xác nhận riêng mọi thay đổi model trước khi cho phép áp dụng Migration; hiện tuyệt đối chưa chạy lên `MediQueue`.
3. Người dùng tự commit/push vào đúng Pull Request Tuần 1 sau khi review.
4. Chỉ khi được giao Prompt 02.3 mới tiếp tục; không tự động triển khai Login/API/RabbitMQ hoặc thay đổi database.

Không tự động chuyển sang Prompt 02.3.
