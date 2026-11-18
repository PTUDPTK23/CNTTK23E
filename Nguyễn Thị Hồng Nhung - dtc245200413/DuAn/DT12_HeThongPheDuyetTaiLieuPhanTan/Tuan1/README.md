# Đề tài 12 - Hệ thống Phê duyệt Tài liệu Phân tán

## 1. Kiến trúc hệ thống
Hệ thống được xây dựng theo kiến trúc hướng sự kiện (Event-Driven Architecture) trên nền tảng **.NET 10**, sử dụng **ASP.NET Core Minimal API**, **PostgreSQL**, **RabbitMQ** và **SignalR**:

```
ClientWeb (:5000)
   │  (REST / HTTP qua Docker network)
   ▼
DocumentApi (:5001) ──► PostgreSQL (document_db + OutboxMessages)
   │  (OutboxPublisherWorker - Asynchronous Messaging)
   ▼
RabbitMQ (:5672 / UI :15672) [Exchange: document.events -> Queue: notification.queue + DLQ]
   │  (RabbitMqConsumerWorker - Idempotent Consumer)
   ▼
NotificationService (:5002) ──► PostgreSQL (notification_db)
   │  (SignalR Real-time Push: /hubs/notifications)
   ▼
ClientWeb (:5000)
```

## 2. Cấu trúc thư mục
```
DuAn/
├── docker-compose.yml
├── .env.example
├── README.md
├── docs/
│   └── BaoCao_DeTai12.docx
└── src/
    ├── DocumentApi/            # Minimal API quản lý tài liệu, lịch sử duyệt & Outbox
    ├── NotificationService/    # Consumer RabbitMQ, lưu thông báo & SignalR Hub
    └── ClientWeb/              # Giao diện Web + SignalR Client
```

## 3. Hướng dẫn cài đặt và chạy hệ thống
### Yêu cầu môi trường
- Docker Desktop (hỗ trợ `docker compose`)

### Khởi chạy toàn bộ hệ thống
```bash
docker compose up --build -d
```

### Truy cập các thành phần
- **Giao diện Web Client:** http://localhost:5000
- **Document API:** http://localhost:5001/health
- **Notification Service:** http://localhost:5002/health
- **RabbitMQ Management UI:** http://localhost:15672 (tài khoản/mật khẩu: `guest` / `guest`)

## 4. Danh sách API chính (DocumentApi)
- `POST /documents` : Tạo tài liệu mới (`Draft`)
- `GET /documents` : Tra cứu tài liệu (hỗ trợ `?status=`, `?approver=`, `?createdBy=`, `?search=`)
- `GET /documents/{id}` : Xem chi tiết tài liệu và lịch sử xử lý
- `POST /documents/{id}/submit` : Gửi tài liệu phê duyệt (`Draft -> Submitted`)
- `POST /documents/{id}/approve` : Phê duyệt tài liệu (`Submitted -> Approved`)
- `POST /documents/{id}/reject` : Từ chối tài liệu (`Submitted -> Rejected`)

## 5. Kịch bản kiểm thử khả năng chịu lỗi & phục hồi (Demo khi bảo vệ)
1. **Tạm dừng Notification Service:**
   - Chạy: `docker compose stop notification-service`
   - Thực hiện gửi/duyệt tài liệu trên Web UI (`http://localhost:5000`). Document API vẫn hoạt động bình thường, sự kiện nằm an toàn trong hàng đợi `notification.queue` của RabbitMQ.
   - Chạy: `docker compose start notification-service` -> Service tự động kết nối lại, tiêu thụ toàn bộ tin nhắn tồn đọng và đẩy thông báo bù.
2. **Tạm dừng RabbitMQ (Transactional Outbox Pattern):**
   - Chạy: `docker compose stop rabbitmq`
   - Thực hiện gửi/duyệt tài liệu trên Web UI. Nhờ mô hình **Transactional Outbox**, Document API không bị lỗi mà lưu trạng thái cùng sự kiện vào bảng `OutboxMessages` (hiển thị số lượng chờ gửi trên thanh trạng thái).
   - Chạy: `docker compose start rabbitmq` -> `OutboxPublisherWorker` tự động đẩy toàn bộ sự kiện chờ sang RabbitMQ.
