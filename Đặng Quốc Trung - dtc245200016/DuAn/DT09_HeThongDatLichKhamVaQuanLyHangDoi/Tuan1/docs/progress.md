# Nhật ký tiến độ MediQueue

## 1. Trạng thái tổng quan

| Thuộc tính | Giá trị |
|---|---|
| Nhánh | `duan-dtc245200016` |
| Thư mục nộp | `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1` |
| Tuần | Tuần 1 — Phân tích và thiết kế nền tảng |
| Pull Request | Chưa tạo; tiêu đề bắt buộc `DuAn-Tuan1 - dtc245200016 - Đặng Quốc Trung` |
| Hạn nộp | 23h59 ngày 17/11/2026 |
| Trạng thái mã nguồn | Chưa khởi tạo solution/project |
| Trạng thái database | Không thay đổi |
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

- Schema thực tế của database chỉ được khảo sát read-only ở giai đoạn database.
- Kiểu khóa chính/enum, Capacity mặc định, độ dài slot và thuật toán sinh slot.
- Thời hạn JWT Access Token, port/CORS/chứng chỉ HTTPS/WSS cho các dịch vụ Windows.
- Số lần retry/backoff, ngưỡng DLQ và thời hạn lưu Inbox/Outbox/Idempotency/Audit.
- `CancellationReason` bắt buộc hay tùy chọn; giới hạn khoảng ngày tra cứu slot.

## 6. Bước tiếp theo đề xuất

1. Người dùng review tính nhất quán của bộ tài liệu đã chuẩn hóa.
2. Người dùng tự commit/push và tạo đúng một Pull Request Tuần 1 với tiêu đề bắt buộc.
3. Khi bắt đầu Tuần 2, tạo/làm việc trong `Tuan2`, kiểm tra lại nhánh/status rồi mới khởi tạo solution .NET 10 và các project đã chốt.
4. Trước Migration đầu tiên, khảo sát read-only schema database `MediQueue` và lập kế hoạch thay đổi để duyệt.

Không tự động chuyển sang giai đoạn lập trình.
