# Bài 3 — SignalR

Lê Hải Anh — dtc245200851

## Bước 1 — Tạo solution và build

![Build](anh/buoc1-build.png)

Hai project Ping.Api và Ping.Client đã build thành công. Gói SignalR Client được cố định ở 10.0.15 theo slide 25.

## Bước 2 — Negotiate

PingApi chạy cổng 5200, hub tại /hubs/ping. POST negotiate cấp connectionId và công bố WebSockets, ServerSentEvents, LongPolling.
Ảnh negotiate cũ dùng cổng 5000 và /ping. Cần chụp lại theo lệnh cuối tài liệu sau khi sửa đúng slide.

![Negotiate](anh/buoc2-negotiate.png)


## Chạy và chụp minh chứng

Trong terminal VS Code ở CNTTK23E, chuyển vào Bai3 trước. Mỗi terminal mới chạy lại lệnh cd này.

```powershell
cd ".\Lê Hải Anh - dtc245200851\Bai3"
```

Chụp cả lệnh và kết quả, để prompt thấy Bai3. Lưu ảnh vào anh/. Các ảnh chưa có được ghi trong bảng dưới, cần chụp bằng kết quả chạy thật trước khi nộp.

| Nội dung | Tên ảnh | Cần thấy |
|---|---|---|
| Build hai solution | buoc1-build.png, edusubmit-build.png | 0 Error(s) |
| Bước 2 | buoc2-negotiate.png | connectionId, availableTransports |
| Bước 3–4 | buoc4-echo.png | Server đang chạy và client nhận Echo |
| Bước 5 | buoc5-nhom.png | An thấy Group và Broadcast của Bình, Echo riêng mỗi người |
| Bước 6 | buoc6-noilai.png | Đang kết nối lại rồi đã kết nối lại, ConnectionId đổi |
| Bước 7 | buoc7-loi1.png, buoc7-loi2.png, buoc7-loi3.png | Lỗi nối đầu, 404 sai hub, sai sự kiện im lặng |
| EduSubmit | edusubmit-client.png | processing, graded và REST cuối graded |
| REST | edusubmit-requests.png | 9 request trong requests.http |

Build từng solution, chụp kết quả.

```powershell
dotnet build .\PingSignalR\PingSignalR.slnx --tl:off
dotnet build .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx --tl:off
```

### Bước 2 — Bắt tay

Terminal máy chủ Ping, giữ chạy.

```powershell
dotnet run --project .\PingSignalR\src\Ping.Api --urls http://localhost:5200
```

Terminal khác, chạy các lệnh sau và chụp negotiate.

```powershell
curl.exe http://localhost:5200/
curl.exe -i -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
curl.exe -i http://localhost:5200/hubs/ping
```

GET trực tiếp hub trả 400 Connection ID required theo slide 27.

### Bước 3–4 — Echo

Giữ server chạy, mở terminal khác và chạy An. Chụp dòng Echo.

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 An
```

Echo gọi public method trên hub. Clients.Caller gửi EchoResponse về riêng kết nối vừa gọi. On đăng ký trước StartAsync để nhận sự kiện đến sớm.

### Bước 5 — Nhóm và broadcast

Giữ An mở, terminal thứ ba chạy Bình rồi chụp hai terminal client.

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 Binh
```

An nhận Group và Broadcast của Bình. Bình nhận Echo của chính mình và các thông điệp phát từ lúc Bình đã kết nối. Tin An gửi trước đó không được phát lại.

| Lời gọi | An | Bình | Chi ngoài nhóm |
|---|---|---|---|
| An gọi Echo | Có | Không | Không |
| Chi gửi nhóm lop-hoc-01 | Có | Có | Không |
| Bình gọi Broadcast | Có | Có | Có |

Chi gửi được vì hub minh họa chưa kiểm tra quyền gọi SendToGroup. Người nhận được xác định bằng thành viên nhóm.

### Bước 6 — Kết nối lại

Giữ An chạy. Ở terminal server nhấn Ctrl+C, đợi An hiện Dang ket noi lai, rồi chạy lại lệnh server. Chụp An khi có cả hai dòng lifecycle. Lịch thử mặc định là 0, 2, 10, 30 giây. Mỗi lần nối lại có ConnectionId mới và phải vào lại nhóm. Client đã gọi JoinGroup trong Reconnected.

### Bước 7 — Ba lỗi cố ý

Đối số thứ ba tái hiện lỗi bằng đúng đường dẫn và tên sự kiện sai trong slide. Chế độ mặc định vẫn dùng hợp đồng đúng.

1. Dừng server bằng Ctrl+C, chạy client để chụp lỗi nối đầu.

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 Test
```

2. Chạy lại server, dùng sai hub để chụp 404.

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 Test wrong-hub
```

