# HỆ THỐNG XỬ LÝ TỆP BẤT ĐỒNG BỘ (ĐỀ TÀI 05)

Hệ thống xử lý tệp phân tán, bất đồng bộ độ tin cậy cao được xây dựng trên nền tảng **.NET 10**, tuân thủ nghiêm ngặt **Kiến trúc Sạch (Clean Architecture)** và mô hình thiết kế **Outbox Pattern**.

Hệ thống chuyên xử lý các tác vụ đồ họa tốn kém tài nguyên (nén ảnh, tạo thumbnail, chuyển đổi định dạng ảnh PNG sang JPEG) tách biệt hoàn toàn khỏi luồng HTTP Request của người dùng, giải phóng tài nguyên Web API ngay lập tức và đảm bảo khả năng phục hồi lỗi tự động.

---

## 1. Công nghệ Sử dụng

- **Ngôn ngữ & Runtime:** C# 13 / .NET 10 (SDK `10.0.202`)
- **Web API & Giao diện:** ASP.NET Core Web API, Static Files SPA (Bootstrap 5, HTML5/ES6)
- **Xử lý nền:** .NET Worker Service (`BackgroundService`)
- **Cơ sở dữ liệu:** Microsoft SQL Server, Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer 10.0.15`), Code-First Migrations
- **Truyền thông điệp (Message Broker):** `RabbitMQ.Client 7.2.2` (AMQP 0-9-1) kết hợp cơ chế **In-Memory Channel Fallback** (`System.Threading.Channels`)
- **Xử lý Đồ họa:** `SixLabors.ImageSharp 4.1.3`
- **Mô hình kiến trúc:** Clean Architecture, Outbox Pattern (Dual-Write safety), Worker Lease Locking (`LeaseToken`), Exponential Retry & Dead Letter Queue (DLQ).

---

## 2. Kiến trúc Tổng quan và Luồng Xử lý

Hệ thống gồm 4 project cấu trúc theo Clean Architecture:

```
                  ┌────────────────────────────────────────┐
                  │          Client / Web UI (SPA)         │
                  └──────┬─────────────────────────▲───────┘
                         │ 1. POST /files          │ 6. GET /files/{id}/result
                         │    POST /jobs           │    GET /jobs/{id}
                         ▼                         │
                  ┌────────────────────────────────┴───────┐
                  │           FileProcessing.Api           │
                  └──────┬─────────────────────────────────┘
                         │ 2. Lưu file gốc / Ghi Job & Outbox trong 1 SQL Tx
                         ▼
        ┌─────────────────────────────────────────────────────┐
        │        FileProcessing.Infrastructure                │
        │  ┌───────────────────────┐ ┌──────────────────────┐ │
        │  │ LocalFileStorage      │ │ OutboxPublisher      │ │
        │  │ (uploads, temp, res)  │ │ (Scan OutboxMessages)│ │
        │  └───────────────────────┘ └──────────┬───────────┘ │
        └───────────────────────────────────────┼─────────────┘
                                                │ 3. Publish (RabbitMQ hoặc Fallback)
                                                ▼
        ┌─────────────────────────────────────────────────────┐
        │   Message Queue (RabbitMQ / InMemory Fallback)      │
        └───────────────────────┬─────────────────────────────┘
                                │ 4. Consume Message (Manual Ack)
                                ▼
        ┌─────────────────────────────────────────────────────┐
        │        FileProcessing.Worker (ImageWorker)          │
        │  - 5. Atomic Claim Job + cấp LeaseToken (SQL Server)│
        │  - Đọc file gốc từ LocalFileStorage                 │
        │  - Xử lý ảnh thật bằng ImageSharp                   │
        │  - Lưu file kết quả, commit ProcessingResult (SQL)  │
        │  - Đánh dấu Job: Completed (hoặc Failed sau 3 retry)│
        └─────────────────────────────────────────────────────┘
