# BÀI 3 – GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR

## 1. Giới thiệu

Bài thực hành gồm hai hệ thống:

- **PingSignalR:** Minh họa giao tiếp thời gian thực bằng SignalR với các chức năng Echo, Broadcast, Group và quản lý kết nối.
- **RealtimeSubmissionSystem:** Mô phỏng hệ thống tiếp nhận bài nộp, sử dụng REST API kết hợp SignalR để cập nhật trạng thái xử lý theo thời gian thực.

Công nghệ sử dụng:
- C# và .NET 10
- ASP.NET Core Web API
- SignalR
- SignalR Client
- HTTP REST API

## 2. Cấu trúc thư mục

```text
Bai3_SignalR_Complete/
├── PingSignalR/
│   ├── PingSignalR.slnx
│   └── src/
│       ├── Ping.Api/
│       └── Ping.Client/
├── RealtimeSubmissionSystem/
│   ├── RealtimeSubmissionSystem.slnx
│   ├── requests.http
│   └── src/
│       ├── Submission.RealtimeApi/
│       └── Submission.RealtimeClient/
├── .gitignore
└── README.md
```

## 3. Yêu cầu môi trường

- Windows, Linux hoặc macOS.
- .NET SDK 10.
- Visual Studio Code hoặc Visual Studio.
- Extension REST Client trong VS Code (để chạy các request HTTP).

Kiểm tra .NET:

```powershell
dotnet --version
```

## 4. Chạy PingSignalR

Mở terminal thứ nhất tại thư mục gốc dự án:

```powershell
cd PingSignalR
dotnet restore
dotnet build
dotnet run --project src/Ping.Api/Ping.Api.csproj --urls http://localhost:5001
```

API chạy tại:

```text
http://localhost:5001
```

SignalR Hub:

```text
http://localhost:5001/hubs/ping
```

Mở terminal thứ hai:

```powershell
cd PingSignalR
dotnet run --project src/Ping.Client/Ping.Client.csproj
```

Các chức năng được minh họa:

1. **Echo:** Client gửi thông điệp và nhận phản hồi.
2. **Broadcast:** Gửi thông điệp đến tất cả client đang kết nối.
3. **Group:** Tham gia nhóm `demo-room`.
4. **SendToGroup:** Gửi thông điệp đến các thành viên trong nhóm.
5. **Connection management:** Theo dõi kết nối, ngắt kết nối và tự động kết nối lại.

Có thể mở nhiều terminal để chạy nhiều client và quan sát Broadcast, Group.

## 5. Chạy RealtimeSubmissionSystem

Mở terminal thứ nhất:

```powershell
cd RealtimeSubmissionSystem
dotnet restore
dotnet build
dotnet run --project src/Submission.RealtimeApi/Submission.RealtimeApi.csproj --urls http://localhost:5000
```

API chạy tại:

```text
http://localhost:5000
```

Mở terminal thứ hai:

```powershell
cd RealtimeSubmissionSystem
dotnet run --project src/Submission.RealtimeClient/Submission.RealtimeClient.csproj
```

Client thực hiện quy trình:

1. Đăng ký các sự kiện SignalR bằng `On(...)`.
2. Kết nối đến SignalR Hub bằng `StartAsync()`.
3. Gửi yêu cầu tạo bài nộp qua REST API.
4. Theo dõi bài nộp bằng `WatchSubmission(id)`.
5. Nhận các thông báo trạng thái `received`, `processing`, `graded`.
6. Gọi GET để kiểm tra trạng thái cuối cùng.

## 6. Kiểm thử HTTP

Mở file:

```text
RealtimeSubmissionSystem/requests.http
```

Cài extension **REST Client** của Huachao Mao trong VS Code, sau đó chọn `Send Request` để chạy từng yêu cầu.

Các kết quả mong đợi:

| Tình huống | HTTP Status |
|---|---:|
| GET `/` | 200 |
| GET `/api/assignments` | 200 |
| POST bài nộp hợp lệ | 201 |
| GET bài nộp hợp lệ | 200 |
| POST thiếu dữ liệu | 400 |
| POST assignment không tồn tại | 404 |
| GET GUID không tồn tại | 404 |
| SignalR negotiate hợp lệ | 200 |

Lưu ý: Với các request GET cần GUID bài nộp, sử dụng ID thực tế nhận được từ response POST hợp lệ.

## 7. Thí nghiệm phân tán

| Thí nghiệm | Kết quả cần quan sát |
|---|---|
| Client offline khi trạng thái thay đổi | REST vẫn trả về trạng thái mới nhất |
| Không gọi WatchSubmission | Client không nhận thông báo trong group |
| Watch sai ID | Không nhận thông báo của bài nộp khác |
| Ngắt mạng rồi kết nối lại | ConnectionId thay đổi, cần Watch lại và gọi GET |
| Watch sau khi bài nộp đã graded | GET vẫn trả về trạng thái cuối cùng |

SignalR hỗ trợ thông báo thời gian thực nhưng không đảm bảo phát lại các sự kiện đã bỏ lỡ. REST API được sử dụng để truy vấn và đồng bộ trạng thái hiện tại.

## 8. Kiểm tra build

Tại thư mục gốc:

```powershell
dotnet build .\PingSignalR\PingSignalR.slnx
dotnet build .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx
```

Cả hai solution cần hiển thị:

```text
Build succeeded.
```

## 9. Ghi chú

Đây là bài thực hành mô phỏng giao tiếp thời gian thực và xử lý bài nộp. Dữ liệu được sử dụng trong bộ nhớ và thời gian xử lý được mô phỏng bằng `Task.Delay`, không phải hệ thống production.

Khi triển khai thực tế cần bổ sung cơ sở dữ liệu, hàng đợi xử lý, xác thực và phân quyền người dùng.
