# Đề tài 12: Hệ thống phê duyệt tài liệu phân tán

- **Sinh viên thực hiện:** Lường Thị Giang
- **Mã sinh viên:** `dtc245200988`
- **Lớp:** CNTTK23E
- **Học phần:** Phát triển ứng dụng phân tán

---

## 1. Kiến trúc hệ thống

Hệ thống được thiết kế theo kiến trúc hướng sự kiện (Event-Driven Architecture) gồm các service chạy độc lập trong Docker containers và giao tiếp qua mạng nội bộ `distributed-doc-net`:

```text
+-------------+   REST API    +--------------+   EF Core   +------------+
| Web Client  | ------------> | Document API | ----------> | PostgreSQL |
| (.NET 10)   |               | (.NET 10)    |             |   (DB)     |
+-------------+               +--------------+             +------------+
       ^                             |
       | SignalR                     | RabbitMQ (Topic Exchange: document.events.exchange)
       | Real-time                   v
+----------------------+      +--------------+
| Notification Service | <--- |   RabbitMQ   |  ---> Dead-Letter Queue (document.events.dlq)
|      (.NET 10)       |      |   (Broker)   |
+----------------------+      +--------------+
```

### Các thành phần chính:
1. **Document API (`src/DocumentApi` - Port 5001):** ASP.NET Core Minimal API quản lý vòng đời tài liệu (`Draft` -> `Submitted` -> `Approved` / `Rejected`), lưu trữ dữ liệu & lịch sử vào PostgreSQL và phát sự kiện thay đổi trạng thái lên RabbitMQ.
2. **Notification Service (`src/NotificationService` - Port 5002):** Worker tiêu thụ sự kiện bất đồng bộ từ RabbitMQ (hỗ trợ Retry, Dead-Letter Queue, Idempotent Consumer) và đẩy thông báo tức thời tới Client qua SignalR Hub.
3. **Web Client (`src/WebClient` - Port 3000):** Giao diện web trực quan cho phép tạo tài liệu, gửi duyệt, phê duyệt, từ chối, tra cứu lịch sử và nhận thông báo real-time.
4. **PostgreSQL (`postgres:16-alpine` - Port 5432):** Lưu trữ bảng `documents` và `approval_histories`.
5. **RabbitMQ (`rabbitmq:3.13-management-alpine` - Port 5672 / Management UI 15672):** Message Broker trung gian đảm bảo giao tiếp bất đồng bộ và không mất gói tin.

---

## 2. Hướng dẫn cài đặt và chạy hệ thống

### Yêu cầu môi trường
- Docker & Docker Compose (hoặc .NET 10 SDK nếu chạy local).

### Khởi chạy toàn bộ hệ thống bằng Docker Compose
Tại thư mục `DuAn/`, mở Terminal và chạy lệnh:

```bash
docker compose up --build -d
```

Kiểm tra trạng thái các container:
```bash
docker compose ps
```

### Truy cập các dịch vụ
- **Giao diện Web Client:** http://localhost:3000
- **Document API Health Check:** http://localhost:5001/health
- **Notification Service Health Check:** http://localhost:5002/health
- **RabbitMQ Management UI:** http://localhost:15672 (Tài khoản: `guest` / Mật khẩu: `guest`)

---

## 3. Danh sách API tối thiểu & mở rộng

| Phương thức | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `POST` | `/documents` | Tạo tài liệu mới ở trạng thái `Draft` |
| `GET` | `/documents?status=&creator=&approver=` | Tra cứu danh sách tài liệu theo trạng thái / người xử lý |
| `GET` | `/documents/{id}` | Xem chi tiết tài liệu và lịch sử các bước xử lý |
| `POST` | `/documents/{id}/submit` | Gửi tài liệu vào quy trình phê duyệt (`Draft`/`Rejected` -> `Submitted`) |
| `POST` | `/documents/{id}/approve` | Phê duyệt tài liệu (`Submitted` -> `Approved`) |
| `POST` | `/documents/{id}/reject` | Từ chối tài liệu kèm lý do (`Submitted` -> `Rejected`) |
| `GET` | `/health` | Kiểm tra tình trạng hoạt động của service và kết nối CSDL |

---

## 4. Kịch bản kiểm thử khả năng chịu lỗi (Fault Tolerance)

1. **Tạm dừng `notification-service`:**
   ```bash
   docker stop doc-approval-notification
   ```
   - Thực hiện gửi duyệt / phê duyệt tài liệu trên Web Client.
   - `Document API` vẫn hoạt động bình thường, lưu DB và đẩy message vào queue `document.events.queue` (durable) trên RabbitMQ.
   - Khởi động lại service:
   ```bash
   docker start doc-approval-notification
   ```
   - `Notification Service` tự động kết nối lại RabbitMQ, tiêu thụ toàn bộ các sự kiện tồn đọng và gửi thông báo tới Client mà không mất dữ liệu.

2. **Tạm dừng `rabbitmq`:**
   ```bash
   docker stop doc-approval-rabbitmq
   ```
   - `Document API` thực hiện retry gửi message và vẫn đảm bảo lưu trạng thái tài liệu vào PostgreSQL mà không làm sập API. Khi chạy lại `docker start doc-approval-rabbitmq`, cơ chế `AutomaticRecoveryEnabled` tự động khôi phục kênh kết nối.
