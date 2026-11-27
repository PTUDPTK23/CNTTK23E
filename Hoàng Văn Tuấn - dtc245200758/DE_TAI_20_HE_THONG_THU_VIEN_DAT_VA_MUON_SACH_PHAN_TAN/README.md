# Đề tài 20 – Hệ thống thư viện và đặt mượn sách phân tán

React + TypeScript (Vite) · Spring Boot (Library API + Reservation Worker) · RabbitMQ · PostgreSQL 16

```
React (5173) --REST--> Library API (8081) --> PostgreSQL
                            |  (Outbox)            ^
                            v                      |
                        RabbitMQ --> Reservation Worker
```

## Bản nâng cấp v1.1 – có gì mới
- Giao diện mới hoàn toàn: sáng/tối, responsive, trang chủ có tìm kiếm + sơ đồ kiến trúc, thẻ sách có thanh số bản, trang chi tiết hiển thị từng bản sao, stepper tiến trình đặt mượn, toast thông báo.
- Trang Quản trị: biểu đồ tổng quan, giám sát đường ống RabbitMQ/Outbox/Worker/DLQ, quản lý sách & bản sao, yêu cầu (giao/nhận trả/hủy), người dùng (khóa/mở khóa), thể loại.
- Backend: tự động hủy giữ sách quá hạn (48 giờ), `/api/system/pipeline`, `/api/stats/categories`, `PUT /api/users/{id}/status`, thống kê mượn quá hạn.
- Chi tiết: `docs/architecture.md` mục "Tính năng nâng cấp".

## ⚠️ Đọc trước khi chạy (quan trọng)
1. **Frontend đã được build (`npm run build`) và kiểm tra giao diện bằng trình duyệt thật với API giả. Phần Java MỚI THÊM ở v1.1 chưa được biên dịch** (môi trường tạo gói không tải được Maven). Hãy chạy `.\scripts\test-all.ps1` / khởi động API và báo lỗi nếu có.
2. **Phiên bản Spring Boot:** gói dùng **3.5.6** (đã quen thuộc, ổn định). Dự án Initializr của bạn là 4.1.2 mà tôi không xác minh được; Boot 4 đổi nhiều thứ (Jackson 3, tách starter, Flyway/test starter riêng). Nếu bạn muốn giữ 4.1.2 thì cần chỉnh pom + import.
3. **Maven Wrapper:** gói không có `mvnw.cmd`. Copy `mvnw`, `mvnw.cmd`, thư mục `.mvn` từ dự án Initializr cũ của bạn vào `backend/library-api/` và `backend/reservation-worker/` (hoặc dùng Maven cài sẵn).
4. **Package Java cũ:** nếu Initializr tạo package khác (vd `com.example...`), xóa thư mục `src` cũ, dùng `src` + `pom.xml` trong gói này.
5. **Không ghi đè `infrastructure/docker-compose.yml` của bạn** – gói không chứa file này. Không chạy `docker compose down -v`.
6. `.env.local` chứa mật khẩu dev bạn đã cung cấp, đã nằm trong `.gitignore` – đừng commit.
7. Chưa có đăng nhập/JWT: demo chọn người dùng ở góc phải; trang Quản trị ai cũng vào được. Đây là hạn chế đã biết (xem docs/architecture.md).

## Chạy bằng VS Code (cách nhanh nhất)
1. `File > Open Workspace from File...` → chọn `thu-vien.code-workspace` (cài các extension được gợi ý: Extension Pack for Java, Spring Boot).
2. `Terminal > Run Task... > Chạy tất cả` (mở 3 terminal: API, Worker, Frontend). Hoặc bấm F5 và chọn `Debug API + Worker` để debug.
3. Mở http://localhost:5173

## Chạy bằng dòng lệnh (Windows)
Yêu cầu: JDK 21, Node 18+, Docker Desktop (PostgreSQL + RabbitMQ của bạn đang chạy).

```powershell
# Terminal 1 – Library API (chạy TRƯỚC: Flyway tạo bảng + dữ liệu mẫu)
.\scripts\run-api.ps1
# Terminal 2 – Reservation Worker
.\scripts\run-worker.ps1
# Terminal 3 – Frontend
cd frontend; npm install; npm run dev
```
Mở http://localhost:5173 · Swagger: http://localhost:8081/swagger-ui.html · Health: http://localhost:8081/actuator/health · RabbitMQ: http://localhost:15672

Nếu PowerShell chặn script: `Set-ExecutionPolicy -Scope Process Bypass`.

## Kiểm tra
```powershell
.\scripts\test-all.ps1                 # unit test API + Worker
node scripts\concurrency-test.mjs 20 3 # 20 người đặt sách có 3 bản => đúng 3 RESERVED
```
Chi tiết: `docs/testing-guide.md`. Kịch bản demo: `docs/demo-script.md`.

## Tài liệu
`docs/architecture.md` · `database-design.md` · `api-documentation.md` · `rabbitmq-flow.md` · `installation-guide.md` · `testing-guide.md` · `demo-script.md`
