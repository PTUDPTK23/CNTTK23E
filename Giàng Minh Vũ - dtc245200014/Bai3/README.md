# BÀI 3 – SIGNALR: EDUSUBMIT NHẬN THÔNG BÁO TRẠNG THÁI THỜI GIAN THỰC

## 1. Thông tin sinh viên

- **Họ và tên:** Giàng Minh Vũ
- **Mã sinh viên:** dtc245200014
- **Lớp:** CNTTK23E
- **Bài thực hành:** Bài 3 – SignalR
- **Chủ đề:** EduSubmit nhận thông báo trạng thái thời gian thực.

## 2. Cấu trúc thư mục

```text
Bai3/
├── README.md
├── PingSignalR/
│   ├── PingSignalR.slnx
│   └── src/
│       ├── Ping.Api/
│       └── Ping.Client/
└── RealtimeSubmissionSystem/
    ├── RealtimeSubmissionSystem.slnx
    ├── requests.http
    ├── request.json
    ├── request-invalid.json
    └── src/
        ├── Submission.RealtimeApi/
        └── Submission.RealtimeClient/
```

## 3. Nội dung thực hiện

### 3.1. PingSignalR

- Xây dựng ứng dụng Ping API và Ping Client.
- Tích hợp SignalR để thiết lập kết nối giữa máy chủ và máy khách.
- Kiểm tra khả năng gửi và nhận thông điệp Echo.

### 3.2. RealtimeSubmissionSystem – EduSubmit

- Xây dựng REST API quản lý danh sách bài tập và lượt nộp bài.
- Tạo SignalR Hub tại `/hubs/submissions`.
- Sử dụng Group theo định dạng `submission:{id}` để theo dõi từng lượt nộp.
- Sử dụng `IHubContext<SubmissionHub>` để gửi thông báo từ bên ngoài Hub.
- Mô phỏng quá trình chấm bài với các trạng thái:
  - `received`: Đã tiếp nhận bài nộp.
  - `processing`: Đang xử lý bài nộp.
  - `graded`: Đã chấm bài.
- Xây dựng máy khách SignalR nhận thông báo trạng thái theo thời gian thực.
- Sử dụng REST API để đọc lại trạng thái cuối cùng của lượt nộp.

## 4. Kết quả chạy chương trình

### 4.1. Kết quả Build

Thực hiện lệnh:

```powershell
dotnet build
```

Kết quả: Build thành công, cả hai project trong solution RealtimeSubmissionSystem đều thành công, không có lỗi.

Solution PingSignalR cũng đã được kiểm tra Build thành công.

### 4.2. Kết quả chạy EduSubmit Client

Máy khách kết nối SignalR, tạo lượt nộp bài, nhận thông báo trạng thái và đọc lại trạng thái cuối cùng bằng REST API.

Kết quả chạy thực tế:

```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: [GUID]
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: [GUID] -> processing
Thong bao SignalR: [GUID] -> graded
Trang thai doc lai bang REST: graded
```

Kết quả: Máy khách nhận được hai thông báo `processing` và `graded`. REST API xác nhận trạng thái cuối cùng là `graded`.

### 4.3. Kết quả chạy PingSignalR

- Máy khách kết nối tới Ping API.
- Máy chủ nhận được thông điệp từ Client.
- Kiểm tra phản hồi Echo giữa máy chủ và máy khách.

## 5. Kết quả 5 thí nghiệm SignalR

| Thí nghiệm                                        | Dự đoán                                                      | Kết quả thực tế                                                            | Giải thích                                                                                  |
| ------------------------------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| 1. Chạy hai máy khách SV001 và SV002 cùng lúc     | Mỗi máy khách chỉ nhận thông báo của lượt nộp mình theo dõi. | Mỗi máy khách chỉ thấy lượt nộp vừa tạo.                                   | Group `submission:{id}` giúp phân tách người nhận thông báo.                                |
| 2. Chạy máy khách khi máy chủ chưa chạy           | Máy khách không thể kết nối tới máy chủ.                     | Xuất hiện `HttpRequestException: Connection refused (localhost:5000)`.     | Lần kết nối đầu tiên không được tự động thử lại.                                            |
| 3. Sửa đường dẫn Hub thành `/hub/submissions`     | Kết nối SignalR thất bại.                                    | Xuất hiện lỗi HTTP 404 Not Found tại bước negotiate.                       | Đường dẫn máy khách phải khớp với `MapHub` của máy chủ.                                     |
| 4. Sửa tên sự kiện thành `SubmissionStatusChange` | Máy khách không nhận được thông báo.                         | Không báo lỗi, hết 10 giây xuất hiện thông báo quá thời gian chờ `graded`. | Tên sự kiện trong `On` phải trùng với tên trong `SendAsync`.                                |
| 5. Chờ 4 giây rồi mới gọi `WatchSubmission`       | Máy khách có thể bỏ lỡ thông báo trạng thái.                 | Không nhận được thông báo SignalR; REST đọc lại trạng thái `graded`.       | Tham gia Group muộn sẽ bỏ lỡ tin đã gửi. REST là nguồn sự thật để đọc trạng thái cuối cùng. |

