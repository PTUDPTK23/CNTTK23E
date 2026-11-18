# Bài nộp dự án Tuần 1 — Lê Hải Anh — dtc245200851

Đề tài 09: Hệ thống đặt lịch khám và quản lý hàng đợi. Phạm vi bản nộp: **M0 — khởi tạo hệ thống**. Xem [báo cáo Tuần 1](BAO_CAO_TUAN1.md).

# ClinicQueue

Hệ thống đặt lịch khám và quản lý hàng đợi. **M0: chỉ khởi tạo nền tảng**, chưa có đăng nhập, đặt lịch, consumer RabbitMQ hoặc SignalR nghiệp vụ.

## Yêu cầu

- .NET SDK 10.0.403 hoặc SDK 10.0 feature band mới hơn; EF Core **10.0.14**.
- Node.js 24 LTS (hoặc phiên bản được Vite hỗ trợ), npm; trên Windows dùng `npm.cmd` nếu execution policy chặn npm.ps1.
- Docker Desktop chạy Linux containers và Docker Compose >= 2.30 (post_start hook). SQL Server cần tài nguyên RAM phù hợp (tối thiểu 2 GB cho SQL Server).

## Chạy bằng Docker

Tại thư mục gốc:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
# Điền SQL_PASSWORD và RABBITMQ_PASSWORD trong .env.
# SQL_PASSWORD: ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.
docker compose config --quiet
docker compose up -d --build --wait --wait-timeout 300
docker compose ps -a
```

Không commit `.env`. Bản nộp không kèm `.env`; hãy tạo từ `.env.example` và đặt mật khẩu riêng. Lệnh `docker compose config` không có `--quiet` sẽ in giá trị secrets đã nội suy.

| Thành phần | Địa chỉ mặc định |
| --- | --- |
| React SPA | http://localhost:8088 |
| API health / liveness | http://localhost:5000/health /health/live |
| API thông tin hệ thống | http://localhost:5000/api/system |
| Notification health | http://localhost:5001/health |
| SQL Server | localhost,14333; database ClinicQueue |
| RabbitMQ AMQP | localhost:5673 |
| RabbitMQ Management | http://localhost:15673; tài khoản trong .env |

Các cổng chỉ bind loopback, có thể đổi qua `.env.example`. Project Compose luôn là `clinicqueue`; network và volumes có tiền tố riêng. Không thay đổi hoặc dừng `crm_*`. Không chạy prune, reset volume hoặc `down -v`.

Compose chạy tuần tự `api-migrate` rồi `worker-migrate` trước các service. Hai container migration kết thúc **Exited (0)** là bình thường. API và Worker dùng chung database ClinicQueue, DbContext và migration history riêng: `api.__EFMigrationsHistory`, `worker.__EFMigrationsHistory`. Migrations M0 rỗng chỉ thiết lập lịch sử; chưa tạo bảng nghiệp vụ. Không dùng EnsureCreated hoặc tự xóa database. SA chỉ phục vụ sandbox M0, cần tài khoản ít quyền theo service ở giai đoạn triển khai thực tế.

RabbitMQ dùng post_start hook để đợi broker, nạp topology và tạo user từ biến môi trường nếu còn thiếu. Hook giữ nguyên mật khẩu user hiện hữu; health chỉ đạt khi hook xong. RabbitMQ nạp exchange topic `clinic.events`, DLX `clinic.events.dlx`, DLQ và các durable queue từ `docker/rabbitmq-definitions.json`. TicketAssigned có queue riêng cho API và Notification để cả hai đều nhận event. M0 chưa có producer/consumer nên không cấu hình secrets RabbitMQ vào code service.

## Chạy backend và frontend trên máy

Khởi động hạ tầng bằng `docker compose up -d sqlserver rabbitmq`. Tự đặt connection string bằng environment variable trong từng terminal (không lưu mật khẩu vào source):

```powershell
$env:ConnectionStrings__ClinicQueue = "Server=localhost,14333;Database=ClinicQueue;User Id=sa;Password=<SQL_PASSWORD từ .env>;Encrypt=True;TrustServerCertificate=True"
dotnet restore
dotnet build
dotnet run --project src/ClinicQueue.Api --no-launch-profile -- --migrate
dotnet run --project src/ClinicQueue.Worker --no-launch-profile -- --migrate
# Terminal riêng cho từng tiến trình; đặt biến connection string ở cả API và Worker.
dotnet run --project src/ClinicQueue.Api --no-launch-profile --urls http://localhost:5000
dotnet run --project src/ClinicQueue.Worker --no-launch-profile
dotnet run --project src/ClinicQueue.Notification --no-launch-profile --urls http://localhost:5001
```

Frontend, terminal riêng:

```powershell
cd src/clinicqueue-web
npm.cmd ci
npm.cmd run dev
# http://localhost:5173
```

Vite proxy `/api`, `/health/api`, `/health/notification` sang backend. Production nginx phục vụ SPA và proxy cùng origin. `/hubs` chỉ được thêm cùng SignalR xác thực ở M4.

EF CLI được ghim ở 10.0.14 trong `.config/dotnet-tools.json`. Chạy `dotnet tool restore` tại thư mục gốc trước khi dùng `dotnet ef`. Khi thêm migration ở milestone sau, chỉ định `--project src/ClinicQueue.Api` hoặc `--project src/ClinicQueue.Worker` và đặt connection string tương ứng. Không nâng lên EF Core 11.

## Health và kiểm thử

```powershell
dotnet restore
dotnet build
npm.cmd --prefix src/clinicqueue-web run build
docker compose config --quiet
docker compose ps -a
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-M0.ps1
# Truyền -WebPort/-ApiPort/-NotificationPort nếu đã đổi cổng.
Invoke-WebRequest http://localhost:5000/health
Invoke-WebRequest http://localhost:5001/health
Invoke-WebRequest http://localhost:8088/health/api
Invoke-WebRequest http://localhost:8088/health/notification
Invoke-WebRequest http://localhost:8088
```

API `/health` kiểm tra kết nối SQL (503 nếu không sẵn sàng); `/health/live` chỉ kiểm tra tiến trình HTTP. Notification M0 chỉ kiểm tra host sống, chưa kiểm tra broker vì chưa có consumer. Worker giữ nguyên .NET Worker Service, không mở HTTP port: health check kết nối SQL mỗi 10 giây và ghi heartbeat tại `/tmp/clinicqueue-worker.healthy` trong container; Docker chỉ healthy nếu file còn mới dưới một phút. Khi SQL lỗi, file bị xóa; khi Worker chết/treo, file hết hạn. Khi chạy local, dùng đường dẫn temp hệ điều hành hoặc cấu hình `Health__HeartbeatPath`. Docker kiểm tra broker riêng bằng rabbitmq-diagnostics.

Xem log trong phạm vi dự án: `docker compose logs --tail 100 api worker notification api-migrate worker-migrate`. Nếu migration thất bại, sửa lỗi rồi chạy lại `docker compose up -d --build --wait`; không xóa dữ liệu. Khi đổi mật khẩu trong .env, credentials trong volume hiện hữu không tự đổi; sử dụng credentials đã khởi tạo hoặc đổi mật khẩu bằng công cụ quản trị, không reset volume.

## Cấu trúc và ranh giới

- `ClinicQueue.slnx`: 4 project .NET 10 hiện hữu, 3 service tham chiếu Contracts.
- `src/ClinicQueue.Api`: HTTP API, ApiDbContext, health SQL; chưa có business controllers.
- `src/ClinicQueue.Worker`: background host, WorkerDbContext, readiness heartbeat; chỉ Worker được ghi trạng thái hàng đợi ở M3+.
- `src/ClinicQueue.Notification`: HTTP host, health; không có database nghiệp vụ.
- `src/ClinicQueue.Contracts`: shared metadata, contracts nghiệp vụ bổ sung M1–M3.
- `src/clinicqueue-web`: React + TypeScript + Vite + Tailwind CSS 4, trang khởi đầu responsive, kiểm tra trạng thái service.
- `Dockerfile`, `compose.yaml`, `docker/`: build, hạ tầng và RabbitMQ topology.

DESIGN.md đã sửa khóa SQL Server dùng UPDLOCK/HOLDLOCK, bộ đếm QueueCounters tăng đơn điệu không tái sử dụng số hủy, và API gửi command qua outbox thay vì ghi QueueEntries. Chỉ thực hiện các cơ chế nghiệp vụ này ở milestone tương ứng.

## Kết quả kiểm thử M0 — 18/11/2026

- `dotnet restore`: đạt; `dotnet build --no-restore`: 0 warning, 0 error.
- `dotnet tool restore`: EF CLI 10.0.14; cả hai model không có thay đổi chưa được migration ghi nhận.
- `npm.cmd run build`: đạt; đã build lại frontend trong Docker.
- `docker compose config --quiet`: đạt; build image và khởi động thực tế thành công.
- `scripts/Test-M0.ps1`: đạt tất cả HTTP, health và migration checks. SQL Server, RabbitMQ, API, Worker, Notification, web đều healthy; hai migration job Exited (0). Chạy lại migrations không tạo thêm bản ghi lịch sử.
- Truy vấn SQL trực tiếp xác nhận database `ClinicQueue`, chỉ có hai bảng lịch sử migration M0 trong schema `api` và `worker`, phiên bản EF 10.0.14.
- RabbitMQ Management đăng nhập 200; 6 durable queue, 0 consumer; hai queue nhận `ticket.assigned`. Hostname broker được cố định để dữ liệu RabbitMQ tiếp tục dùng cùng node khi tạo lại container.
- Edge headless kiểm tra React qua nginx tại cổng 8088: viewport 390px và 1440px, không có JavaScript exception; mobile không tràn ngang; hiển thị dịch vụ sẵn sàng. Ảnh kiểm tra được đính kèm tại [desktop](docs/images/frontend-desktop.png) và [mobile](docs/images/frontend-mobile.png).
- Kiểm thử khi SQL chưa có: API liveness 200, readiness 503; Worker không ghi heartbeat. Console logging đã được cấu hình để chạy được trong terminal Windows không có quyền Event Log.

Các container `crm_*` không bị thay đổi hoặc dừng. Không xóa dữ liệu, prune hay reset volume. M1–M6 chưa được triển khai; chưa phát hiện lỗi M0 còn tồn tại qua các kiểm thử trên.
