# Yêu cầu hệ thống MediQueue

## 1. Mục đích và phạm vi

Tài liệu xác định baseline nội bộ cho website MediQueue phục vụ bốn Actor: `Patient`, `Doctor`, `Receptionist`, `Admin`. Hệ thống quản lý đặt lịch và hàng đợi khám, trong đó cấp số và phát sự kiện hàng đợi được thiết kế tách bất đồng bộ bằng RabbitMQ. Nguồn chính thức hiện chỉ xác nhận tên đề tài và quy định nộp; xem [topic09-requirements-traceability.md](topic09-requirements-traceability.md) trước khi coi một yêu cầu chi tiết là yêu cầu của giảng viên.

Ký hiệu:

- **[Baseline cốt lõi]**: yêu cầu chi tiết MediQueue đang dùng để lập kế hoạch; chưa xác minh được với đặc tả chính thức của giảng viên.
- **[Mở rộng]**: hoàn thiện sản phẩm nhưng vẫn trong phạm vi đề tài.
- **[Đề xuất]**: quyết định thiết kế cần xác nhận trước khi triển khai.

## 2. Yêu cầu chức năng

### 2.1 Xác thực và hồ sơ

Trạng thái Prompt 03.2: backend đã đáp ứng FR-AUTH-01–04 qua sáu `/api/account` endpoint; phần form/redirect MVC của FR-AUTH-02 và quản trị staff của FR-AUTH-05 thuộc các prompt sau. Receptionist/Admin chưa có profile FullName riêng.

| Mã | Mức | Yêu cầu | Tiêu chí chấp nhận chính |
|---|---|---|---|
| FR-AUTH-01 | Mở rộng | Patient tự đăng ký tài khoản | Tài khoản mới chỉ nhận Role `Patient`; dữ liệu trùng/không hợp lệ bị từ chối |
| FR-AUTH-02 | Mở rộng | Bốn Role đăng nhập bằng một form | Sau xác thực, hệ thống xác định Role và điều hướng đúng khu vực |
| FR-AUTH-03 | Mở rộng | Đăng xuất và kết thúc phiên | Xóa Access Token khỏi bộ nhớ client; phiên bản đầu không có Refresh Token |
| FR-AUTH-04 | Mở rộng | Patient xem/cập nhật hồ sơ cá nhân | Không đọc hoặc sửa hồ sơ của người khác |
| FR-AUTH-05 | Mở rộng | Admin cấp tài khoản Doctor/Receptionist | Không có chức năng tự đăng ký cho hai Role này |

### 2.2 Tra cứu và lịch khám

| Mã | Mức | Yêu cầu | Tiêu chí chấp nhận chính |
|---|---|---|---|
| FR-DOC-01 | Baseline cốt lõi | Tra cứu Doctor/phòng khám | Lọc theo tên/Department; mỗi Doctor thuộc đúng một Department; chỉ trả dữ liệu đang hoạt động |
| FR-SLOT-01 | Baseline cốt lõi | Xem slot còn chỗ của Doctor | Không hiển thị slot quá khứ, bị khóa hoặc đã đủ Capacity |
| FR-APT-01 | Baseline cốt lõi | Đặt Appointment | Kiểm tra slot, sức chứa, trùng giờ và quyền; tạo trạng thái `PendingNumber` đúng một lần |
| FR-APT-02 | Baseline cốt lõi | Hủy Appointment | Chỉ hủy trước `Slot.StartAt` khi chưa phục vụ; giải phóng chỗ đúng một lần, không tái sử dụng số |
| FR-APT-03 | Baseline cốt lõi | Xem chi tiết Appointment | Patient chỉ xem của mình; Role nghiệp vụ xem theo phạm vi được cấp |
| FR-APT-04 | Mở rộng | Xem lịch sử Appointment | Có lọc theo thời gian/trạng thái và phân trang |
| FR-APT-05 | Mở rộng | Receptionist đặt/hủy hộ Patient | Ghi nhận người thực hiện để truy vết; vẫn áp dụng mọi quy tắc đặt/hủy |
| FR-APT-06 | Mở rộng | Receptionist ghi nhận check-in | Check-in là thông tin tiếp nhận; không phải điều kiện cấp QueueTicket |

### 2.3 Cấp số và hàng đợi

