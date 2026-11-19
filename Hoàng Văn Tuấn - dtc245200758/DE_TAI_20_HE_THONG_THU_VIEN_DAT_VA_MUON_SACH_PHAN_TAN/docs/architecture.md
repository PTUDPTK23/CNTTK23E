# Kiến trúc

| Thành phần | Vai trò |
|---|---|
| Frontend (React) | Giao diện tiếng Việt, gọi REST, polling trạng thái mỗi 2 giây khi PENDING |
| Library API | REST, quản lý sách/bản sao/người dùng, tạo reservation PENDING + Outbox, mượn/trả/hủy |
| RabbitMQ | Trung gian bất đồng bộ (exchange direct, DLX/DLQ) |
| Reservation Worker | Chỉ Worker phân bổ bản sao khi xử lý message |
| PostgreSQL | Dữ liệu dùng chung; Flyway (do API chạy) quản lý schema |

## Luồng đặt mượn
1. `POST /api/reservations` → API kiểm tra, lưu `reservations(PENDING)` + `reservation_events` + `message_outbox` **trong một transaction**, trả **202**.
2. `OutboxPublisher` (mỗi 1 giây) lấy event chưa gửi (`FOR UPDATE SKIP LOCKED`), publish, chờ publisher-confirm, đánh dấu PUBLISHED.
3. Worker nhận message → một transaction: ghi `processed_events` → khóa reservation → khóa 1 bản sao (`FOR UPDATE SKIP LOCKED`) → RESERVED hoặc REJECTED → ghi lịch sử → commit → ACK.

## Quyết định thiết kế
- **Chống đặt trùng, 3 lớp:** (1) `SKIP LOCKED` khi chọn bản sao; (2) idempotency theo `eventId` + kiểm tra reservation còn PENDING; (3) partial unique index `uq_res_active_copy` ở DB.
- **Outbox** thay vì publish trực tiếp: tránh mất message nếu API chết sau commit. Hệ quả là giao ít nhất một lần (at-least-once) nên Worker phải idempotent.
- **Một người – một yêu cầu đang hoạt động cho mỗi sách** (index `uq_res_active_user_book`).
- **Xóa sách là xóa mềm** (`active=false`), từ chối nếu còn yêu cầu hoạt động; FK `ON DELETE RESTRICT`.
- Worker và API dùng chung DB nhưng Worker chỉ ghi: reservations, book_copies, reservation_events, processed_events.

## Tính năng nâng cấp (v1.1)
- **Tự động hủy giữ sách quá hạn:** `ReservationExpiryService` (mỗi 60 giây) hủy các yêu cầu `RESERVED` quá `app.reservation.hold-hours` (mặc định 48 giờ), trả bản sao về `AVAILABLE`, ghi lịch sử với `actor_type = SYSTEM`. Dùng khóa dòng nên không xung đột với Worker/giao sách.
- **Giám sát đường ống:** `GET /api/system/pipeline` trả số message trong hàng đợi, số consumer (Worker online), số message DLQ, số Outbox chờ/đã phát hành. Trang quản trị hiển thị sơ đồ trực quan và cảnh báo.
- **Thống kê mở rộng:** `GET /api/stats` thêm `overdueLoans`, `reservationsByStatus`; `GET /api/stats/categories` cho biểu đồ theo thể loại.
- **Quản lý người dùng:** `PUT /api/users/{id}/status` (ACTIVE/LOCKED); tài khoản bị khóa không đặt mượn được (API) và bị Worker từ chối.
- **Giao diện:** thiết kế lại toàn bộ (sáng/tối, responsive), trang chủ có tìm kiếm + sơ đồ kiến trúc, stepper tiến trình đặt mượn, trang quản trị có biểu đồ, quản lý sách/bản sao/yêu cầu/người dùng/thể loại, thông báo toast.

## Hạn chế đã biết
- Chưa có xác thực/JWT/phân quyền: backend chưa kiểm tra role (đề bài ghi "nếu triển khai"). Giao diện chỉ ẩn trang Quản trị với vai trò Bạn đọc. Cần Spring Security + JWT nếu muốn dùng thật.
- Message trong DLQ chưa có công cụ xử lý lại (xem RabbitMQ Management để requeue thủ công); giao diện chỉ hiển thị số lượng.