```

### Trách nhiệm các Project:
1. **`FileProcessing.Core`:** Tầng trung tâm, độc lập hoàn toàn. Chứa các Domain Entities (`UploadedFile`, `ProcessingJob`, `ProcessingResult`, `OutboxMessage`), Enums (`JobStatus`, `JobType`, `OutboxStatus`), Interfaces (`IFileStorageService`, `IImageProcessor`) và Contracts (`ProcessImageJobMessage`).
2. **`FileProcessing.Infrastructure`:** Triển khai truy cập dữ liệu qua EF Core `FileProcessingDbContext`, cấu hình Fluent API, bộ lưu trữ đĩa `LocalFileStorageService`, bộ xử lý đồ họa `ImageSharpProcessor`, `RabbitMqPublisher`, `OutboxPublisherService` và `ImageProcessingWorker`.
3. **`FileProcessing.Api`:** Cung cấp các HTTP Endpoints RESTful chuẩn hóa, phục vụ Web UI tiếng Việt tại thư mục `wwwroot/`.
4. **`FileProcessing.Worker`:** Tiến trình Worker độc lập phục vụ triển khai phân tán đa node.

---

## 3. Cấu trúc Thư mục

```
D:\XuLyTepBatDongBo/
├── AsyncFileProcessing.slnx           # File Solution .NET 10
├── Directory.Build.targets            # Tối ưu hóa build target
├── dotnet-tools.json                  # Manifest công cụ cục bộ (dotnet-ef 10.0.15)
├── README.md                          # Tài liệu hướng dẫn sử dụng chính
├── docs/
│   ├── THIET_KE_KIEN_TRUC.md          # Đặc tả chi tiết thiết kế kiến trúc hệ thống
│   └── BAO_CAO_KIEM_THU.md            # Báo cáo nghiệm thu và kết quả kiểm thử thực tế
├── storage/                           # Thư mục lưu trữ tệp vật lý
│   ├── uploads/                       # Tệp gốc tải lên
│   ├── temp/                          # Tệp tạm thời trong quá trình xử lý
│   └── results/                       # Tệp kết quả sau khi xử lý hoàn tất
└── src/
    ├── FileProcessing.Core/           # Domain Entities, Enums, Contracts, Interfaces
    ├── FileProcessing.Infrastructure/ # EF Core, SQL Server, Storage, ImageSharp, Messaging
    │   └── Persistence/Migrations/    # Migration InitialCreate
    ├── FileProcessing.Api/            # ASP.NET Core Web API Controllers & Web UI
    │   └── wwwroot/                   # Giao diện người dùng SPA (index.html)
    └── FileProcessing.Worker/         # Tiến trình Worker Service chạy nền độc lập
```

---

## 4. Yêu cầu Môi trường

1. **Hệ điều hành:** Windows 10/11, Linux hoặc macOS.
2. **.NET SDK:** Phiên bản `.NET 10.0` (SDK `10.0.202` trở lên).
3. **Cơ sở dữ liệu:** Microsoft SQL Server (Instance `MSSQLSERVER` hoặc `MSSQLLocalDB`).
4. **Message Broker (Tùy chọn):** Docker Desktop hoặc RabbitMQ 3.x với plugin Management (Cổng 5672 và 15672). *(Nếu chưa có RabbitMQ, hệ thống tự động chạy qua In-Memory Fallback Channel an toàn)*.

---

## 5. Hướng dẫn Cấu hình

### 5.1. Cấu hình Chuỗi kết nối SQL Server
Chuỗi kết nối được thiết lập tại `src/FileProcessing.Api/appsettings.json` và `src/FileProcessing.Worker/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=XuLyTepBatDongBo;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
*Lưu ý: Đảm bảo dịch vụ SQL Server (`MSSQLSERVER`) đang ở trạng thái `Running`.*

### 5.2. Cấu hình RabbitMQ
Thiết lập tại mục `RabbitMQ` trong file cấu hình:
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "file.processing.exchange",
    "MainQueueName": "file.processing.main",
    "RetryQueueName": "file.processing.retry",
    "DlqQueueName": "file.processing.dlq",
    "DlxExchangeName": "file.processing.dlx",
    "RoutingKey": "image.process"
  }
}
```
Khởi chạy container RabbitMQ thật bằng Docker (khi môi trường có Docker):
```powershell
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```
Truy cập giao diện quản trị RabbitMQ tại: `http://localhost:15672` (Tài khoản: `guest` / Mật khẩu: `guest`).

---

## 6. Hướng dẫn Chạy Migration CSDL

*(Database `XuLyTepBatDongBo` đã được tạo và áp dụng migration. Dưới đây là câu lệnh tham khảo để cập nhật schema khi triển khai ở môi trường mới, **không tự ý chạy lại** trên database hiện có)*:

```powershell
# Cài đặt công cụ ef cục bộ (nếu máy mới)
dotnet tool restore

# Áp dụng Migration vào SQL Server
dotnet ef database update --project src/FileProcessing.Infrastructure --startup-project src/FileProcessing.Api --context FileProcessingDbContext
```

---

## 7. Hướng dẫn Biên dịch và Khởi chạy

### 7.1. Biên dịch toàn bộ Solution
```powershell
dotnet build AsyncFileProcessing.slnx
```
*(Kết quả mong đợi: `Build succeeded: 0 Warning(s), 0 Error(s)`)*.

### 7.2. Khởi chạy Ứng dụng
Bạn có thể chọn 1 trong 2 phương án khởi chạy:

