# BÁO CÁO KẾT QUẢ KIỂM THỬ THỰC TẾ (BÀI NỘP TUẦN 1)

**Dự án:** Hệ thống xử lý tệp bất đồng bộ (Đề tài 05)  
**Nền tảng:** .NET 10 | ASP.NET Core Web API | Worker Service | EF Core 10 | SQL Server  
**Thư mục làm việc:** `D:\XuLyTepBatDongBo`  
**Thời điểm thực hiện kiểm thử:** Ngày 17/11/2026  

---

## 1. Mục Tiêu & Phạm Vi Kiểm Thử

Báo cáo này ghi nhận toàn bộ số liệu và bằng chứng kiểm thử thực tế trên hệ thống đang chạy tại máy chủ phát triển, bao gồm:
1. Xác minh khả năng tiếp nhận và lưu trữ tệp qua HTTP API.
2. Xác minh 3 nghiệp vụ xử lý ảnh cốt lõi: Nén ảnh (`Compress`), Tạo ảnh thu nhỏ (`Thumbnail`), Chuyển đổi định dạng (`ConvertPngToJpeg`).
3. Xác minh chu trình chuyển đổi trạng thái bất đồng bộ: `Pending` $\rightarrow$ `Processing` $\rightarrow$ `Completed`.
4. Xác minh cơ chế tự động thử lại (Retry) và chuyển trạng thái kết thúc `Failed` khi gặp tệp hỏng.
5. Kiểm tra tính hợp lệ về mặt nhị phân (Magic Bytes) của tệp kết quả.
6. Đối chiếu tính toàn vẹn dữ liệu giữa SQL Server, File Storage và API tải về.
7. Đánh giá tính sẵn sàng của kiến trúc Outbox Pattern và cơ chế InMemory Fallback.

---

## 2. Môi Trường Thực Nghiệm

- **Hệ điều hành:** Windows 10/11 (Architecture: x64).
- **.NET SDK:** `10.0.202` (Target Framework: `net10.0`).
- **SQL Server:** Dịch vụ `MSSQLSERVER` cục bộ (`Running`).
- **Cơ sở dữ liệu:** `XuLyTepBatDongBo` (Đã áp dụng Migration `20261116163526_InitialCreate`).
- **Trạng thái Cổng RabbitMQ 5672:** Đóng (`TcpTestSucceeded: False` do máy chưa cài Docker/RabbitMQ). Hệ thống tự kích hoạt kênh **In-Memory Channel Fallback (`InMemoryJobQueue`)**.
- **Địa chỉ dịch vụ API:** `http://localhost:5227`.

---

## 3. Bằng Chứng & Số Liệu Kiểm Thử Từng Ca Nghiệp Vụ