3. Giữ server chạy, dùng sai tên sự kiện. Client kết nối và nhận Group/Broadcast nhưng không in Echo. Chụp rồi nhấn Enter để thoát.

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 Test wrong-event
```

| Thí nghiệm Ping | Dự đoán | Kết quả kiểm tra | Giải thích |
|---|---|---|---|
| API không chạy | Nối đầu thất bại | Client thất bại trên cổng không có server 5299 | AutomaticReconnect chỉ áp dụng sau khi đã nối thành công |
| /hub/ping | 404 | Đã kiểm tra 404 | Negotiate phải khớp MapHub /hubs/ping |
| EchoRespone | Không có Echo | Đã kiểm tra không có dòng Echo | SendAsync và On phải cùng tên |

### EduSubmit — REST và SignalR

Terminal máy chủ riêng. Cổng 5000 dành cho EduSubmit.

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeApi --urls http://localhost:5000
```

Terminal client, chụp kết quả trước khi client tự thoát.

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 dtc245200851
```

Mở RealtimeSubmissionSystem/requests.http bằng REST Client trong VS Code, gửi 9 request theo thứ tự. Request 4 dùng id của request 3, gửi lại sau 4 giây để thấy graded. Các mã mong đợi theo thứ tự là 200, 200, 201, 200, 400, 404, 404, 200, 404. POST giữ thứ tự kiểm tra bài tồn tại, bài đang mở, dữ liệu hợp lệ từ Bài 2. Dữ liệu được khóa khi đọc/ghi. Tác vụ nền lưu trạng thái trước khi gửi qua IHubContext tới nhóm submission:{id}.

### Năm thí nghiệm EduSubmit

| Thí nghiệm | Dự đoán trước khi chạy | Kết quả và giải thích |
|---|---|---|
| Hai client cùng lúc | Mỗi client nhận lượt nộp riêng | Đã chạy hai client SV001, SV002, mỗi bên nhận một id. Nhóm theo id tách người nhận |
| API tắt | Nối đầu thất bại | Cần chụp lại với API 5000 tắt. StartAsync chưa có kết nối để tự nối lại |
| Sai /hub/submissions | 404 | Ca 9 requests.http đã kiểm tra 404. Tuyến hub phải khớp |
| Sai SubmissionStatusChange | Hết thời gian chờ | Đã chạy, không có thông báo, REST cuối graded. Sai tên sự kiện khiến thông điệp không được xử lý |
| Watch muộn 4 giây | Lỡ processing và graded | Đã chạy, hết thời gian chờ, REST cuối graded. Tin đã gửi trước khi vào nhóm |

Các lệnh tái hiện hai thí nghiệm cuối.

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 wrong-event
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 late-watch
```

Với lỗi hub dùng đối số wrong-hub. Hai client dùng hai terminal với SV001 và SV002.

## Trả lời ba câu hỏi đầu giờ

1. Máy chủ chủ động báo được khi client đã mở kết nối lâu dài. Trong Hub dùng Clients, ngoài Hub dùng IHubContext để gửi sự kiện.
2. REST tạo và đọc tài nguyên, trả mã HTTP. SignalR đẩy thay đổi trạng thái tới nhóm đang theo dõi.
3. Tin graded có thể bị lỡ khi mất mạng. Sau khi nối lại, client Watch lại id và GET REST để biết trạng thái cuối. Client EduSubmit đã thực hiện cả hai việc trong Reconnected.

## Câu hỏi ôn tập

- Polling gửi yêu cầu liên tục, độ trễ phụ thuộc chu kỳ. Push gửi khi phát sinh sự kiện qua kết nối đang mở.
- Yêu cầu–phản hồi do client khởi tạo, có một phản hồi. Push do server gửi sự kiện. Streaming truyền nhiều phần dữ liệu trong một luồng.
- WebSocket là giao thức. SignalR là thư viện quản lý hub, kết nối, nhóm và truyền tải dự phòng bằng ServerSentEvents hoặc LongPolling.
- Nhóm theo lượt nộp giới hạn dữ liệu gửi cho người theo dõi và giảm băng thông. Hệ thật cần xác thực quyền vào nhóm.
- On<T> đăng ký xử lý sự kiện và kiểu dữ liệu. Thiếu handler thì ứng dụng không xử lý sự kiện đó.
- REST tạo lượt nộp có 201, Location và lỗi 400/404 rõ ràng. SignalR bổ sung thông báo thay đổi sau khi tạo.
- Polling phù hợp khi cập nhật hiếm hoặc hạ tầng không duy trì được kết nối lâu dài.
- Khi chạy nhiều API, nhóm trong bộ nhớ từng máy không chia sẻ tự động. Cần backplane như Redis hoặc Azure SignalR Service.

## Git

Các commit theo từng bước đã tạo cục bộ. Kiểm tra từ thư mục Bai3.

```powershell
git log --oneline -- .
git ls-files | Select-String '/(bin|obj)/'
```

Sau khi bổ sung ảnh, commit minh chứng. PR dùng tiêu đề Bai3 - dtc245200851 - Lê Hải Anh theo quy định lớp.