- **Phương án 1 (Khuyến nghị cho phát triển / demo trọn gói):**
  Chỉ cần khởi chạy project `FileProcessing.Api`. API đã tích hợp sẵn cả Web API, Giao diện Web, Outbox Publisher và Image Worker:
  ```powershell
  dotnet run --project src/FileProcessing.Api
  ```
  Truy cập giao diện web tại: **`http://localhost:5227`**

- **Phương án 2 (Mô hình Microservices / Đa tiến trình):**
  - Mở Terminal 1 (Khởi chạy Web API & Giao diện):
    ```powershell
    dotnet run --project src/FileProcessing.Api
    ```
  - Mở Terminal 2 (Khởi chạy Worker Service độc lập):
    ```powershell
    dotnet run --project src/FileProcessing.Worker
    ```
  *(Cơ chế `LeaseToken` và khóa `LockedUntil` đảm bảo an toàn tuyệt đối, không bao giờ xảy ra tình trạng 2 worker cùng xử lý trùng 1 job)*.

---

## 8. Hướng dẫn Sử dụng Giao diện Web (SPA)

Truy cập trình duyệt tại địa chỉ: `http://localhost:5227`

Giao diện cung cấp quy trình 3 bước trực quan:
1. **Bước 1 - Tải lên tệp ảnh:**
   - Kéo thả hoặc nhấn chọn tệp ảnh PNG / JPEG từ máy tính.
   - Nhấn **Bắt đầu Tải Lên (POST /files)**. Hệ thống sẽ lưu trữ và hiển thị `FileId`.
2. **Bước 2 - Tạo yêu cầu xử lý (Job):**
   - `FileId` được tự động điền.
   - Chọn loại xử lý:
     - **Nén ảnh (`Compress`):** Kèm thanh trượt chất lượng (ví dụ 60%).
     - **Tạo ảnh thu nhỏ (`Thumbnail`):** Nhập kích thước mong muốn (ví dụ 150x150).
     - **Chuyển đổi PNG sang JPEG (`ConvertPngToJpeg`):** Tùy chỉnh chất lượng xuất ra.
   - Nhấn **Tạo Job Bất Đồng Bộ (POST /jobs)**.
3. **Bước 3 - Theo dõi tiến trình & Nhận kết quả:**
   - Hệ thống tự động polling mỗi 1.5 giây để theo dõi sự chuyển đổi trạng thái: `Pending` $\rightarrow$ `Processing` $\rightarrow$ `Completed`.
   - Xem trước ảnh kết quả trực tiếp và nhấn **Tải Kết Quả (GET /files/{id}/result)**.
4. **Bảng Lịch Sử:** Danh sách các tác vụ gần đây được hiển thị chi tiết ở phần dưới trang.

---

## 9. Danh mục API và Ví dụ Request

Hệ thống hỗ trợ song song cả đường dẫn ngắn (`/...`) và tiền tố chuẩn (`/api/...`):

### 9.1. Tải lên tệp (`POST /files`)
- **Mô tả:** Tải lên tệp ảnh nhị phân (.png, .jpg, .jpeg; tối đa 25MB).
- **curl:**
  ```powershell
  curl.exe -X POST "http://localhost:5227/files" -F "file=@test_sample.png"
  ```
- **Response mẫu (201 Created):**
  ```json
  {
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "originalFileName": "test_sample.png",
    "storedFileName": "cabd7c92efb54626b67b38726c4cc65c.png",
    "contentType": "image/png",
    "fileSize": 2349,
    "createdAt": "2026-11-17T13:52:05.365Z",
    "downloadRawUrl": "/files/c9c56d65-4da9-4553-879b-5bba34d3dd53/raw"
  }
  ```

### 9.2. Tạo Job xử lý bất đồng bộ (`POST /jobs`)
- **Mô tả:** Tạo tác vụ xử lý tệp và lưu Outbox trong cùng 1 transaction SQL.
- **curl (Nén ảnh):**
  ```powershell
  curl.exe -X POST "http://localhost:5227/jobs" -H "Content-Type: application/json" -d '{"fileId":"c9c56d65-4da9-4553-879b-5bba34d3dd53","jobType":"Compress","parameters":{"quality":60}}'
  ```
- **curl (Chuyển đổi PNG sang JPEG):**
  ```powershell
  curl.exe -X POST "http://localhost:5227/jobs" -H "Content-Type: application/json" -d '{"fileId":"8d33cb8b-cc6a-4bf7-babe-49026e7e6c47","jobType":"ConvertPngToJpeg","parameters":{"quality":90}}'
  ```
- **Response mẫu (202 Accepted):**
  ```json
  {
    "jobId": "145c3225-4a47-4e6d-a816-22cac805ba06",
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "jobType": "Compress",
    "status": "Pending",
    "createdAt": "2026-11-17T13:52:14.194Z",
    "statusUrl": "/jobs/145c3225-4a47-4e6d-a816-22cac805ba06",
    "resultUrl": "/files/145c3225-4a47-4e6d-a816-22cac805ba06/result"
  }
  ```

