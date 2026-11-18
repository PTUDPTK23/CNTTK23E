# Giải thích Bài 3 — SignalR

Tài liệu này diễn giải nội dung bài giảng theo chủ đề và chỉ ra phần code tương ứng trong project Bài 3.

## 1. Ba mô hình giao tiếp realtime — MEP (trang 2–12)

- **Polling:** máy khách gọi máy chủ lặp lại để hỏi có dữ liệu mới chưa. Dễ triển khai nhưng tạo nhiều request thừa.
- **Push:** máy chủ chủ động gửi thông báo khi trạng thái thay đổi. Đây là cách EduSubmit báo tiến độ chấm bài.
- **Streaming:** giữ một luồng mở để trao đổi nhiều dữ liệu liên tục.

WebSocket cung cấp kênh hai chiều trên một kết nối. SignalR xây thêm lớp trừu tượng Hub, sự kiện và nhóm người nhận; SignalR có thể thương lượng transport phù hợp như WebSocket, Server-Sent Events hoặc Long Polling.

## 2. Kết nối, Hub và Group (trang 13–19)

- **Hub** là nơi khai báo các phương thức máy khách có thể gọi và cách máy chủ gửi sự kiện.
- **ConnectionId** định danh một kết nối hiện tại. Nó có thể đổi khi kết nối lại.
- **Clients.Caller / All / Group / Client(id)** gửi sự kiện cho người gọi, mọi kết nối, một nhóm hoặc một kết nối cụ thể.
- **Group** gom các kết nối quan tâm cùng một đối tượng, chẳng hạn `submission:{id}`. Máy chủ thêm kết nối bằng `Groups.AddToGroupAsync`.
- **Negotiate** là bước khởi tạo kết nối SignalR; gọi trực tiếp URL Hub như một API GET không thay thế cho bước này.

Hub nên xử lý giao tiếp. Khi công việc REST hoặc một service phía sau cần phát thông báo, dùng `IHubContext<Hub>` thay vì tạo Hub thủ công.

## 3. Bài thực hành PingSignalR (trang 20–41)

1. Tạo solution gồm API và console client; client thêm package `Microsoft.AspNetCore.SignalR.Client`.
2. API gọi `AddSignalR()` và `MapHub<PingHub>("/hubs/ping")`.
3. `PingHub` minh họa `Echo`, `Broadcast`, `JoinGroup`, `SendToGroup` cùng `OnConnectedAsync` và `OnDisconnectedAsync`.
4. Client đăng ký `connection.On<T>(...)` để nhận sự kiện, gọi `StartAsync()`, rồi dùng `InvokeAsync(...)` để gọi phương thức Hub.
5. `WithAutomaticReconnect()` và các callback `Reconnecting`, `Reconnected`, `Closed` minh họa vòng đời kết nối.

`InvokeAsync` dùng khi cần gọi phương thức Hub và có thể chờ lỗi/trả về. `SendAsync` dùng để gửi sự kiện từ máy chủ tới máy khách. Tên sự kiện trong `On<T>` phải trùng với tên trong `SendAsync`.

Trong project, ví dụ này nằm ở `PingHub` trong API và chế độ `ping` trong `EduSubmit.Client`.

## 4. Bài tập EduSubmit: REST + SignalR (trang 42–63)

REST xử lý thao tác và là nguồn để đọc trạng thái; SignalR đẩy thông báo trạng thái mới. Trình tự trong bài giảng:

1. Client lấy bài tập qua `GET /api/assignments`.
2. Client tạo lượt nộp bằng `POST /api/assignments/{assignmentId}/submissions`; server trả `201 Created` cùng ID và trạng thái ban đầu `received`.
3. Client kết nối `/hubs/submissions` và gọi `WatchSubmission(id)` để vào group `submission:{id}`.
4. Tác vụ chấm mô phỏng đổi trạng thái `received` → `processing` → `graded`; nhánh lỗi minh họa chuyển thành `failed`.
5. `IHubContext<SubmissionHub>` gửi `SubmissionStatusChanged` đúng group của lượt nộp đó.
6. Client nhận thông báo SignalR rồi đọc lại `GET /api/submissions/{id}` để lấy trạng thái chính thức.

Các DTO trong bài gồm `AssignmentDto`, `CreateSubmissionRequest`, `SubmissionDto`, `SubmissionStatusMessage` và `ApiError`. Bản demo lưu dữ liệu trong bộ nhớ để tập trung vào REST và SignalR.

## 5. Lỗi thường gặp và tiêu chí kiểm tra (trang 64–72)

- `HubConnectionBuilder` không tìm thấy: thiếu package SignalR Client ở project client.
- Kết nối bị từ chối: API chưa chạy hoặc client dùng sai port.
- HTTP 404 khi kết nối Hub: sai route đã truyền vào `MapHub`.
- `HubException` báo method không tồn tại: tên trong `InvokeAsync` không khớp với tên phương thức Hub.
- Không nhận được event: tên trong `On<T>` và `SendAsync` phải giống nhau; client phải `StartAsync` và vào đúng group.

Checklist cuối bài yêu cầu kiểm tra: `GET /api/assignments` trả 200; POST hợp lệ trả 201 và `Location`; dữ liệu sai trả 400; assignment không tồn tại trả 404; negotiate `/hubs/submissions` trả 200; GET theo Location sau khi chấm trả 200 với `graded`; và negotiate `/hubs/ping` trên port 5200 trả 200. Bài cũng yêu cầu chạy hai máy khách để quan sát cập nhật độc lập.

## 6. Ảnh minh chứng được nêu trong bài giảng

Trang 22–23 liệt kê ảnh theo từng bước. Ảnh này là minh chứng đầu ra chạy thật, không phải ảnh minh họa:

| Tên trong bài | Nội dung cần thấy |
|---|---|
| `buoc1-build.png` | `dotnet build` thành công, không có lỗi |
| `buoc2-negotiate.png` | Phản hồi negotiate thành công cho Hub |
| `buoc4-echo.png` | Client nhận `EchoResponse` |
| `buoc5-nhom.png` | Hai client nhận thông báo group/broadcast |
| `buoc6-noilai.png` | Client mất rồi khôi phục kết nối SignalR |
| `buoc7-loi1.png` – `buoc7-loi3.png` | Ba tình huống lỗi: sai Hub route (404), payload sai (400), assignment không tồn tại (404) |
| `edusubmit-requests.png` | `GET` 200, POST hợp lệ 201, payload sai 400, assignment không tồn tại 404 |
| `edusubmit-graded.png` | Client nhận `processing`/`graded` qua SignalR và đọc lại REST |

Ảnh PNG trong `images/` là bản trình bày log và phản hồi HTTP từ lần chạy thực tế, không phải ảnh chụp màn hình terminal. Các ảnh minh họa khái niệm dạng SVG nằm cùng thư mục.