| Mã | Mức | Yêu cầu | Tiêu chí chấp nhận chính |
|---|---|---|---|
| FR-QUE-01 | Baseline cốt lõi | Đưa yêu cầu cấp số vào RabbitMQ | HTTP không tự sinh QueueNumber; message có định danh duy nhất |
| FR-QUE-02 | Baseline cốt lõi | Queue Worker sinh QueueNumber | Tăng riêng theo Doctor/ngày từ 1; không trùng/tái sử dụng; hiển thị tối thiểu ba chữ số |
| FR-QUE-03 | Baseline cốt lõi | Worker cập nhật trạng thái hàng đợi | Tạo ticket `Waiting`, chuyển Appointment `PendingNumber → Waiting`, commit trước ack |
| FR-QUE-04 | Baseline cốt lõi | Doctor gọi Patient tiếp theo | Chỉ chọn slot đã đến giờ, theo slot rồi QueueNumber; chuyển trực tiếp `Waiting → Serving` |
| FR-QUE-05 | Baseline cốt lõi | Doctor hoàn thành hoặc xác nhận vắng mặt | `Serving → Completed`; hoặc lượt Waiting kế tiếp đủ điều kiện → `NoShow`; một Doctor tối đa một `Serving` |
| FR-QUE-06 | Baseline cốt lõi | Phát QueueChanged qua RabbitMQ | Phát khi cấp/hủy/gọi/hoàn thành hoặc current serving thay đổi |
| FR-QUE-07 | Baseline cốt lõi | Notification Service phát SignalR | Client đúng Doctor/Appointment nhận sự kiện liên quan |
| FR-QUE-08 | Baseline cốt lõi | Tra cứu snapshot hàng đợi | Chỉ người đăng nhập; trả current serving, số lượt chờ được đếm từ bản ghi và thời điểm cập nhật |
| FR-QUE-09 | Mở rộng | Receptionist theo dõi nhiều hàng đợi | Chỉ đọc; không có quyền gọi, hoàn thành hoặc xác nhận NoShow thay Doctor |

### 2.4 Quản trị

| Mã | Mức | Yêu cầu | Tiêu chí chấp nhận chính |
|---|---|---|---|
| FR-ADM-01 | Mở rộng | Quản lý tài khoản và Role | Chỉ Admin; thay đổi được ghi audit |
| FR-ADM-02 | Mở rộng | Quản lý Doctor/Department/Room | Không xóa cứng dữ liệu đang được tham chiếu; dùng trạng thái hoạt động |
| FR-ADM-03 | Mở rộng | Quản lý lịch làm việc và slot | Ngăn lịch Doctor/Room chồng lấn và Capacity không hợp lệ |
| FR-ADM-04 | Mở rộng | Xem báo cáo tổng hợp | Số lịch theo thời gian, trạng thái, Doctor/Department; không lộ dữ liệu nhạy cảm không cần thiết |

## 3. Yêu cầu phi chức năng

| Mã | Nhóm | Yêu cầu / thước đo dự kiến |
|---|---|---|
| NFR-SEC-01 | Security | API riêng tư dùng JWT; MVC dùng Cookie và gọi API phía server; backend kiểm tra Role + ownership/scope |
| NFR-SEC-02 | Security | Mật khẩu do Identity băm; secret nằm ngoài source control; bắt buộc HTTPS ngoài môi trường Development |
| NFR-SEC-03 | Privacy | Chỉ lưu dữ liệu giả lập/phục vụ học tập; log không chứa mật khẩu, token hoặc thông tin nhạy cảm đầy đủ |
| NFR-SEC-04 | Security | JWT Access Token có thời hạn và lưu phía server MVC; v1 không Refresh Token, không lộ token cho JavaScript/browser storage; cookie/CSRF và SignalR group được bảo vệ |
| NFR-CON-01 | Consistency | Không overbooking khi request đồng thời; dùng transaction và concurrency token/cập nhật nguyên tử |
| NFR-CON-02 | Consistency | Unique constraint bảo vệ QueueNumber và quan hệ một Appointment–một QueueTicket |
| NFR-MSG-01 | Reliability | Consumer xử lý at-least-once và idempotent; ack sau khi transaction hoàn tất |
| NFR-MSG-02 | Reliability | Retry lỗi tạm thời có giới hạn; message lỗi lâu dài chuyển dead-letter queue |
| NFR-RT-01 | Real-time | SignalR client tự reconnect; sau reconnect phải gọi lại snapshot để bù sự kiện đã lỡ |
| NFR-PERF-01 | Performance | **Đề xuất:** p95 API đọc < 500 ms và lệnh ghi < 1 s trong môi trường học tập, không tính thời gian bất đồng bộ |
| NFR-PERF-02 | Performance | **Đề xuất:** thay đổi hàng đợi tới client trong ≤ 3 giây khi broker/mạng bình thường |
| NFR-AVL-01 | Availability | API đặt lịch trả trạng thái rõ ràng nếu broker tạm ngừng; outbox sẽ gửi lại khi broker phục hồi |
| NFR-OBS-01 | Observability | Log có `CorrelationId`, `MessageId`, `AppointmentId`; health check cho SQL Server và RabbitMQ |
| NFR-AUD-01 | Audit | Lưu người tạo/hủy/chuyển trạng thái và thời điểm cho thao tác nghiệp vụ quan trọng |
| NFR-USA-01 | Usability | Giao diện responsive, thông báo lỗi tiếng Việt rõ ràng, không chỉ dựa vào màu sắc |
| NFR-TIME-01 | Time | Lưu timestamp dạng UTC; mọi so sánh ngày/giờ nghiệp vụ dùng `Asia/Ho_Chi_Minh` |
| NFR-COMP-01 | Compatibility | Chạy trực tiếp trên Windows, .NET 10, SQL Server Express và RabbitMQ; không Docker |
| NFR-TEST-01 | Testability | Có unit, integration, authorization, concurrency và message-idempotency tests ở giai đoạn code |

