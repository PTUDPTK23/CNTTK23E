# Đề tài 07 – Hệ thống hỗ trợ yêu cầu khách hàng

Ứng dụng mẫu gồm giao diện web tiếng Việt, REST API ASP.NET Core, PostgreSQL, RabbitMQ, Assignment Worker và Notification Service dùng SignalR.

## Chạy bằng Docker Compose

1. Tạo tệp cấu hình local: `Copy-Item .env.example .env`.
2. Trước khi chạy, đổi các mật khẩu demo và `AUTH_SIGNING_KEY` trong `.env`.
3. Chạy tại thư mục chứa `docker-compose.yml`:

```powershell
docker compose up --build -d
docker compose ps
docker compose logs -f support-api assignment-worker notification-service
```

Mở giao diện tại http://localhost:3000. API được proxy dưới `/api`, Swagger ở http://localhost:3000/swagger/index.html, health ở http://localhost:3000/api/health, SignalR được proxy tại http://localhost:3000/notifications và RabbitMQ Management ở http://localhost:15673. Nếu cổng đang bận, đổi `FRONTEND_PORT` hoặc `RABBITMQ_MANAGEMENT_PORT` trong `.env`.

Tắt các dịch vụ bằng `docker compose down`. Lệnh `docker compose down -v` xóa cả dữ liệu PostgreSQL demo.

## Tài khoản demo

Tài khoản được cấu hình qua `.env` và phải đổi mật khẩu trước khi dùng ngoài máy cá nhân. Các mật khẩu `customer-demo-change`, `agent-demo-change`, `admin-demo-change` là fallback khi không có biến môi trường tương ứng; nếu đã sao chép `.env.example`, hãy dùng các mật khẩu trong tệp đó hoặc thay bằng mật khẩu riêng.

| Vai trò | Tên mặc định | Mật khẩu mặc định |
|---|---|---|
| Người gửi yêu cầu | `customer` | `customer-demo-change` |
| Nhân viên hỗ trợ | `agent` | `agent-demo-change` |
| Quản trị viên | `admin` | `admin-demo-change` |

Người gửi chỉ xem yêu cầu của tài khoản mình và tạo yêu cầu mới. Nhân viên và quản trị viên được xem danh sách, tạo yêu cầu và cập nhật trạng thái. API kiểm tra trạng thái chuyển hợp lệ. Đăng nhập qua `POST /auth/login`; token Bearer có thời hạn 8 giờ.

## API chính

- `POST /auth/login` — xác thực thông tin tài khoản.
- `POST /tickets` — tạo yêu cầu; người gửi phải dùng mã tài khoản của mình.
- `GET /tickets?search=...&status=...`, `GET /tickets/{id}` — tìm/lọc và xem chi tiết theo quyền.
- `PATCH /tickets/{id}/status` — nhân viên/quản trị viên cập nhật trạng thái.
- `GET /tickets/{id}/history` — xem lịch sử theo quyền.
- `GET /users/{userId}/tickets` — xem yêu cầu của người dùng theo quyền.
- `GET /health` — kiểm tra API.

Các trạng thái là `New`, `Assigned`, `InProgress`, `Resolved`; ưu tiên là `Normal` hoặc `High`.

## Luồng xử lý

API ghi yêu cầu, lịch sử và sự kiện `ticket.created` vào PostgreSQL trong cùng một lần lưu. Khi trạng thái đổi, API cũng ghi `ticket.status.changed` vào outbox cùng với bản ghi lịch sử. Outbox Publisher chuyển các sự kiện đang chờ tới RabbitMQ và thử lại khi broker tạm thời không sẵn sàng. Assignment Worker phân công yêu cầu ưu tiên cao vào `PrioritySupport`, yêu cầu thường vào `GeneralSupport`, rồi phát `ticket.assigned`. Notification Service nhận cả hai sự kiện và gửi SignalR `TicketUpdated` theo nhóm khách hàng/nhân viên liên quan; giao diện cập nhật danh sách khi nhận thông báo, đồng thời tải lại định kỳ để dự phòng.

## Kiểm tra nhanh

```powershell
docker compose ps
Invoke-RestMethod http://localhost:3000/api/health
```

Đăng nhập trên giao diện bằng tài khoản demo, tạo yêu cầu với người dùng `customer`, rồi đăng nhập `agent` để xem và cập nhật yêu cầu. Chọn mức ưu tiên `High` để quan sát nhóm phân công ưu tiên trong dữ liệu/API. Swagger có thể dùng để kiểm tra các endpoint.

## Chuẩn bị trình diễn khi không có Internet

Khi còn mạng, chạy PowerShell tại thư mục repository:

```powershell
.\scripts\prepare-offline.ps1
```

Lệnh này tải image PostgreSQL/RabbitMQ và build image của các ứng dụng. Sau khi chuẩn bị xong, có thể ngắt mạng và khởi động bằng:

```powershell
.\scripts\start-offline.ps1
```

Giao diện không tải thư viện hoặc font từ Internet. Để chạy console client offline, cần có .NET 10 SDK trên máy; phần web/API chạy trong các image đã chuẩn bị.

## Báo cáo và sơ đồ

Báo cáo đồ án, checklist đối chiếu và sơ đồ kiến trúc/luồng xử lý nằm trong `docs/REPORT.md` và `docs/`. Điền thông tin sinh viên, chụp màn hình hệ thống khi chạy thực tế, rồi bổ sung vào `docs/screenshots/` trước khi nộp.

## Cấu trúc

```text
backend/src/Support.Api/                 REST API, Swagger, SignalR Hub
backend/src/Support.AssignmentWorker/    Tiêu thụ yêu cầu mới và phân công
backend/src/Support.NotificationService/ Phát sự kiện SignalR
frontend/web/                            Giao diện HTML/CSS/JavaScript
frontend/Support.Client/                 Console client đăng nhập qua API
docker-compose.yml
```

Chạy console client bằng `dotnet run --project frontend/Support.Client`. Mặc định client dùng API qua `http://localhost:3000/api`; đặt `SUPPORT_API_URL` để đổi địa chỉ.

Đây là bản thực hành. Khi triển khai thực tế cần dùng kho tài khoản và mật khẩu băm, JWT/OIDC chuẩn, migrations, DLQ/chính sách số lần thử, dọn outbox đã gửi, giám sát, TLS và quản lý secrets phù hợp. Outbox và worker có thể phát lại sự kiện, vì vậy phía nhận cần giữ tính idempotent.
