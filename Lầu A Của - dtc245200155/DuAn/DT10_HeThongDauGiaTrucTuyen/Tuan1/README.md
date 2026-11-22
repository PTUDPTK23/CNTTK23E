# HỆ THỐNG ĐẤU GIÁ TRỰC TUYẾN (ONLINE AUCTION SYSTEM)
### Học phần: Phát triển ứng dụng phân tán - Đề tài 10 (Tuần 1)

---

## 👤 Thông tin Sinh viên & Đề tài
* **Họ và tên**: Lầu A Của
* **Mã sinh viên (MSSV)**: `dtc245200155`
* **Lớp**: K23.K1.D2.2627.CNTT K23E
* **Đề tài**: **10 – Hệ thống đấu giá trực tuyến**
* **Thư mục nộp bài**: `Lầu A Của - dtc245200155/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/`
* **Mã đề tài đăng ký**: `DT10_HeThongDauGiaTrucTuyen`
* **Giai đoạn**: Báo cáo & Sản phẩm Tuần 1 (Tuan1)

---

## 📌 Giới thiệu Đề tài
Dự án xây dựng hệ thống **Đấu giá trực tuyến phân tán** theo mô hình Microservices, cho phép nhiều người dùng (Bidders) theo dõi các phiên đấu giá và gửi các lượt trả giá mới theo thời gian thực (Real-time).

Hệ thống đáp ứng đầy đủ các yêu cầu kỹ thuật và kiến trúc:
* **Auction API**: Tiếp nhận yêu cầu REST Minimal API từ Client.
* **Bid Validation Service**: Hoạt động độc lập, xác thực tính hợp lệ của lượt đặt giá qua giao thức **gRPC**.
* **RabbitMQ Message Broker**: Đóng vai trò Event Bus phân phối các sự kiện (`BidAccepted`, `AuctionEnded`, `AuctionCreated`).
* **Auction Hub (SignalR)**: Nhận sự kiện từ RabbitMQ và broadcast mức giá mới tức thì đến các Client đang theo dõi phiên đấu giá.
* **Audit Worker**: Nhận sự kiện từ RabbitMQ để ghi lại nhật ký kiểm toán và lịch sử giao dịch vào cơ sở dữ liệu.
* **Clients**: Bao gồm cả **Web App (SPA Realtime)** trực quan và **Console App** tương tác / mô phỏng bot tự động.

---

## 🏛️ Sơ đồ Kiến trúc Hệ thống

```mermaid
flowchart TD
    subgraph Clients ["Lớp Client"]
        WebClient["🌐 Web Client (SPA / Port 5000)"]
        ConsoleClient["💻 Console Client (Bot / CLI)"]
    end

    subgraph CoreServices ["Lớp Dịch vụ Phân tán (.NET 10)"]
        AuctionAPI["🚀 Auction API\n(ASP.NET Core Minimal API :5001)"]
        BidValidation["⚡ Bid Validation Service\n(gRPC Server :5002)"]
        AuctionHub["📡 Auction Hub\n(SignalR Broadcast :5003)"]
        AuditWorker["🛡️ Audit Worker\n(Event Logging :5004)"]
    end

    subgraph Middleware ["Middleware & Storage"]
        RabbitMQ["🐰 RabbitMQ Message Broker\n(Exchange: auction.exchange :5672)"]
        Database[("🗄️ Database\n(PostgreSQL / SQLite)")]
    end

    %% Giao tiếp giữa các thành phần
    WebClient -->|HTTP REST: POST /auctions/{id}/bids| AuctionAPI
    ConsoleClient -->|HTTP REST / Auto-Bid Bot| AuctionAPI
    AuctionAPI -->|gRPC: ValidateBid| BidValidation
    BidValidation -.->|Valid / Invalid + Reason| AuctionAPI

    AuctionAPI -->|Save Bid & Update Price| Database
    AuctionAPI -->|Publish BidAcceptedEvent / AuctionEndedEvent| RabbitMQ

    RabbitMQ -->|Queue: auction.hub.queue| AuctionHub
    RabbitMQ -->|Queue: auction.audit.queue| AuditWorker

    AuctionHub -->|SignalR Realtime WebSocket| WebClient
    AuctionHub -->|SignalR Realtime Event| ConsoleClient

    AuditWorker -->|Persist Audit Logs| Database
```

