# BÀI 3 – GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR

**Bộ bài thực hành dành cho Visual Studio Code – .NET 10**

Bộ bài gồm đúng 2 solution theo cấu trúc trong tài liệu:
- `PingSignalR`: luyện Hub, `Echo`, `Broadcast`, `Group`, vòng đời kết nối và tự kết nối lại.
- `RealtimeSubmissionSystem`: REST tạo/đọc lượt nộp + SignalR đẩy `processing` và `graded` tới đúng nhóm `submission:{id}`.

> Cổng cố định: Ping API `5200`; EduSubmit API `5000`. Máy khách nhận URL qua tham số dòng lệnh. Không đưa `bin/`, `obj/` vào Git.

## 1. Yêu cầu môi trường

- Visual Studio Code
- .NET SDK 10.0.x
- Extension **C# Dev Kit** (khuyến nghị)
- Internet ở lần restore NuGet đầu tiên

Kiểm tra trong Terminal của VS Code:

```powershell
dotnet --version
dotnet --list-sdks
```

## 2. Mở đúng thư mục

Giải nén ZIP, mở thư mục `Bai3_SignalR_ThucHanh` bằng VS Code (`File > Open Folder`).

Mở Terminal > New Terminal, chạy:

```powershell
dotnet restore .\PingSignalR\PingSignalR.slnx
dotnet build .\PingSignalR\PingSignalR.slnx
dotnet restore .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx
dotnet build .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx
```

Kết quả mong đợi: `Build succeeded`, `0 Error(s)` ở cả hai solution.

## 3. Phần A – PingSignalR

### Terminal 1: chạy máy chủ

```powershell
dotnet run --project .\PingSignalR\src\Ping.Api --urls http://localhost:5200
```

Giữ terminal này mở. Kiểm tra bằng terminal khác:

```powershell
curl.exe http://localhost:5200/
curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
```

Lệnh negotiate phải trả JSON có `connectionId` và `availableTransports`.

### Terminal 2: chạy máy khách An

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 An
```

### Terminal 3: chạy máy khách Bình

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 Binh
```

Kết quả cần quan sát:
- Mỗi máy khách nhận `[Echo]` của chính mình (Caller).
- Cả An và Bình nhận `[Group]` khi đã tham gia `lop-hoc-01`.
- Cả An và Bình nhận `[Broadcast]` (All).
- An không nhận `[Echo]` của Bình.
- Nhấn Enter ở từng máy khách để thoát.

### Thử tự kết nối lại

Khi hai client đang chờ, dừng API bằng `Ctrl+C`, sau đó chạy lại lệnh máy chủ. Client sẽ in trạng thái reconnect/reconnected. Lưu ý ConnectionId thay đổi; membership của group cũ không được bảo đảm tự phục hồi.

## 4. Phần B – RealtimeSubmissionSystem (EduSubmit)

### Terminal 1: chạy API

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeApi --urls http://localhost:5000
```

### Terminal 2: chạy máy khách

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001
```

Máy khách sẽ:
1. Mở SignalR và đăng ký `On<SubmissionStatusMessage>`.
2. Gọi REST lấy danh sách bài tập.
3. POST tạo lượt nộp (201, trạng thái `received`).
4. Gọi `WatchSubmission(id)` để vào nhóm riêng.
5. Nhận push `processing`, rồi `graded`.
6. GET REST lần cuối để xác nhận trạng thái mới nhất.

Kết quả chính cần thấy:

```text
Da ket noi SignalR.
Da tao luot nop: <GUID>
Trang thai REST ban dau: received
Thong bao SignalR: <GUID> -> processing
Thong bao SignalR: <GUID> -> graded
Trang thai doc lai bang REST: graded
```

Mỗi lượt nộp có một nhóm riêng `submission:{id}`. Không phát trạng thái bài của sinh viên này cho sinh viên khác.

## 5. Kiểm tra 9 ca bằng requests.http

Mở `RealtimeSubmissionSystem/requests.http` bằng VS Code. Cài extension **REST Client** (humao.rest-client), sau đó bấm **Send Request** phía trên từng request.

Mã mong đợi:
1. `GET /` → 200
2. `GET /api/assignments` → 200
3. POST hợp lệ → 201
4. GET theo ID vừa tạo → 200 (chạy lại sau vài giây sẽ thấy `graded`)
5. POST thiếu trường → 400
6. POST assignment không tồn tại → 404
7. GET ID không tồn tại → 404
8. POST negotiate đúng hub → 200 và có `connectionId`
9. POST negotiate sai đường dẫn `/hub/submissions` → 404

