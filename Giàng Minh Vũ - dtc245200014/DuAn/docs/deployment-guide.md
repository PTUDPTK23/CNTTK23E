# Hướng dẫn triển khai local

## Compose

1. Cài Docker Desktop với Linux containers, khởi động Docker Engine.
2. Trong thư mục gốc, sao chép `.env.example` thành `.env` và đổi cả mật khẩu.
3. Chạy `docker version`, xác nhận có Client và Server.
4. Chạy `docker compose up -d --build`, sau đó `docker compose ps`.
5. Kiểm tra frontend `http://localhost:8080`, EventService Swagger `http://localhost:5001/swagger`, Dashboard Swagger `http://localhost:5002/swagger`, RabbitMQ UI `http://localhost:15672`.
6. Xem log bằng `docker compose logs -f eventservice dashboardservice auditworker`.

SQL Server lưu ở volume `sql_data`; RabbitMQ lưu `rabbit_data`. Dùng `docker compose down` để dừng nhưng giữ volume.

## Cổng và readiness

| Service | Cổng | Probe |
|---|---:|---|
| Frontend/nginx | 8080 | GET `/` |
| EventService | 5001 | GET `/health/ready` |
| DashboardService | 5002 | GET `/health/ready` |
| SQL Server | 1433 | `sqlcmd SELECT 1` |
| RabbitMQ | 5672, 15672 | `rabbitmq-diagnostics ping` |

Containers gọi nhau bằng service name (`sqlserver`, `rabbitmq`, `eventservice`, `dashboardservice`) trên bridge network. Connection strings và broker URI được truyền qua environment variables.

## Phát triển local

Cài .NET 10 SDK, Node.js 22+ và chạy SQL Server/RabbitMQ riêng. Khởi động EventService, DashboardService, AuditWorker ở ba terminal với connection strings riêng cho `EventManagement`, `EventDashboard`, `EventAudit`; chạy frontend bằng `npm.cmd ci` và `npm.cmd run dev` trong terminal thứ tư. Vite proxy API tới cổng 5001/5002.

Migrations được áp dụng tự động lúc khởi động. Log startup sẽ báo lỗi kết nối/migration để kiểm tra cấu hình hoặc readiness trước khi dùng API.
