# Hệ thống xử lý đơn hàng phân tán

Đề tài triển khai:
- Order API: tạo đơn, lưu PostgreSQL, gửi tin nhắn RabbitMQ.
- Order Worker: đọc hàng đợi, cập nhật Pending -> Processing -> Completed/Failed.
- Notification Service: nhận sự kiện và đẩy thông báo tới client qua SignalR.
- PostgreSQL + RabbitMQ chạy bằng Docker Compose.

## Chạy hệ thống

Yêu cầu: Docker Desktop và .NET 8 SDK.

```powershell
docker compose up --build
```

Sau khi khởi động:
- Swagger Order API: http://localhost:8081/swagger
- Client demo: http://localhost:8082
- RabbitMQ Management: http://localhost:15672 (guest / guest)

## API

```http
POST /orders
Content-Type: application/json

{
  "customerName": "Nguyen Van A",
  "products": [
    {"name": "Laptop", "quantity": 1}
  ]
}
```

```http
GET /orders
GET /orders/{id}
```

Để kiểm tra nhánh thất bại, tạo đơn có một sản phẩm với `quantity` lớn hơn 100.