### Ca 1: Tải lên tệp ảnh hợp lệ (`POST /files`)
- **Tệp nguồn:** [test_sample.png](file:///D:/XuLyTepBatDongBo/test_sample.png) (Ảnh PNG 400x300 px, dung lượng 2,349 bytes).
- **HTTP Request:**
  ```http
  POST http://localhost:5227/files HTTP/1.1
  Content-Type: multipart/form-data; boundary=------------------------2odrEcaX5EDGUJ3Zn9Sh38

  [Dữ liệu nhị phân của test_sample.png]
  ```
- **HTTP Response:**
  ```http
  HTTP/1.1 201 Created
  Content-Type: application/json; charset=utf-8
  Location: http://localhost:5227/api/files/c9c56d65-4da9-4553-879b-5bba34d3dd53

  {
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "originalFileName": "test_sample.png",
    "storedFileName": "cabd7c92efb54626b67b38726c4cc65c.png",
    "contentType": "image/png",
    "fileSize": 2349,
    "createdAt": "2026-11-17T13:52:05.3650632Z",
    "downloadRawUrl": "/files/c9c56d65-4da9-4553-879b-5bba34d3dd53/raw"
  }
  ```
- **Xác minh lưu trữ:** Tệp vật lý được tạo tại `storage/uploads/cabd7c92efb54626b67b38726c4cc65c.png`. Bản ghi được tạo trong bảng `UploadedFiles`.

---

### Ca 2: Nén ảnh JPEG/PNG (`JobType: Compress`)
- **Mục tiêu:** Nén ảnh với tham số chất lượng `quality: 60`.
- **HTTP Request tạo Job:**
  ```http
  POST http://localhost:5227/jobs HTTP/1.1
  Content-Type: application/json

  {
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "jobType": "Compress",
    "parameters": { "quality": 60 }
  }
  ```
- **HTTP Response tạo Job:** `HTTP/1.1 202 Accepted`
  ```json
  {
    "jobId": "145c3225-4a47-4e6d-a816-22cac805ba06",
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "jobType": "Compress",
    "status": "Pending",
    "createdAt": "2026-11-17T13:52:14.1941718Z"
  }
  ```
- **Tra cứu kết quả (`GET /jobs/145c3225-4a47-4e6d-a816-22cac805ba06`):**
  - **Trạng thái cuối:** `Completed`.
  - **Thời gian Worker bắt đầu:** `2026-11-17T13:52:19.684Z`.
  - **Thời gian hoàn tất:** `2026-11-17T13:52:20.040Z`.
  - **Thời gian xử lý ảnh (`durationMs`):** **237 ms**.
  - **Kích thước ảnh sau xử lý:** 400x300 px, dung lượng 7,277 bytes.
  - **Tệp kết quả:** `storage/results/145c32254a474e6da81622cac805ba06.jpg`.

---

### Ca 3: Tạo ảnh thu nhỏ (`JobType: Thumbnail`)
- **Mục tiêu:** Resize ảnh theo tỷ lệ với tham số `width: 150`, `height: 150`.
- **HTTP Request tạo Job:**
  ```http
  POST http://localhost:5227/jobs HTTP/1.1
  Content-Type: application/json

  {
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "jobType": "Thumbnail",
    "parameters": { "width": 150, "height": 150 }
  }
  ```
- **HTTP Response tạo Job:** `HTTP/1.1 202 Accepted`
  ```json
  {
    "jobId": "87487b20-37be-4491-852d-bb3bbb4b17c7",
    "fileId": "c9c56d65-4da9-4553-879b-5bba34d3dd53",
    "jobType": "Thumbnail",
    "status": "Pending",
    "createdAt": "2026-11-17T13:52:59.4090016Z"
  }
  ```
- **Tra cứu kết quả (`GET /jobs/87487b20-37be-4491-852d-bb3bbb4b17c7`):**
  - **Trạng thái cuối:** `Completed`.
  - **Thời gian xử lý ảnh (`durationMs`):** **46 ms**.
  - **Kích thước ảnh sau thu nhỏ:** **150x112 px** (tự động căn chuẩn tỷ lệ gốc 4:3).
  - **Dung lượng sau thu nhỏ:** **1,725 bytes** (giảm đáng kể so với ảnh gốc).
  - **Tệp kết quả:** `storage/results/87487b2037be4491852dbb3bbb4b17c7.jpg`.

---

### Ca 4: Chuyển đổi PNG sang JPEG (`JobType: ConvertPngToJpeg`)
- **Tệp nguồn:** [test_convert.png](file:///D:/XuLyTepBatDongBo/test_convert.png) (Ảnh PNG 800x600 px, dung lượng 6,672 bytes).
- **Tải lên tệp:** Tạo FileId `8d33cb8b-cc6a-4bf7-babe-49026e7e6c47`.
- **HTTP Request tạo Job:**
  ```http
  POST http://localhost:5227/jobs HTTP/1.1
  Content-Type: application/json

  {
    "fileId": "8d33cb8b-cc6a-4bf7-babe-49026e7e6c47",
    "jobType": "ConvertPngToJpeg",
    "parameters": { "quality": 90 }
  }
  ```
- **HTTP Response tạo Job:** `HTTP/1.1 202 Accepted` (JobId: `b0c7efd7-1d07-4d6d-a283-45d13eb7c758`).
- **Tra cứu kết quả (`GET /jobs/b0c7efd7-1d07-4d6d-a283-45d13eb7c758`):**
  - **Trạng thái cuối:** `Completed`.
  - **Thời gian xử lý ảnh (`durationMs`):** **370 ms**.
  - **Kích thước ảnh:** 800x600 px.
  - **Dung lượng JPEG xuất ra:** 37,442 bytes.
  - **Tệp kết quả:** `storage/results/b0c7efd71d074d6da28345d13eb7c758.jpg`.
- **Xác minh nhị phân & Magic Bytes của tệp kết quả tải về:**
  ```powershell
  # Đọc 3 byte đầu tiên và giải mã định dạng ảnh
  Magic Bytes (First 3 bytes): FF-D8-FF
  Is Jpeg Magic (FF-D8-FF)    : True
  Decoded Width               : 800 px
  Decoded Height              : 600 px
  RawFormat is JPEG           : True
  PixelFormat                 : Format24bppRgb
  ```
  *(Khẳng định tệp xuất ra là JPEG chuẩn 100%, không bị lỗi header)*.

---

### Ca 5: Tệp không hợp lệ & Quy trình Retry tự động chuyển `Failed`
- **Mục tiêu:** Xác minh hệ thống khi nhận tệp hỏng (corrupted file) sẽ tự động bắt lỗi, thực hiện cơ chế Retry có dãn cách (Backoff), và chỉ chuyển `Failed` khi cạn số lần thử (`MaxRetries = 3`).
- **Tệp nguồn:** [corrupt_sample.png](file:///D:/XuLyTepBatDongBo/corrupt_sample.png) (Tệp chứa chuỗi text ngẫu nhiên, không phải cấu trúc ảnh hợp lệ).
- **Tải lên tệp:** Tạo FileId `12ff8435-c271-4d0f-b177-d3793dedb226`.
- **HTTP Request tạo Job:** `POST http://localhost:5227/jobs` (JobId: `24546cf0-0bb5-4e8d-8dc0-487bb83af96b`).
- **Diễn tiến Retry được ghi nhận:**
  - **Lần 1 (13:53:40):** Worker giải mã ảnh thất bại $\rightarrow$ `RetryCount = 1`, `Status = Pending`, Outbox tạo message retry với lịch hẹn tương lai (`NextAttemptAt`).
  - **Lần 2 (13:53:50):** Thử lại lần 2 thất bại $\rightarrow$ `RetryCount = 2`.
  - **Lần 3 (13:54:04):** Đạt ngưỡng `MaxRetries = 3` $\rightarrow$ Chuyển trạng thái chính thức sang **`Failed`**.
- **Tra cứu kết quả (`GET /jobs/24546cf0-0bb5-4e8d-8dc0-487bb83af96b`):**
  ```json
  {
    "jobId": "24546cf0-0bb5-4e8d-8dc0-487bb83af96b",
    "fileId": "12ff8435-c271-4d0f-b177-d3793dedb226",
    "originalFileName": "corrupt_sample.png",
    "jobType": "Compress",
    "status": "Failed",
    "retryCount": 3,
    "maxRetries": 3,
    "errorMessage": "Image cannot be loaded. Available decoders: ...",
    "result": null
  }
  ```
- **HTTP Response khi cố gắng tải kết quả (`GET /files/24546cf0-0bb5-4e8d-8dc0-487bb83af96b/result`):**
  - **HTTP Status:** `400 Bad Request`.
  - **Body:** `{"error": "Quá trình xử lý tệp đã thất bại.", "jobId": "24546cf0-0bb5-4e8d-8dc0-487bb83af96b", "status": "Failed", "errorMessage": "..."}`.

---

## 4. Bằng Chứng Đối Chiếu Trực Tiếp trong SQL Server

Dưới đây là dữ liệu thực tế được truy vấn trực tiếp từ Database `XuLyTepBatDongBo`:

### 4.1. Bảng `ProcessingJobs`
| JobId | FileId | JobType | Status | RetryCount | StartedAt | CompletedAt | Duration |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| `b0c7efd7-1d07-4d6d-a283-45d13eb7c758` | `8d33cb8b-...` | `ConvertPngToJpeg` | **`Completed`** | 0 | 14:05:22 | 14:05:23 | 370 ms |
| `87487b20-37be-4491-852d-bb3bbb4b17c7` | `c9c56d65-...` | `Thumbnail` | **`Completed`** | 0 | 13:53:03 | 13:53:03 | 46 ms |
| `145c3225-4a47-4e6d-a816-22cac805ba06` | `c9c56d65-...` | `Compress` | **`Completed`** | 0 | 13:52:19 | 13:52:20 | 237 ms |
| `24546cf0-0bb5-4e8d-8dc0-487bb83af96b` | `12ff8435-...` | `Compress` | **`Failed`** | 3 | 13:54:04 | 13:54:04 | Lỗi giải mã |

### 4.2. Bảng `ProcessingResults`
| ResultId | JobId | RelativePath | FileSize | Dimensions | DurationMs |
| :--- | :--- | :--- | :---: | :---: | :---: |
| `657a90df-8f0f-435c-9b64-df7718f47317` | `b0c7efd7-...` | `results/b0c7efd71d074d6da28345d13eb7c758.jpg` | 37,442 B | 800x600 px | 370 ms |
| `fc67383e-927f-4fb0-b17e-9a1160ce8e62` | `87487b20-...` | `results/87487b2037be4491852dbb3bbb4b17c7.jpg` | 1,725 B | 150x112 px | 46 ms |
| `93e46df1-d796-45aa-a097-a97a3df0ef60` | `145c3225-...` | `results/145c32254a474e6da81622cac805ba06.jpg` | 7,277 B | 400x300 px | 237 ms |

### 4.3. Bảng `OutboxMessages`
| OutboxId | EventType | IdempotencyKey | Status | ScheduledAt | ProcessedAt |
| :--- | :--- | :--- | :---: | :---: | :---: |
| `66f844e5-4c82-46c3-984d-bf9d06ea2ec6` | `ProcessImageJob` | `JOB_b0c7efd7...` | **`Processed`** | 14:05:17 | 14:05:22 |
| `a18ab97a-7052-4f61-b95f-098a32b5cc95` | `ProcessImageJob` | `JOB_87487b20...` | **`Processed`** | 13:53:03 | 13:53:03 |
| `5efd0fb1-3b5c-4dbd-a7c9-199677233a15` | `ProcessImageJob` | `JOB_145c3225...` | **`Processed`** | 13:52:19 | 13:52:19 |
| `a78d9290-bb9d-4453-901e-e2e6c27ccfdc` | `RetryImageJob` | `RETRY_24546cf0_2`| **`Processed`** | 13:54:04 | 13:54:04 |
| `e506150c-1f85-42de-a947-01224ddb5e33` | `RetryImageJob` | `RETRY_24546cf0_1`| **`Processed`** | 13:53:50 | 13:53:50 |

---

## 5. Kết Quả Kiểm Tra Biên Dịch Toàn Solution

Chạy kiểm tra sau cùng:
```powershell
dotnet build AsyncFileProcessing.slnx
```
**Kết quả:**
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Tất cả 4 project (`Core`, `Infrastructure`, `Worker`, `Api`) đều biên dịch sạch, không có cảnh báo nào.

---

## 6. Ghi Nhận Giới Hạn Môi Trường & Cơ Chế Fallback

### 6.1. Thực trạng về RabbitMQ Broker Thật
- Máy tính phát triển hiện tại **chưa được cài đặt Docker Desktop, WSL hay dịch vụ RabbitMQ Windows Service**. Cổng 5672 và 15672 chưa mở.
- **Xác nhận:** Toàn bộ các ca kiểm thử Job thành công ở trên **chưa được chạy qua RabbitMQ Broker thật**, mà chạy qua cơ chế **Outbox Pattern kết hợp In-Memory Channel Fallback (`InMemoryJobQueue`)** được thiết kế trong mã nguồn tầng Infrastructure.

### 6.2. Phân tích Rủi ro & Hạn chế của In-Memory Fallback
1. **Tính bền vững (Durability):** Kênh Fallback sử dụng cấu trúc `System.Threading.Channels.Channel` nằm trong bộ nhớ RAM của tiến trình. Nếu tiến trình Web API hoặc máy chủ bị mất điện/tắt đột ngột khi message đã được đẩy vào Fallback nhưng Worker chưa kịp lưu vào Database, thông điệp đó sẽ bị mất vĩnh viễn trên RAM.
2. **Đánh dấu Processed của Outbox:** Hiện tại, khi `OutboxPublisherService` đẩy message thành công vào `_fallbackQueue.EnqueueAsync`, hàm trả về `true` và Outbox lập tức cập nhật `Status = 'Processed'`. Đây là giải pháp tình thế để ứng dụng chạy được mà không bị vòng lặp gửi lại trong bộ nhớ, **nhưng chưa đạt chuẩn độ tin cậy At-Least-Once cao cấp** như khi sử dụng RabbitMQ với chế độ Publisher Confirms và Durable Queues trên đĩa cứng.

### 6.3. Kiến nghị Cải thiện cho Giai đoạn tiếp theo
1. Khởi chạy container RabbitMQ thật: `docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management`.
2. Khi RabbitMQ khả dụng, `RabbitMqPublisher` sẽ tự động hủy Fallback và gửi qua AMQP.
3. Cập nhật `OutboxPublisherService` để chỉ gán `Status = 'Processed'` khi nhận được xác nhận `BasicAck` từ RabbitMQ Broker thật.

