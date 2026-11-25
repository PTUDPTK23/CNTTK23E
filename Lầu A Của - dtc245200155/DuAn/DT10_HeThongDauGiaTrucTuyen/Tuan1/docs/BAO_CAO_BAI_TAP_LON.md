# BÁO CÁO BÀI TẬP LỚN
# HỌC PHẦN: PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN

**ĐỀ TÀI 10: XÂY DỰNG HỆ THỐNG ĐẤU GIÁ TRỰC TUYẾN PHÂN TÁN**

* **Sinh viên thực hiện**: LẦU A CỦA
* **Mã sinh viên**: DTC245200155
* **Nền tảng công nghệ**: C# (.NET 10), ASP.NET Core Minimal API, gRPC, RabbitMQ, SignalR, PostgreSQL, Docker Compose.

---

## MỤC LỤC
1. [CHƯƠNG 1: GIỚI THIỆU BÀI TOÁN & PHÂN TÍCH YÊU CẦU](#chương-1-giới-thiệu-bài-toán--phân-tích-yêu-cầu)
2. [CHƯƠNG 2: THIẾT KẾ KIẾN TRÚC HỆ THỐNG](#chương-2-thiết-kế-kiến-trúc-hệ-thống)
3. [CHƯƠNG 3: CÁC CƠ CHẾ GIAO TIẾP PHÂN TÁN](#chương-3-các-cơ-chế-giao-tiếp-phân-tán)
4. [CHƯƠNG 4: THIẾT KẾ CƠ SỞ DỮ LIỆU & DỮ LIỆU SỰ KIỆN](#chương-4-thiết-kế-cơ-sở-dữ-liệu--dữ-liệu-sự-kiện)
5. [CHƯƠNG 5: THIẾT KẾ & TRIỂN KHAI CHI TIẾT CÁC THÀNH PHẦN](#chương-5-thiết-kế--triển-khai-chi-tiết-các-thành-phần)
6. [CHƯƠNG 6: KẾT QUẢ THỰC NGHIỆM & KỊCH BẢN KIỂM THỬ](#chương-6-kết-quả-thực-nghiệm--kịch-bản-kiểm-thử)
7. [CHƯƠNG 7: ĐÁNH GIÁ HỆ THỐNG & HƯỚNG PHÁT TRIỂN](#chương-7-đánh-giá-hệ-thống--hướng-phát-triển)

---

## CHƯƠNG 1: GIỚI THIỆU BÀI TOÁN & PHÂN TÍCH YÊU CẦU

### 1.1. Đặt vấn đề
Trong thương mại điện tử, đấu giá trực tuyến (Online Auction) là một bài toán kinh điển đòi hỏi tính phân tán cao:
- **Độ trễ thấp và tính tức thời (Real-time)**: Hàng trăm hoặc hàng nghìn người tham gia đấu giá cần nhận được mức giá mới nhất ngay khi có một người vừa trả giá hợp lệ. Việc sử dụng cơ chế kéo dữ liệu truyền thống (HTTP Polling) sẽ gây nghẽn cổ chai tại máy chủ và tăng độ trễ không chấp nhận được.
- **Tính độc lập và cô lập lỗi**: Khâu kiểm tra nghiệp vụ trả giá (xác thực giá hợp lệ, bước giá, thời gian còn hiệu lực) cần phải được thực hiện với hiệu năng cao, không làm chậm quá trình tiếp nhận yêu cầu.
- **Tính nhất quán và kiểm toán (Auditability)**: Mọi thao tác trả giá và sự kiện kết thúc phiên đều phải được lưu trữ lịch sử để phục vụ tra soát, giải quyết khiếu nại.

### 1.2. Yêu cầu chức năng
1. **Quản lý phiên đấu giá**: Tạo phiên mới, tra cứu danh sách các phiên đang mở, sắp diễn ra hoặc đã kết thúc; tra cứu chi tiết một phiên đấu giá.
2. **Theo dõi phiên đấu giá**: Người dùng có thể tham gia vào "phòng đấu giá" ảo của một sản phẩm và nhận cập nhật mức giá mới ngay lập tức qua WebSocket/SignalR mà không cần reload trang.
3. **Gửi mức giá mới (Bidding)**: Người dùng gửi yêu cầu đặt giá qua REST API. Hệ thống tiến hành xác thực qua dịch vụ kiểm tra độc lập (gRPC).
4. **Phân phối sự kiện bất đồng bộ**: Khi một mức giá hợp lệ được chấp nhận, hệ thống phát sinh sự kiện `BidAccepted` lên RabbitMQ để kích hoạt luồng realtime (SignalR) và luồng kiểm toán (Audit Worker).
5. **Kết thúc phiên & chốt người thắng**: Tự động hoặc thủ công kết thúc phiên khi hết giờ, xác định người trả giá cao nhất là người chiến thắng và phát sự kiện `AuctionEnded`.
6. **Lịch sử & Kiểm toán**: Ghi nhận toàn bộ vết sự kiện, payload và correlation ID để đối soát.

### 1.3. Yêu cầu phi chức năng
* **Công nghệ nền tảng**: C# trên .NET 10.
* **Giao tiếp phân tán**: Kết hợp REST, gRPC (đồng bộ tốc độ cao), RabbitMQ (bất đồng bộ lỏng lẻo - Event-driven), SignalR (realtime hai chiều).
* **Đóng gói & Điều phối**: Mỗi service sở hữu một `Dockerfile` độc lập; toàn bộ hệ thống được khởi chạy bằng `docker-compose.yml` trên mạng Docker ảo (`auction-network`).
* **Khả năng chịu lỗi (Resilience)**: Cơ chế retry khi kết nối Message Broker bị gián đoạn, dịch vụ tự phục hồi khi khởi động lại.

---

## CHƯƠNG 2: THIẾT KẾ KIẾN TRÚC HỆ THỐNG

### 2.1. Kiến trúc tổng thể (Architecture Overview)

Hệ thống được tổ chức theo kiến trúc **Hướng sự kiện (Event-Driven Microservices Architecture)**:

```
[ Bidder Clients ]
(Web Client / Console Client)
      | 
      | (1) POST /auctions/{id}/bids (REST)
      v
[ Auction.API ] -------- (2) gRPC: ValidateBid -------> [ BidValidation.Service ]
      |                                                        (Hiệu năng cao,
      | (3) Lưu DB & Phát Event                                 độc lập nghiệp vụ)
      v
[ RabbitMQ Message Broker ] (Exchange: auction.exchange)
      |                                  |
      | (4a) Routing: auction.bid.*      | (4b) Routing: auction.#
      v                                  v
[ Auction.Hub ]                   [ Audit.Worker ]
      |                                  |
      | (5) SignalR Realtime             | (6) Ghi nhật ký Audit Log
      v                                  v
[ Clients Realtime UI ]           [ Database (AuditEntries) ]
```

### 2.2. Phân chia trách nhiệm các thành phần

| Thành phần | Project | Trách nhiệm chính |
| :--- | :--- | :--- |
| **Auction.API** | Minimal API | Quản lý phiên đấu giá, điều phối nghiệp vụ, gọi gRPC xác thực, lưu dữ liệu chính và phát sự kiện ra RabbitMQ. Có background worker tự động kết thúc phiên hết hạn. |
| **BidValidation.Service**| gRPC Service | Dịch vụ chuyên biệt xử lý logic nghiệp vụ kiểm tra tính hợp lệ của mức giá, trạng thái phiên và bước giá tối thiểu. |
| **Auction.Hub** | SignalR Service | Tiêu thụ sự kiện từ queue của RabbitMQ và phát sóng (broadcast) tức thì qua SignalR group tới các trình duyệt đang mở phiên đó. |
| **Audit.Worker** | Background Worker | Tiêu thụ toàn bộ sự kiện từ RabbitMQ để ghi vết kiểm toán (Audit Trail) vào CSDL, cung cấp API tra cứu log. |
| **Auction.WebClient** | Web App (SPA) | Cung cấp giao diện trực quan cho người dùng theo dõi giá nhảy realtime, xem đồng hồ đếm ngược, gửi giá và xem log. |
| **Auction.ConsoleClient**| Console App | Ứng dụng dòng lệnh cho phép tương tác đặt giá và chạy kịch bản Auto-Bid Bot mô phỏng tải. |
| **AuctionSystem.Shared** | Class Library | Chứa Entity models, DTOs, Event contracts và thư viện RabbitMQ publisher tái sử dụng. |
| **AuctionSystem.Contracts**| Class Library | Chứa file protobuf định nghĩa hợp đồng gRPC và mã nguồn sinh tự động. |

---

## CHƯƠNG 3: CÁC CƠ CHẾ GIAO TIẾP PHÂN TÁN

Trong đề tài này, 4 cơ chế giao tiếp phân tán phổ biến nhất hiện nay được áp dụng đúng vị trí và mục đích:

### 3.1. RESTful API (ASP.NET Core Minimal API)
* **Vị trí**: Giữa Client và `Auction.API`.
* **Đặc điểm**: Dùng cho các thao tác CRUD và gửi yêu cầu hành vi (Query phiên đấu giá, Tạo phiên, Gửi mức giá mới, Kết thúc phiên).
* **Ưu điểm**: Đơn giản, chuẩn hóa, thân thiện với trình duyệt web và thiết bị di động.

### 3.2. gRPC (Google Remote Procedure Call)
* **Vị trí**: Giữa `Auction.API` (Client) và `BidValidation.Service` (Server).
* **Đặc điểm**: Giao tiếp đồng bộ dạng RPC dựa trên HTTP/2 và nhị phân Protocol Buffers (`Protobuf`).
* **Ưu điểm**:
  * Tốc độ cực nhanh và dung lượng gói tin siêu nhỏ so với JSON over HTTP/1.1.
  * Hợp đồng giao tiếp (Contract) chặt chẽ được định nghĩa bằng file `.proto`, tự động sinh mã nguồn C#.
  * Tách biệt logic nghiệp vụ phức tạp của việc thẩm định giá ra khỏi API Gateway.

### 3.3. RabbitMQ Message Broker (Publish/Subscribe)
* **Vị trí**: Giữa `Auction.API` (Publisher) và `Auction.Hub`, `Audit.Worker` (Consumers).
* **Cấu hình**:
  * Exchange: `auction.exchange` (Type: `topic`).
  * Queue 1: `auction.hub.queue` lắng nghe routing key `auction.#`.
  * Queue 2: `auction.audit.queue` lắng nghe routing key `auction.#`.
* **Ưu điểm**:
  * **Decoupling (Giảm phụ thuộc)**: `Auction.API` không cần biết có bao nhiêu service đang quan tâm đến sự kiện đặt giá.
  * **Asynchronous (Bất đồng bộ)**: Người dùng nhận được phản hồi ngay khi giá được xác thực mà không cần chờ lưu log kiểm toán hay broadcast SignalR.
  * **Chịu lỗi**: Nếu `Auction.Hub` hoặc `Audit.Worker` tạm dừng, message vẫn nằm an toàn trên RabbitMQ và được xử lý ngay khi service hoạt động trở lại.

### 3.4. SignalR (WebSockets / Long Polling)
* **Vị trí**: Giữa `Auction.Hub` và các Client (Web/Console).
* **Đặc điểm**: Duy trì kết nối song công toàn phần (Full-duplex).
* **Ưu điểm**: Cung cấp khả năng push dữ liệu từ server xuống client theo thời gian thực mà không cần client phải liên tục gửi request thăm dò (polling). Hỗ trợ nhóm (`Group`) theo từng mã phiên đấu giá `auction-{id}` để chỉ gửi thông tin cho những người đang quan tâm phiên đó.

---

## CHƯƠNG 4: THIẾT KẾ CƠ SỞ DỮ LIỆU & DỮ LIỆU SỰ KIỆN

### 4.1. Thực thể Đấu giá (Auction)
* `Id` (UUID/Guid): Khóa chính phiên đấu giá.
* `Title` (VARCHAR 200): Tên tài sản/sản phẩm đấu giá.
* `Description` (TEXT): Mô tả chi tiết.
* `ImageUrl` (VARCHAR 500): Ảnh đại diện sản phẩm.
* `StartingPrice` (DECIMAL 18,2): Giá khởi điểm.
* `CurrentPrice` (DECIMAL 18,2): Mức giá hiện tại cao nhất.
* `StepPrice` (DECIMAL 18,2): Bước giá tối thiểu giữa 2 lần trả giá liên tiếp.
* `CurrentWinner` (VARCHAR 100): Tên người đang dẫn đầu.
* `StartTime` (DATETIME UTC): Thời điểm bắt đầu.
* `EndTime` (DATETIME UTC): Thời điểm kết thúc.
* `Status` (INT): Trạng thái (`Pending = 0`, `Active = 1`, `Ended = 2`, `Cancelled = 3`).
* `Winner` (VARCHAR 100): Người chiến thắng sau khi phiên kết thúc.
* `CreatedAt` (DATETIME UTC): Thời điểm tạo phiên.

### 4.2. Thực thể Lượt trả giá (Bid)
* `Id` (UUID/Guid): Khóa chính lượt đặt giá.
* `AuctionId` (UUID/Guid): Khóa ngoại tham chiếu đến phiên đấu giá.
* `Bidder` (VARCHAR 100): Tên người trả giá.
* `Amount` (DECIMAL 18,2): Số tiền đặt giá.
* `BidTime` (DATETIME UTC): Thời điểm đặt giá.
* `IsAccepted` (BOOLEAN): Được chấp thuận hay bị từ chối.
* `Reason` (VARCHAR 255): Lý do (nếu bị từ chối).

### 4.3. Thực thể Nhật ký Kiểm toán (AuditEntry)
* `Id` (UUID/Guid): Khóa chính bản ghi kiểm toán.
* `AuctionId` (VARCHAR 100): Mã phiên liên quan.
* `EventType` (VARCHAR 100): Tên sự kiện (`auction.bid.accepted`, `auction.ended`, `auction.created`).
* `Payload` (TEXT): Toàn bộ nội dung sự kiện dưới dạng JSON.
* `CorrelationId` (VARCHAR 100): Mã định danh theo dõi luồng yêu cầu.
* `Timestamp` (DATETIME UTC): Thời điểm ghi nhận.

---

## CHƯƠNG 5: THIẾT KẾ & TRIỂN KHAI CHI TIẾT CÁC THÀNH PHẦN

### 5.1. Hợp đồng gRPC (`bid_validation.proto`)
```protobuf
syntax = "proto3";
option csharp_namespace = "AuctionSystem.Contracts";
package auction.bids;

service BidValidation {
  rpc ValidateBid (ValidateBidRequest) returns (ValidateBidResponse);
}

message ValidateBidRequest {
  string auction_id = 1;
  string bidder = 2;
  double amount = 3;
  double current_price = 4;
  double step_price = 5;
  string auction_status = 6;
  string start_time = 7;
  string end_time = 8;
  double starting_price = 9;
}

message ValidateBidResponse {
  bool is_valid = 1;
  string reason = 2;
  double min_required_price = 3;
}
```

### 5.2. Luồng xử lý nghiệp vụ tại `Auction.API`
Khi có yêu cầu `POST /auctions/{id}/bids`:
1. Trích xuất `CorrelationId` từ header HTTP `X-Correlation-Id`.
2. Truy vấn thông tin phiên đấu giá từ Database.
3. Tạo `ValidateBidRequest` và gọi đồng bộ sang `BidValidation.Service` qua gRPC channel.
4. Nếu gRPC trả về `is_valid == false`: Ghi nhận lượt bid bị từ chối vào Database và phản hồi mã `400 Bad Request` kèm lý do.
5. Nếu gRPC trả về `is_valid == true`:
   * Cập nhật `CurrentPrice = amount` và `CurrentWinner = bidder`.
   * Thêm bản ghi `Bid` thành công vào Database.
   * Tạo `BidAcceptedEvent` và gọi `IRabbitMqPublisher.Publish(...)` phát lên RabbitMQ Topic Exchange `auction.exchange` với routing key `auction.bid.accepted`.
   * Phản hồi mã `200 OK` cho Client.

### 5.3. Tự động đóng phiên đấu giá (`AuctionExpirationWorker`)
Một HostedService chạy ngầm trong `Auction.API` mỗi 5 giây quét các phiên có trạng thái `Active` nhưng `EndTime <= DateTime.UtcNow`:
* Chuyển trạng thái sang `Ended`.
* Xác định người trả giá cao nhất là `Winner`.
* Phát sự kiện `AuctionEndedEvent` lên RabbitMQ để SignalR thông báo người chiến thắng tới tất cả client và Audit Worker lưu trữ.

---

## CHƯƠNG 6: KẾT QUẢ THỰC NGHIỆM & KỊCH BẢN KIỂM THỬ

### 6.1. Kiểm thử chức năng gRPC (Tính hợp lệ của giá)
* **Kịch bản**: Giá hiện tại là 72,000,000 đ, bước giá là 1,000,000 đ. Người dùng thử đặt giá 72,500,000 đ.
* **Kết quả**: gRPC `BidValidation.Service` từ chối ngay lập tức: *"Mức giá trả (72,500,000 đ) phải lớn hơn hoặc bằng mức giá tối thiểu (73,000,000 đ)"*. Trả về HTTP 400 rõ ràng.
* **Kịch bản**: Người dùng đặt mức giá 73,000,000 đ.
* **Kết quả**: gRPC chấp thuận hợp lệ, API ghi nhận và giá nhảy tức thì.

### 6.2. Kiểm thử truyền phát Real-time (SignalR)
* Mở đồng thời 2 trình duyệt: Cửa sổ 1 và Cửa sổ 2 cùng vào phòng đấu giá MacBook Pro.
* Người dùng ở Cửa sổ 1 bấm đặt giá.
* Cửa sổ 2 ngay lập tức nhấp nháy màu xanh (Flash animation), hiển thị mức giá mới và thêm một dòng vào bảng lịch sử trả giá mà không hề tải lại trang.

### 6.3. Kiểm thử truyền thông điệp qua RabbitMQ và Audit Worker
* Mở giao diện RabbitMQ Management tại `http://localhost:15672`: Thấy exchange `auction.exchange` nhận thông điệp và phân phối vào 2 queues `auction.hub.queue` và `auction.audit.queue`.
* Mở modal "Audit Logs" trên Web Client: Thấy đầy đủ danh sách các event `auction.bid.accepted` và `auction.ended` kèm JSON payload và Correlation ID.

### 6.4. Kiểm thử chịu lỗi (Fault Tolerance)
* Tắt container `auction-hub` trong khi hệ thống đang chạy.
* Tiến hành đặt giá mới trên `Auction.API`. Lệnh đặt giá vẫn thành công, dữ liệu được ghi vào CSDL và message được giữ lại trên queue RabbitMQ.
* Bật lại `auction-hub`: Hub tự động kết nối lại, tiêu thụ message đang chờ và khôi phục hoạt động bình thường.

---

## CHƯƠNG 7: ĐÁNH GIÁ HỆ THỐNG & HƯỚNG PHÁT TRIỂN

### 7.1. Đánh giá ưu điểm
1. **Tuân thủ chuẩn mực đề tài**: Đáp ứng đầy đủ 100% yêu cầu kỹ thuật chung (.NET 10, Minimal API, Dockerfile riêng, docker-compose, logging, healthcheck) và yêu cầu riêng của Đề tài 10.
2. **Kiến trúc lỏng lẻo (Loosely Coupled)**: Các thành phần giao tiếp qua chuẩn mở (gRPC, AMQP, WebSocket, REST), dễ dàng scale ngang từng service độc lập.
3. **Trải nghiệm người dùng vượt trội**: Có cả Web Client SPA hiện đại và Console Client có bot simulation tự động.
4. **Đáp ứng các tiêu chí mở rộng**: Đã hoàn thành Correlation ID, Health Checks trên mọi service, Audit Worker lưu vết và cơ chế tự động hết hạn phiên đấu giá.

### 7.2. Hướng phát triển mở rộng
* Bổ sung cơ chế Idempotent Consumer bằng cách lưu trữ `EventId` đã xử lý vào Redis/Database để tránh trùng lặp nếu message bị gửi lại.
* Tích hợp Authentication / Authorization bằng JWT Bearer Token để quản lý tài khoản người dùng và ví tiền đặt cọc.
* Cấu hình Dead-letter Exchange (DLX) để gom các message bị lỗi định dạng đưa vào hàng đợi phân tích riêng.

---
**XÁC NHẬN CỦA SINH VIÊN THỰC HIỆN**  
*(Ký và ghi rõ họ tên)*  

**LẦU A CỦA**