### 9.3. Tra cứu trạng thái Job (`GET /jobs/{id}`)
- **curl:**
  ```powershell
  curl.exe "http://localhost:5227/jobs/145c3225-4a47-4e6d-a816-22cac805ba06"
  ```
- **Response mẫu (200 OK):**
  ```json
  {
    "jobId": "145c3225-4a47-4e6d-a816-22cac805ba06",
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "originalFileName": "test_sample.png",
    "jobType": "Compress",
    "status": "Completed",
    "retryCount": 0,
    "maxRetries": 3,
    "createdAt": "2026-11-17T13:52:14.194Z",
    "startedAt": "2026-11-17T13:52:19.684Z",
    "completedAt": "2026-11-17T13:52:20.040Z",
    "errorMessage": null,
    "result": {
      "resultId": "93e46df1-d796-45aa-a097-a97a3df0ef60",
      "fileSize": 7277,
      "width": 400,
      "height": 300,
      "durationMs": 237,
      "createdAt": "2026-11-17T13:52:20.027Z",
      "downloadUrl": "/files/145c3225-4a47-4e6d-a816-22cac805ba06/result"
    }
  }
  ```

### 9.4. Tải tệp kết quả xử lý (`GET /files/{id}/result`)
- **Mô tả:** Tải tệp kết quả nhị phân (.jpg / .png). Hỗ trợ truyền `{id}` linh hoạt là `JobId`, `FileId` hoặc `ResultId`.
- **curl:**
  ```powershell
  curl.exe -s "http://localhost:5227/files/145c3225-4a47-4e6d-a816-22cac805ba06/result" -o "ket_qua.jpg"
  ```
- Nếu Job đang xử lý: Trả về `202 Accepted` kèm JSON thông báo.
- Nếu Job thất bại: Trả về `400 Bad Request` kèm chi tiết lỗi `errorMessage`.
- Nếu hoàn tất: Trả về `200 OK` với stream file nhị phân đính kèm.

### 9.5. Danh sách các Job gần đây (`GET /jobs`)
- **curl:**
  ```powershell
  curl.exe "http://localhost:5227/jobs?limit=20"
  ```

### 9.6. Tải tệp gốc ban đầu (`GET /files/{id}/raw`)
- **curl:**
  ```powershell
  curl.exe -s "http://localhost:5227/files/c9c56d65-4da9-4553-879b-5bba34d3dd53/raw" -o "tep_goc.png"
  ```

---

## 10. Ghi nhận Giới Hạn Môi Trường & Cơ Chế Fallback

> [!WARNING]
> **Hiện trạng thực tế về Message Broker:**
> 1. Trên máy chủ phát triển hiện tại **chưa được cài đặt Docker Desktop, WSL hay Erlang/RabbitMQ Windows Service**. Do đó, cổng kết nối AMQP **5672 chưa mở**, tiến trình RabbitMQ Broker thật **chưa được vận hành trực tiếp**.
> 2. Các đợt kiểm thử xử lý Job thành công vừa qua được thực hiện hoàn toàn thông qua cơ chế **Outbox Pattern kết hợp In-Memory Channel Fallback (`InMemoryJobQueue`)** do hệ thống tự động kích hoạt để bảo toàn tính liên tục trong môi trường phát triển thiếu broker.
> 3. **Hạn chế về tính bền vững (Durability):** Kênh `InMemoryJobQueue` sử dụng bộ nhớ RAM tiến trình (`System.Threading.Channels`). Nếu tiến trình ứng dụng bị crash hoặc tắt đột ngột trước khi Worker kịp dequeue và claim Job vào Database, thông điệp trên RAM sẽ bị mất.
> 4. **Hạn chế của Outbox Publisher hiện tại:** Bản ghi `OutboxMessages` hiện được cập nhật thành `Status = 'Processed'` ngay sau khi đẩy thành công vào Fallback Queue. Cơ chế này đảm bảo không gửi trùng trong cùng phiên chạy, nhưng chưa tương đương với cơ chế **Durable Messaging + Publisher Confirms có xác nhận Ack từ RabbitMQ Broker trên đĩa cứng**.
> 5. **Hướng cải thiện cho Giai đoạn tiếp theo:**
>    - Triển khai RabbitMQ container qua Docker.
>    - Khi có RabbitMQ thật, Outbox Publisher sẽ chỉ đánh dấu `Processed` sau khi nhận được sự kiện `BasicAcks` từ broker.
>    - Bổ sung cơ chế quét phục hồi (Re-sweep Outbox) cho những message bị kẹt ở trạng thái `InFlight` khi tiến trình khởi động lại.