## 4. Ràng buộc

- Chỉ một website ASP.NET Core MVC cho cả bốn Role và một form đăng nhập chung; giao diện tổ chức theo nghiệp vụ, không tách thành bốn hệ thống theo Role.
- Một cơ sở y tế với nhiều Department, Doctor và Room; không đa chi nhánh.
- Database `MediQueue` đã tồn tại; không tự xóa/tạo lại hoặc thay đổi khi chưa được duyệt.
- SQL Server cố định tại `LAPTOP-AQFGUM9Q\SQLEXPRESS`.
- RabbitMQ và Erlang cài trực tiếp trên Windows.
- REST API tối thiểu phải giữ các route: `GET /doctors`, `GET /doctors/{id}/slots`, `POST /appointments`, `DELETE /appointments/{id}`, `GET /appointments/{id}`, `GET /queues/{doctorId}`.
- Không dùng dữ liệu bệnh nhân thật.
- Appointment chỉ dùng sáu trạng thái: `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`.

## 5. Ma trận truy vết baseline cốt lõi

| Nhóm baseline | Requirement | Use Case | API / cơ chế | Thành phần chính |
|---|---|---|---|---|
| Tra cứu Doctor/slot | FR-DOC-01, FR-SLOT-01 | UC-01, UC-02 | `GET /doctors`, `GET /doctors/{id}/slots` | Web, API, SQL |
| Đặt/hủy Appointment | FR-APT-01, FR-APT-02 | UC-03, UC-04 | `POST/DELETE /appointments` | Web, API, SQL |
| Yêu cầu cấp số qua RabbitMQ | FR-QUE-01 | UC-05 | `TicketIssuanceRequested` | API, RabbitMQ |
| Worker sinh số/cập nhật queue | FR-QUE-02, FR-QUE-03 | UC-05 | Consumer + transaction | Queue Worker, SQL |
| Cập nhật lượt phục vụ | FR-QUE-04, FR-QUE-05 | UC-07, UC-08 | Queue command endpoints | API, SQL |
| Phát queue event | FR-QUE-06 | UC-05, UC-07, UC-08 | `QueueChanged` qua RabbitMQ | API/Worker, RabbitMQ |
| SignalR tới client | FR-QUE-07 | UC-06 | SignalR Hub | Notification Service |
| Tra cứu lịch/queue | FR-APT-03, FR-QUE-08 | UC-09, UC-06 | `GET /appointments/{id}`, `GET /queues/{doctorId}` | Web, API, SQL |

## 6. Ngoài phạm vi hiện tại

- Hồ sơ bệnh án điện tử, chẩn đoán, kê đơn, thanh toán và bảo hiểm.
- Telemedicine, SMS/email/push notification và tích hợp hệ thống bệnh viện bên ngoài.
- Mô hình đa cơ sở/chi nhánh và triển khai cloud.
- Docker/Kubernetes và mobile app native.

