# Nhật ký tiến độ MediQueue

## 1. Trạng thái tổng quan

| Thuộc tính | Giá trị |
|---|---|
| Nhánh | `duan-dtc245200016` |
| Thư mục nộp | `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1` |
| Tuần | Tuần 1 — Phân tích, thiết kế và khởi tạo nền tảng theo Prompt 02.1 |
| Pull Request | Chưa tạo; tiêu đề bắt buộc `DuAn-Tuan1 - dtc245200016 - Đặng Quốc Trung` |
| Hạn nộp | 23h59 ngày 17/11/2026 |
| Trạng thái mã nguồn | Đã khởi tạo solution .NET 10 gồm 9 project; chưa triển khai nghiệp vụ |
| Trạng thái database | Đã khảo sát metadata read-only; không thay đổi schema/data |
| Trạng thái Git | Chưa commit bởi Codex; chờ người dùng review |

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
- Chốt hạn hủy trước `Slot.StartAt`, queue yêu cầu đăng nhập, một cơ sở y tế và mỗi Doctor thuộc một Specialty.
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

## 3. Mức độ đáp ứng yêu cầu đề tài

| Hạng mục | Trạng thái thiết kế |
|---|---|
| Doctor/phòng khám và slot còn chỗ | Đã mô tả requirement, UC, API, bảng dữ liệu |
| Đặt/hủy Appointment | Đã mô tả transaction, idempotency, concurrency và API |
| Cấp số qua RabbitMQ | Đã mô tả command, exchange/queue và Worker flow |
| Worker sinh số/cập nhật queue | Đã mô tả thuật toán counter, constraint, inbox/outbox |
| Doctor cập nhật lượt phục vụ | Đã mô tả command API, state machine và authorization |
| QueueChanged qua RabbitMQ | Đã mô tả event versioned và producer flow |
| Notification Service + SignalR | Đã mô tả consumer, group, security, reconnect/snapshot |
| Tra cứu Appointment/queue | Đã mô tả API, payload và ownership/scope |

Tất cả hiện ở mức **thiết kế**, chưa khẳng định đã triển khai.

## 4. Quyết định thiết kế chính thức

1. QueueNumber riêng theo Doctor/ngày, bắt đầu 1, hiển thị `001...`, không tái sử dụng.
2. QueueTicket được Worker cấp qua RabbitMQ ngay sau đặt lịch, không đợi check-in.
3. V1 không có `Called`; gọi lượt chuyển trực tiếp `Waiting → Serving`.
4. Receptionist hỗ trợ lịch/check-in/theo dõi; chỉ Doctor phụ trách gọi, hoàn thành và NoShow.
5. Hủy trước `Slot.StartAt` nếu chưa phục vụ; NoShow là kết thúc và v1 không gọi lại.
6. Queue yêu cầu đăng nhập; không công khai dữ liệu Patient khác.
7. Một cơ sở y tế; mỗi Doctor thuộc đúng một Specialty.
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

1. Người dùng review source, tài liệu, kết quả kiểm thử và diff của Prompt 02.1.
2. Người dùng tự commit/push và tạo đúng một Pull Request Tuần 1 với tiêu đề bắt buộc.
3. Chỉ khi được giao Prompt 02.2, kế thừa solution hiện có để xây dựng Domain/entity/DbContext mapping và test invariant; không tạo solution mới hoặc làm lại từ đầu.
4. Lập Migration đầu tiên để review riêng sau khi mô hình được duyệt; không áp dụng lên `MediQueue` nếu chưa có cho phép rõ ràng.

Không tự động chuyển sang Prompt 02.2.