## 6. Thí nghiệm bắt buộc và câu trả lời

| Thí nghiệm | Kết quả mong đợi | Giải thích |
|---|---|---|
| Hai client theo dõi hai lượt khác nhau | Mỗi client chỉ nhận thông báo của ID mình | Group `submission:{id}` cô lập người nhận |
| Chạy client khi API tắt | `Connection refused` | Kết nối ban đầu thất bại; automatic reconnect chủ yếu xử lý kết nối đã mở |
| Sai `/hub/submissions` | 404 | Không khớp `MapHub("/hubs/submissions")` |
| Sai tên `SubmissionStatusChanged` ở client | Không có lỗi rõ ràng, hết thời gian chờ | Tên sự kiện chiều xuống không khớp |
| Gọi Watch muộn sau khi đã graded | Không nhận push cũ; REST vẫn đọc `graded` | SignalR không lưu lịch sử thông điệp; REST là nguồn sự thật |

**Thứ tự chuẩn:** `On(...)` → `StartAsync()` → REST `POST` → `Watch(id)` → REST `GET`.

### Trả lời 3 câu hỏi đầu giờ

1. **Máy chủ có thể chủ động báo không?** Có. Máy khách mở kết nối lâu dài bằng SignalR; máy chủ gửi qua `Clients.Group(...).SendAsync(...)`. Ở ngoài Hub dùng `IHubContext`.
2. **REST và SignalR chia việc thế nào?** REST tạo và đọc tài nguyên, trả mã HTTP rõ ràng. SignalR đẩy thay đổi phát sinh tới client đang theo dõi. SignalR không thay REST.
3. **Đứt mạng đúng lúc báo graded thì sao?** Có thể lỡ thông điệp. Kết nối lại, gọi Watch lại và GET `/api/submissions/{id}` để lấy trạng thái cuối.

## 7. Ảnh minh chứng và README nộp bài

Tài liệu yêu cầu chụp ảnh lệnh và kết quả trong cùng một ảnh. Lưu ảnh vào `anh/` (thư mục đã tạo sẵn) với tên:
- `buoc1-build.png`
- `buoc2-negotiate.png`
- `buoc4-echo.png`
- `buoc5-nhom.png`
- `buoc6-noilai.png`
- `buoc7-loi1.png` đến `buoc7-loi3.png`
- `edusubmit-client.png`
- `edusubmit-requests.png`

Có thể dùng `Win + Shift + S`. Sau khi chụp, cập nhật README này bằng kết quả thực tế, không dùng ảnh minh họa giả.

## 8. Git – gợi ý commit theo từng bước

Chạy từ thư mục gốc của bộ bài:

```powershell
git init
git add .
git commit -m "Bai3 B1: tao hai solution"
git add .
git commit -m "Bai3 B2: hoan thanh PingSignalR hub va client"
git add .
git commit -m "Bai3 B3: hoan thanh EduSubmit realtime"
git add .
git commit -m "Bai3: bo sung requests va README minh chung"
```

Nếu đang làm trong repo lớp, hãy chép đúng thư mục `Bai3` vào thư mục cá nhân `<Ho ten - MSSV>/Bai3` theo yêu cầu lớp. Tên PR theo slide: `Bai3 - MSSV - Họ tên`.

## 9. Lỗi thường gặp

- `NU1301`: kiểm tra mạng/proxy rồi chạy `dotnet restore`.
- `Connection refused`: API chưa chạy hoặc sai cổng.
- `404` khi SignalR connect: kiểm tra `/hubs/ping` hoặc `/hubs/submissions`.
- `HubException Method does not exist`: tên `InvokeAsync` không khớp phương thức Hub.
- Không nhận thông báo: kiểm tra tên `SendAsync`/`On`, và gọi `WatchSubmission` sau POST.
- Sửa API nhưng chạy code cũ: `Ctrl+C` tiến trình API rồi chạy lại.
- Cổng đã được sử dụng: đóng tiến trình đang giữ cổng hoặc đổi cổng đồng thời ở API và client (bài nộp chính thức nên dùng 5200/5000).

## 10. Lưu ý phạm vi

Đây là bộ code thực hành độc lập, dựng lại theo yêu cầu và luồng trong slide Bài 3. Phần REST của EduSubmit được cung cấp hoàn chỉnh để chạy độc lập, thay vì phụ thuộc source Bài 2 của từng sinh viên. Dữ liệu lưu trong bộ nhớ nên sẽ mất khi khởi động lại API; phù hợp mô phỏng thực hành, không phải triển khai production.
