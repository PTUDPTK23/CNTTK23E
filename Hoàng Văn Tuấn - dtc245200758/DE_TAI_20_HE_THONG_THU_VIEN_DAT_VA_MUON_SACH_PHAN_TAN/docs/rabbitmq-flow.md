# RabbitMQ
- Exchange `library.reservation.exchange` (direct) --`reservation.created`--> queue `reservation.request.queue` (durable).
- Queue chính có `x-dead-letter-exchange=library.reservation.dlx`, routing `reservation.request.dead` → `reservation.request.dlq`.
- Message JSON: `eventId, reservationId, userId, bookId, eventType, createdAt` (không có dữ liệu nhạy cảm). `messageId` = `eventId`, persistent.
- Publisher: Outbox + publisher confirm. Consumer: ACK `auto` (sau khi transaction commit), `concurrency=4`.
- Retry: 3 lần, backoff 1s → 2s (stateless retry). Hết lần: reject không requeue → DLQ. Lỗi nghiệp vụ (hết sách, user bị khóa) là REJECTED bình thường, không retry. JSON hỏng → DLQ ngay.
- Trùng message: `processed_events(event_id)` + kiểm tra reservation còn PENDING.
- Thử DLQ: dừng PostgreSQL ngắn hạn khi gửi yêu cầu, hoặc xóa tạm một reservation trong DB sau khi message nằm trong queue → Worker ném lỗi, sau 3 lần message xuất hiện ở `reservation.request.dlq`.