**Kết luận thí nghiệm:**

Thông điệp thời gian thực có thể bị bỏ lỡ khi máy khách chưa kết nối hoặc chưa tham gia đúng Group. Vì vậy, cần kết hợp SignalR để nhận thông báo nhanh và REST API để xác nhận trạng thái chính xác.

## 6. Kiểm tra REST API và SignalR bằng requests.http

File `requests.http` gồm 9 yêu cầu kiểm tra:

1. GET API root – 200.
2. GET danh sách bài tập – 200.
3. POST tạo lượt nộp hợp lệ – 201.
4. GET thông tin lượt nộp – 200.
5. POST thiếu dữ liệu – 400.
6. POST bài tập không tồn tại – 404.
7. GET GUID không tồn tại – 404.
8. SignalR negotiate đúng đường dẫn – 200.
9. SignalR negotiate sai đường dẫn – 404.

Các mã trạng thái được kỳ vọng theo yêu cầu bài thực hành.

## 7. Trả lời 3 câu hỏi đầu giờ

### Câu 1. Máy chủ có thể chủ động gửi thông báo cho máy khách không?

Có. Máy chủ có thể chủ động gửi thông báo nếu máy khách đã mở sẵn một kết nối lâu dài thông qua SignalR.

Máy chủ sử dụng:

```csharp
Clients.Group(...).SendAsync(...)
```

Khi gửi thông báo từ bên ngoài Hub, sử dụng `IHubContext`.

### Câu 2. REST và SignalR chia việc như thế nào?

- REST dùng để tạo và đọc tài nguyên, trả về mã trạng thái rõ ràng.
- SignalR dùng để đẩy các thay đổi phát sinh sau yêu cầu ban đầu tới máy khách theo thời gian thực.

SignalR không thay thế REST. REST dùng để tạo và đọc dữ liệu, SignalR dùng để thông báo những thay đổi.

### Câu 3. Nếu mất kết nối đúng lúc máy chủ gửi thông báo `graded` thì sao?

Thông báo SignalR có thể bị mất khi máy khách mất kết nối.

`WithAutomaticReconnect()` hỗ trợ kết nối lại, nhưng sau khi kết nối lại, máy khách có ConnectionId mới. Vì vậy, máy khách cần gọi lại `WatchSubmission` để tham gia Group và gọi:

```text
GET /api/submissions/{id}
```

để lấy trạng thái cuối cùng của lượt nộp.

**Kết luận:** Push để nhanh, REST để chắc.

## 8. Quy trình chạy chương trình

### 8.1. Chạy RealtimeSubmissionSystem

Terminal 1 – Khởi chạy API:

```powershell
dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000
```

Terminal 2 – Khởi chạy Client:

```powershell
dotnet run --project src/Submission.RealtimeClient
```

### 8.2. Chạy PingSignalR

Mở Terminal tại thư mục PingSignalR.

Khởi chạy API và Client theo cấu hình cổng của bài thực hành:

```powershell
dotnet run --project src/Ping.Api --urls http://localhost:5200
```

```powershell
dotnet run --project src/Ping.Client
```

## 9. Kết luận

Qua bài thực hành, em đã xây dựng được hai ứng dụng PingSignalR và RealtimeSubmissionSystem, tích hợp SignalR để gửi thông báo trạng thái theo thời gian thực.

Em hiểu được cách kết hợp REST API và SignalR, cách sử dụng Hub, Group, IHubContext, cách xử lý kết nối lại và cách đọc lại trạng thái bằng REST khi thông báo thời gian thực bị bỏ lỡ.

Bài thực hành giúp em hiểu rõ hơn về giao tiếp thời gian thực trong ứng dụng phân tán.
