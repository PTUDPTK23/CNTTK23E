# Cài đặt
1. Bảo đảm PostgreSQL (localhost:5433) và RabbitMQ (5672/15672) đang chạy: `docker ps`. Không đụng container dự án nhà hàng.
2. Kiểm tra cổng trống: 8081, 5173 (`netstat -ano | findstr :8081`). Không dùng 8080.
3. Kiểm tra `.env.local` ở thư mục gốc (đã có giá trị dev). Mẫu: `.env.example`.
4. Copy `mvnw.cmd` + `.mvn` vào hai module backend nếu muốn dùng Maven Wrapper.
5. Chạy API → Worker → Frontend như README. Lần đầu Flyway tạo bảng và dữ liệu mẫu.
6. Chạy bằng Docker (tùy chọn): xem `infrastructure/docker-compose.apps.yml`; trong Docker dùng hostname `postgres:5432`, `rabbitmq:5672`.

Sự cố thường gặp: `password authentication failed` → sai `DB_PASSWORD`; `Connection refused 5672` → RabbitMQ chưa chạy; `PRECONDITION_FAILED` khi khai báo queue → queue cũ khác tham số, xóa queue đó trong RabbitMQ UI (không xóa volume); CORS lỗi → frontend phải chạy ở 5173.
