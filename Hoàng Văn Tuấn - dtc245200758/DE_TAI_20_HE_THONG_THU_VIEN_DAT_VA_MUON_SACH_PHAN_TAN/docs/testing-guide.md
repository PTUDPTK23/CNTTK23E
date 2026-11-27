# Kiểm thử
**Unit test** (`scripts/test-all.ps1`): máy trạng thái reservation; ReservationService (tạo PENDING + Outbox, trùng yêu cầu → 409, user không tồn tại → 404); ReservationProcessor (giữ bản sao, hết sách → REJECTED, event trùng, reservation không còn PENDING, thiếu reservation → ném lỗi để retry).

**Đồng thời (cần API + Worker chạy):** `node scripts/concurrency-test.mjs 20 3` → tạo sách 3 bản + 20 người dùng, gửi 20 yêu cầu cùng lúc; kỳ vọng đúng 3 RESERVED, 17 REJECTED, không bản sao nào bị cấp hai lần. In `PASS`/`FAIL`.

**Thủ công:**
- Sách "Cấu trúc dữ liệu và giải thuật" (1 bản): hai người lần lượt đặt → người sau REJECTED.
- Sách "Thiết kế ứng dụng xử lý dữ liệu lớn" (0 bản) → REJECTED.
- Dừng Worker, đặt sách → PENDING; bật Worker → tự chuyển RESERVED (chứng minh bất đồng bộ + outbox/queue bền).
- Dừng RabbitMQ, đặt sách → API vẫn trả 202, event ở `message_outbox` PENDING; bật lại → được gửi.
- Kiểm tra SQL: `SELECT book_copy_id, count(*) FROM reservations WHERE status IN ('RESERVED','BORROWED') GROUP BY 1 HAVING count(*)>1;` phải rỗng.

**Chưa có:** test tích hợp Testcontainers, test frontend tự động.