---

## 📋 Danh mục Services & Cổng kết nối (Port Mapping)

| Service | Vai trò / Công nghệ | Cổng Container | Cổng Host (Docker / Local) |
| :--- | :--- | :--- | :--- |
| **web-client** | Giao diện Web đấu giá Realtime (HTML5, JS, Bootstrap) | 5000 | `http://localhost:5000` |
| **auction-api** | REST Minimal API quản lý phiên và nhận bid | 5001 | `http://localhost:5001` (Swagger: `/swagger`) |
| **bid-validation-service** | Dịch vụ gRPC kiểm tra mức giá độc lập | 5002 | `http://localhost:5002` (Health: `/health`) |
| **auction-hub** | SignalR Hub phát giá realtime từ RabbitMQ | 5003 | `http://localhost:5003/hubs/auction` |
| **audit-worker** | Worker lưu trữ nhật ký kiểm toán từ RabbitMQ | 5004 | `http://localhost:5004/api/audit-logs` |
| **rabbitmq** | Message Broker & Quản trị giao diện RabbitMQ | 5672, 15672 | AMQP: `5672`, Web UI: `http://localhost:15672` (guest/guest) |
| **db** | Cơ sở dữ liệu PostgreSQL | 5432 | `localhost:5432` |

---

## 🛠️ Yêu cầu Kỹ thuật Đã Hoàn thành

- [x] **Ngôn ngữ**: C# (.NET 10 SDK).
- [x] **API**: ASP.NET Core Minimal API.
- [x] **Client**: Cung cấp cả **Web App** (giao diện SPA trực quan) và **Console App** (mô phỏng auto-bid bot).
- [x] **Giao tiếp phân tán**:
  - **REST API**: Quản lý phiên đấu giá và gửi mức giá.
  - **gRPC**: Kiểm tra tính hợp lệ của lượt trả giá độc lập (`ValidateBid`).
  - **RabbitMQ**: Topic Exchange phân phối sự kiện bất đồng bộ (`auction.exchange`).
  - **SignalR**: Broadcast cập nhật realtime không cần reload trang.
- [x] **Cơ sở dữ liệu**: Hỗ trợ PostgreSQL (Docker Compose) và SQLite (Local Dev).
- [x] **Đóng gói Docker**: Từng service có **Dockerfile riêng**, cấu hình **docker-compose.yml** điều phối cả hệ thống và healthchecks.
- [x] **Cấu hình độc lập**: Tách biệt connection strings, rabbitmq host, urls bằng biến môi trường.
- [x] **Resilience & Fault Tolerance**: Retry kết nối RabbitMQ, tự động hồi phục khi service restart.
- [x] **Các tiêu chí mở rộng**:
  - **Health check** (`/health`) trên tất cả các service.
  - **Correlation ID** tracking qua HTTP headers và message properties.
  - **Audit Worker** ghi nhận toàn bộ payload và lịch sử giao dịch.
  - Tự động đóng phiên đấu giá hết hạn (`AuctionExpirationWorker`).

---

## 🚀 Hướng dẫn Cài đặt và Khởi chạy

### Cách 1: Khởi chạy bằng Docker Compose (Khuyên dùng khi nộp bài & chấm thi)

