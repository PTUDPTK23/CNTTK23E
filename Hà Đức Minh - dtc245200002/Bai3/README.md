# BÀI 3 – GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR

**Họ và tên:** Hà Đức Minh  
**MSSV:** DTC245200002
**Môn học:** Phát triển ứng dụng phân tán  
**Bài thực hành:** Bài 3 – SignalR

---

## 1. Giới thiệu

Bài thực hành xây dựng hai ứng dụng sử dụng SignalR trong ASP.NET Core:

- **PingSignalR:** Thực hành giao tiếp thời gian thực giữa máy chủ và máy khách.
- **RealtimeSubmissionSystem:** Xây dựng hệ thống nộp bài, theo dõi trạng thái chấm bài bằng REST API kết hợp SignalR.

Mục tiêu là hiểu cách máy chủ chủ động gửi thông báo đến máy khách, cách sử dụng Hub, Group, IHubContext và cách kết hợp REST với SignalR.

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
    └── src/
        ├── Submission.RealtimeApi/
        └── Submission.RealtimeClient/
```

## 3. Nội dung thực hiện

### 3.1. PingSignalR

Chức năng đã xây dựng:

- Echo: Máy khách gửi thông điệp, máy chủ phản hồi lại đúng máy khách.
- Broadcast: Máy chủ gửi thông điệp đến tất cả máy khách.
- Group: Máy khách tham gia nhóm và nhận thông điệp dành cho nhóm.
- SendToGroup: Gửi thông điệp đến một nhóm cụ thể.
- Quản lý vòng đời kết nối: xử lý kết nối và ngắt kết nối.
- Tự kết nối lại và tham gia lại nhóm sau khi kết nối được khôi phục.

### 3.2. RealtimeSubmissionSystem

Ứng dụng gồm hai thành phần:

**Realtime API:**

- `GET /api/assignments`: Lấy danh sách bài tập.
- `POST /api/assignments/{assignmentId}/submissions`: Tạo lượt nộp bài.
- `GET /api/submissions/{id}`: Đọc trạng thái lượt nộp.
- `/hubs/submissions`: SignalR Hub theo dõi trạng thái lượt nộp.
- Nhóm SignalR có dạng `submission:{id}`.

**Realtime Client:**

- Kết nối đến SignalR Hub.
- Đăng ký sự kiện `SubmissionStatusChanged`.
- Lấy danh sách bài tập.
- Tạo lượt nộp bài.
- Theo dõi trạng thái `processing` và `graded`.
- Gọi REST API để đọc lại trạng thái cuối cùng.

### 3.3. Luồng xử lý

1. Client đăng ký lắng nghe sự kiện SignalR.
2. Client kết nối đến Hub.
3. Client gọi REST API để lấy danh sách bài tập.
4. Client gửi yêu cầu tạo lượt nộp.
5. Server trả về lượt nộp có trạng thái `received`.
6. Client gọi `WatchSubmission` để tham gia nhóm tương ứng.
7. Server gửi thông báo `processing`, sau đó `graded`.
8. Client gọi REST API để xác nhận trạng thái mới nhất.

**REST dùng để tạo và đọc tài nguyên; SignalR dùng để đẩy thông báo thay đổi trạng thái. REST là nguồn sự thật khi cần xác nhận kết quả.**

## 4. Kết quả chạy chương trình

### 4.1. Kết quả PingSignalR

Đã chạy thử ứng dụng PingSignalR và kiểm tra giao tiếp giữa máy chủ với máy khách, gồm Echo, Broadcast và Group.

### 4.2. Kết quả RealtimeSubmissionSystem

Kết quả chạy máy khách:

```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: 2bafe0f5-650a-46ac-9336-737e6b7be270
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 2bafe0f5-650a-46ac-9336-737e6b7be270 -> processing
Thong bao SignalR: 2bafe0f5-650a-46ac-9336-737e6b7be270 -> graded
Trang thai doc lai bang REST: graded
```

Kết quả: Client nhận được hai thông báo SignalR và đọc lại trạng thái cuối bằng REST thành công.

## 5. Kiểm thử requests.http

Đã thực hiện kiểm thử 9 trường hợp:

| STT | Nội dung kiểm thử | Mã HTTP mong đợi |
|---|---|---:|
| 1 | GET trang chủ `/` | 200 |
| 2 | GET danh sách bài tập | 200 |
| 3 | POST tạo lượt nộp hợp lệ | 201 |
| 4 | GET đọc lượt nộp | 200 |
| 5 | POST thiếu dữ liệu | 400 |
| 6 | POST bài tập không tồn tại | 404 |
| 7 | GET ID không tồn tại | 404 |
| 8 | SignalR negotiate đúng đường dẫn | 200 |
| 9 | SignalR negotiate sai đường dẫn | 404 |

Kết quả kiểm thử: Các trường hợp đã thực hiện trả về đúng mã HTTP mong đợi.

## 6. Năm thí nghiệm

Các thí nghiệm dưới đây cần được chạy và đối chiếu với kết quả thực tế trên máy.

| STT | Thí nghiệm | Dự đoán | Kết quả theo tài liệu và điều cần xác nhận |
|---|---|---|---|
| 1 | Chạy hai máy khách SV001 và SV002 cùng lúc | Mỗi máy khách chỉ nhận thông báo của lượt nộp mình theo dõi. | Kết quả dự kiến: mỗi máy chỉ thấy lượt nộp của mình. Xác nhận bằng cách chạy đồng thời hai client. |
| 2 | Chạy Client khi Server chưa hoạt động | Client không thể kết nối đến máy chủ. | Kết quả dự kiến: `HttpRequestException: Connection refused (localhost:5000)`. |
| 3 | Sửa đường dẫn Hub thành `/hub/submissions` | Kết nối SignalR thất bại vì đường dẫn không khớp. | Kết quả dự kiến: lỗi `404 Not Found` tại bước negotiate. |
| 4 | Đổi tên sự kiện thành `SubmissionStatusChange` | Client không nhận được thông báo vì tên sự kiện không khớp. | Kết quả dự kiến: không báo lỗi ngay, sau 10 giây hết thời gian chờ. |
| 5 | Chờ 4 giây rồi mới gọi `WatchSubmission` | Client có thể bỏ lỡ các thông báo đã được gửi trước khi tham gia nhóm. | Kết quả dự kiến: không nhận được thông báo, nhưng REST vẫn đọc được trạng thái `graded`. |

**Bài học rút ra:**

- Group giúp gửi thông báo đúng đối tượng.
- Đường dẫn Hub phải khớp giữa Client và Server.
- Tên sự kiện ở `SendAsync` và `On` phải giống nhau.
- Nếu tham gia nhóm muộn, máy khách có thể bỏ lỡ thông báo.
- REST cần được sử dụng để xác nhận trạng thái mới nhất.

*Lưu ý: Cần ghi lại kết quả chạy thực tế và ảnh minh chứng cho từng thí nghiệm trước khi nộp.*

## 7. Trả lời ba câu hỏi đầu giờ

### Câu 1. Máy chủ có thể chủ động báo cho máy khách không?

Có. Máy khách cần mở sẵn một kết nối lâu dài thông qua WebSocket và SignalR. Khi có sự kiện, máy chủ có thể gửi thông báo đến máy khách bằng `Clients.Group(...).SendAsync`. Nếu gửi từ bên ngoài Hub thì sử dụng `IHubContext`.

### Câu 2. REST và SignalR chia việc như thế nào?

REST dùng để tạo và đọc tài nguyên, đồng thời trả về mã trạng thái rõ ràng. SignalR dùng để đẩy các thay đổi phát sinh sau yêu cầu ban đầu đến máy khách. SignalR không thay thế REST mà bổ sung khả năng giao tiếp thời gian thực.

### Câu 3. Nếu đứt mạng đúng lúc máy chủ báo graded thì sao?

Thông báo có thể bị mất. `WithAutomaticReconnect` giúp kết nối lại, nhưng ConnectionId sẽ thay đổi. Máy khách cần tham gia lại nhóm bằng `WatchSubmission` và gọi `GET /api/submissions/{id}` để đọc trạng thái cuối cùng từ máy chủ.

**Kết luận:** Push để nhanh, REST để chắc.

## 8. Cách chạy chương trình

### 8.1. Chạy PingSignalR

Mở Terminal tại thư mục `Bai3`:

```powershell
cd .\PingSignalR
dotnet build
```

Chạy API và Client theo cấu hình cổng trong mã nguồn.

### 8.2. Chạy RealtimeSubmissionSystem

Terminal 1 – chạy API:

```powershell
cd C:\Users\Admin\Bai3\RealtimeSubmissionSystem
dotnet run --project .\src\Submission.RealtimeApi
```

Terminal 2 – chạy Client:

```powershell
cd C:\Users\Admin\Bai3\RealtimeSubmissionSystem
dotnet run --project .\src\Submission.RealtimeClient
```

### 8.3. Build hai solution

```powershell
cd C:\Users\Admin\Bai3\PingSignalR
dotnet build

cd ..\RealtimeSubmissionSystem
dotnet build
```

Yêu cầu: cả hai solution build thành công, không có lỗi.

## 9. Kết luận

Qua bài thực hành, đã xây dựng được ứng dụng giao tiếp thời gian thực bằng SignalR, sử dụng Hub, Group, IHubContext và kết hợp SignalR với REST API.

Ứng dụng RealtimeSubmissionSystem minh họa quá trình tạo lượt nộp bài, nhận thông báo trạng thái `processing`, `graded` và đọc lại kết quả bằng REST.

Bài thực hành giúp hiểu rõ vai trò của REST và SignalR trong ứng dụng phân tán.
