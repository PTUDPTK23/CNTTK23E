# TÀI LIỆU PHÂN TÍCH YÊU CẦU VÀ THIẾT KẾ HỆ THỐNG
## HỆ THỐNG GỬI THÔNG BÁO HÀNG LOẠT (MASS NOTIFICATION SYSTEM)
**Môn học:** Phát triển ứng dụng phân tán  
**Sinh viên thực hiện:** Trần Thị Dung - Mã SV: DTC245200209  
**Mục tiêu Tuần 1:** Khởi tạo dự án, chốt yêu cầu, chức năng, vai trò người dùng và luồng gửi thông báo theo kiến trúc phân tán.

---

### 1. Giới thiệu và Bài toán thực tế
Trong các hệ thống quản trị hiện đại (đào tạo, ngân hàng, thương mại điện tử, dịch vụ công), việc gửi thông báo đồng loạt tới hàng ngàn, hàng chục ngàn người dùng qua Email là nhu cầu thiết yếu.
- **Thách thức:** Nếu gửi tuần tự đồng bộ (Synchronous) trực tiếp từ máy chủ Web/API chính, hệ thống sẽ bị treo (blocked), nghẽn kết nối, người dùng phải chờ đợi lâu, dễ dẫn đến timeout hoặc sập máy chủ.
- **Giải pháp kiến trúc phân tán:** Tách biệt hoàn toàn quá trình tiếp nhận yêu cầu (Web/API Producer) với quá trình gửi thông báo thực tế (Message Broker + Background Workers). Việc gửi email được phân phối song song qua nhiều tiến trình Worker độc lập, đảm bảo hiệu năng cao, chịu lỗi tốt và mở rộng linh hoạt (Horizontal Scaling).

---

### 2. Vai trò người dùng (User Roles)

| Vai trò | Quyền hạn và Trách nhiệm |
|---|---|
| **Quản trị viên (Admin)** | • Quản lý tài khoản và phân quyền người dùng trong hệ thống.<br>• Quản trị tham số hệ thống: cấu hình kết nối CSDL MySQL, RabbitMQ AMQP, SMTP Server.<br>• Giám sát trạng thái hàng đợi và hoạt động của các tiến trình Worker.<br>• Xem toàn bộ thống kê, báo cáo và nhật ký lỗi gửi thư. |
| **Nhân viên / Quản lý chiến dịch (Staff)** | • Quản lý danh sách người nhận (thêm mới, cập nhật, xóa, tìm kiếm, nhập danh sách).<br>• Phân loại và gán người nhận vào từng Nhóm người nhận (Recipient Group).<br>• Tạo và khởi chạy các Chiến dịch thông báo (Campaign) gửi email.<br>• Theo dõi tiến độ gửi (Chờ gửi, Đang xử lý, Thành công, Thất bại) của các chiến dịch mình quản lý. |

---

### 3. Các phân hệ chức năng chính (Core Functional Modules)

1. **Phân hệ Quản trị & Xác thực (Authentication & Authorization):**
   - Đăng nhập, đăng xuất, cấp phát JWT token, phân quyền Admin và Staff.
2. **Phân hệ Danh bạ & Nhóm người nhận (Recipients & Groups Management):**
   - CRUD người nhận (Họ tên, Email, SĐT, Trạng thái hoạt động).
   - Quản lý nhóm người nhận (Tên nhóm, Mô tả).
   - Gán/hủy người nhận vào nhóm.
3. **Phân hệ Quản lý Chiến dịch (Campaign Management):**
   - Tạo chiến dịch mới: Tiêu đề email, Nội dung thông báo (hỗ trợ HTML/template), chọn Nhóm người nhận mục tiêu.
   - Backend phân tích nhóm người nhận và sinh ra các bản ghi thông báo con (`notifications`) trong MySQL ở trạng thái `PENDING` (Chờ gửi).
4. **Phân hệ Hàng đợi Tác vụ (Message Broker & Producer):**
   - Backend đóng vai trò Producer, đóng gói từng bản ghi thông báo thành message payload.
   - Đẩy message vào hàng đợi RabbitMQ bền vững (`durable queue: notification_queue`).
   - Xử lý xác nhận publish và tránh trùng lặp thông điệp.
5. **Phân hệ Worker xử lý phân tán (Distributed Background Workers):**
   - Các tiến trình Worker Node.js chạy độc lập trên Windows (ít nhất 2 - 3 Worker song song).
   - Lắng nghe hàng đợi RabbitMQ với cơ chế `prefetch(1)` (chia tải công bằng - fair dispatch).
   - Nhận message -> Chuyển trạng thái bản ghi thành `PROCESSING` (Đang xử lý).
   - Gọi SMTP Nodemailer để chuyển phát email tới hòm thư người nhận.
   - Nếu gửi thành công: gửi `ACK` về RabbitMQ, cập nhật MySQL trạng thái `SENT` kèm thời gian gửi.
   - Nếu lỗi kết nối/tạm thời: tăng số lần thử lại (`retry_count < 3`), gửi `NACK`/re-queue; nếu vượt quá giới hạn thì đánh dấu `FAILED` và ghi chi tiết lỗi vào `send_logs`.
6. **Phân hệ Thống kê, Báo cáo & Giám sát (Dashboard & Monitoring):**
   - Bảng điều khiển trực quan: Tổng số thông báo, Đã gửi, Thất bại, Đang chờ xử lý.
   - Nhật ký gửi chi tiết (Send Logs) hỗ trợ lọc theo chiến dịch, người nhận, trạng thái và mốc thời gian.
   - Theo dõi trạng thái sức khỏe của các thành phần (Backend API, CSDL, Message Queue, Workers).

---

### 4. Luồng xử lý gửi thông báo (Distributed Sequence Flow)

```
[ Người dùng / Giao diện ]
            │ (1) Tạo chiến dịch & Bấm gửi
            ▼
[ Backend Express API (Producer) ]
            │ (2) Ghi Chiến dịch & Danh sách Notifications (PENDING) vào MySQL
            │ (3) Đóng gói và đẩy từng message tác vụ vào Queue
            ▼
[ RabbitMQ Message Broker (Queue: notification_queue) ]
     │                                │
     │ (4a) Phân phối fair-dispatch    │ (4b) Phân phối fair-dispatch
     ▼                                ▼
[ Worker 1 (Node.js) ]           [ Worker 2 / 3 (Node.js) ]
     │ (5) Gửi email qua SMTP          │ (5) Gửi email qua SMTP
     ▼                                ▼
[ SMTP Server (Email Provider) ]  [ SMTP Server (Email Provider) ]
     │ (6) Trả về kết quả              │ (6) Trả về kết quả
     ▼                                ▼
[ Cập nhật MySQL (SENT / FAILED) + ACK cho RabbitMQ ]
```

---

### 5. Tiêu chuẩn công nghệ
- **Môi trường:** Chạy trực tiếp trên Windows (không sử dụng Docker theo đúng yêu cầu đề bài).
- **Backend / Worker:** Node.js, Express.js, amqplib, mysql2, nodemailer.
- **Frontend:** HTML5, CSS3, JavaScript hiện đại, trực quan, nhẹ nhàng.
- **Cơ sở dữ liệu:** MySQL (InnoDB, UTF8mb4).
- **Message Broker:** RabbitMQ + Erlang trên Windows.
- **Quản lý mã nguồn:** Git + GitHub, commit theo từng tuần hoàn thiện.