1. Mở Docker Desktop trên máy.
2. Tại thư mục gốc của dự án, mở Terminal (PowerShell hoặc Bash) và gõ lệnh:
```bash
docker compose up --build
```
3. Sau khi tất cả các container khởi động thành công (`healthy`), truy cập:
* **Giao diện Web Client**: [http://localhost:5000](http://localhost:5000)
* **Tài liệu Swagger API**: [http://localhost:5001/swagger](http://localhost:5001/swagger)
* **RabbitMQ Dashboard**: [http://localhost:15672](http://localhost:15672) *(Tài khoản: `guest` / `guest`)*
* **Audit Logs API**: [http://localhost:5004/api/audit-logs](http://localhost:5004/api/audit-logs)

Để dừng hệ thống:
```bash
docker compose down
```

---

### Cách 2: Khởi chạy cục bộ (Local Development bằng .NET SDK)

Nếu chưa bật Docker hoặc muốn debug từng service:
1. Đảm bảo đã cài đặt .NET 10 SDK.
2. Chạy file script PowerShell tự động bật toàn bộ các cửa sổ service:
```powershell
.\run-local.ps1
```
3. Hoặc chạy thủ công từng service:
```bash
# Terminal 1: Bid Validation Service (gRPC)
cd src/Services/BidValidation.Service && dotnet run

# Terminal 2: Auction Hub (SignalR)
cd src/Services/Auction.Hub && dotnet run

# Terminal 3: Audit Worker
cd src/Services/Audit.Worker && dotnet run

# Terminal 4: Auction API (REST)
cd src/Services/Auction.API && dotnet run

# Terminal 5: Web Client
cd src/Clients/Auction.WebClient && dotnet run
```

---

### Cách 3: Chạy Console Client (Mô phỏng Bot hoặc Đặt giá qua Terminal)

1. Mở một cửa sổ Terminal mới:
```bash
cd src/Clients/Auction.ConsoleClient
dotnet run
```
2. Màn hình Console sẽ hiển thị menu lựa chọn:
   * **1**: Xem danh sách các phiên đấu giá.
   * **2**: Xem chi tiết phiên & lịch sử trả giá.
   * **3**: Tham gia phòng đấu giá & nhận giá Realtime từ SignalR Hub.
   * **4**: Gửi mức giá mới (gọi REST API -> gRPC).
   * **5**: Chạy Bot tự động trả giá liên tục để thử nghiệm tải và concurrency.

---

## 📡 Danh sách API (Endpoints)

| Phương thức | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `POST` | `/auctions` | Tạo một phiên đấu giá mới (phát sự kiện `AuctionCreated`) |
| `GET` | `/auctions` | Lấy danh sách tất cả các phiên đấu giá (hỗ trợ lọc `?status=Active`) |
| `GET` | `/auctions/{id}` | Lấy chi tiết thông tin và giá hiện tại của phiên đấu giá |
| `POST` | `/auctions/{id}/bids` | Gửi lượt trả giá mới (gọi gRPC kiểm tra -> phát `BidAccepted`) |
| `GET` | `/auctions/{id}/bids` | Lấy toàn bộ lịch sử các lượt trả giá của phiên |
| `POST` | `/auctions/{id}/end` | Kết thúc sớm phiên đấu giá và chốt người chiến thắng |
| `GET` | `/health` | Kiểm tra trạng thái hoạt động của Service |

---

## 🎬 Kịch bản Demo khi Thuyết trình / Bảo vệ Bài tập lớn

1. **Khởi chạy hệ thống**: Mở Docker Compose `docker compose up` và mở trình duyệt tại `http://localhost:5000`.
2. **Minh họa danh sách phiên**: Hiển thị các sản phẩm đấu giá có sẵn kèm đồng hồ đếm ngược và mức giá hiện tại.
3. **Mở 2 cửa sổ trình duyệt song song (hoặc 1 Web + 1 Console)**:
   * Cùng tham gia vào phiên đấu giá "MacBook Pro 16 inch M3 Max".
   * Người dùng A tại Web gửi mức giá hợp lệ (ví dụ: 73,000,000 đ).
   * Quan sát: Web tự động flash màu xanh và cập nhật giá mới ngay lập tức qua SignalR mà không cần reload! Cửa sổ Console cũng in log realtime nhận sự kiện!
4. **Kiểm tra nghiệp vụ qua gRPC (Bid Validation)**:
   * Nhập mức giá thấp hơn mức giá tối thiểu (ví dụ: 70,000,000 đ).
   * Bấm gửi -> Hệ thống báo lỗi ngay: *"Mức giá trả phải lớn hơn hoặc bằng mức giá tối thiểu..."*. Minh chứng gRPC hoạt động chính xác và độc lập!
5. **Kiểm tra Audit Worker & RabbitMQ**:
   * Bấm nút **Audit Logs** trên thanh điều hướng Web.
   * Hiển thị danh sách các sự kiện `auction.bid.accepted` kèm JSON payload và Correlation ID được ghi nhận vào cơ sở dữ liệu bởi `Audit.Worker`.
6. **Kết thúc phiên đấu giá**:
   * Bấm nút "Kết thúc phiên ngay (Demo)".
   * Sự kiện `AuctionEndedEvent` được phát qua RabbitMQ, SignalR gửi thông báo đến tất cả client và vinh danh người chiến thắng (Winner).
