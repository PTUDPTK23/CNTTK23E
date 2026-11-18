# Bài 3: Giao tiếp thời gian thực với SignalR (`PingSignalR`)

- **Họ và tên:** Giàng Hồng Anh
- **MSSV:** DTC245200156
- **Lớp:** CNTTK23E
- **Môn học:** Phát triển ứng dụng phân tán
- **Giảng viên hướng dẫn:** TS. Dương Thu Mây

---

## I. Trả lời 3 câu hỏi đặt vấn đề đầu giờ

1. **Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?**
   - **Có**, nếu máy khách mở sẵn một kết nối hai chiều lâu dài tới máy chủ (sử dụng giao thức WebSocket thông qua thư viện SignalR). Khi có sự kiện xảy ra, máy chủ chủ động đẩy thông điệp xuống cho máy khách thông qua `Clients.All`, `Clients.Group(...)` hoặc `Clients.Caller` mà không cần máy khách phải gửi yêu cầu hỏi liên tục (polling).

2. **Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?**
   - Trong HTTP/REST thông thường, chỉ máy khách được phép mở lời trước nên không phù hợp để máy chủ tự báo thay đổi tức thì.
   - **Phân chia vai trò:** **REST** chịu trách nhiệm khởi tạo, cập nhật và đọc trạng thái dữ liệu với mã trạng thái rõ ràng (là nguồn sự thật của hệ thống); **SignalR** bổ sung kênh thông báo thời gian thực để đẩy các thay đổi phát sinh sau yêu cầu ban đầu tới đúng người nhận. SignalR không thay thế REST.

3. **Nếu đứt mạng đúng lúc máy chủ báo tin thì sao?**
   - Thông điệp đẩy đi trong lúc kết nối bị ngắt sẽ bị mất (SignalR không lưu hàng đợi để gửi bù tin cũ).
   - Cơ chế `WithAutomaticReconnect()` giúp máy khách tự động kết nối lại với một `ConnectionId` mới. Sau khi nối lại thành công, máy khách cần tham gia lại vào nhóm (nếu có) và gọi API **REST (`GET`)** để đồng bộ lại trạng thái mới nhất từ máy chủ.

---

## II. Nhật ký thực hành `PingSignalR` (Bước 1 – Bước 7)

### Bước 1: Khởi tạo Solution và hai Project
- **Việc đã làm:** Tạo thư mục `PingSignalR`, khởi tạo file solution `PingSignalR.slnx`, tạo hai project `Ping.Api` (ASP.NET Core Empty) và `Ping.Client` (Console App), cài đặt gói `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15` cho máy khách và tạo `.gitignore`.
- **Kết quả:** Lệnh `dotnet build` biên dịch thành công cả hai project với `0 Warning(s), 0 Error(s)`.

---

### Bước 2: Cấu hình Hub trên máy chủ và kiểm tra bắt tay `negotiate`
- **Việc đã làm:** Đăng ký dịch vụ `builder.Services.AddSignalR()`, ánh xạ tuyến `app.MapHub<PingHub>("/hubs/ping")` và chạy máy chủ tại cổng `5200`. Dùng `curl.exe` gọi kiểm tra endpoint gốc và bước bắt tay `POST /hubs/ping/negotiate?negotiateVersion=1`.
- **Kết quả:**
  - `GET /` trả về `{"service":"PingApi","status":"running"}`.
  - `POST /hubs/ping/negotiate` trả về `connectionId` cùng 3 cơ chế truyền tải hỗ trợ theo thứ tự ưu tiên: `WebSockets`, `ServerSentEvents`, `LongPolling`.
  - `GET /hubs/ping` trực tiếp trả về mã lỗi `400 Bad Request (Connection ID required)` vì Hub yêu cầu quy trình bắt tay kết nối của SignalR.

---

### Bước 3 & Bước 4: Giao tiếp hai chiều với phương thức `Echo`
- **Việc đã làm:**
  - Phía máy chủ (`Ping.Api`): Viết phương thức `Echo(string message)` trong `PingHub`, sử dụng `Clients.Caller.SendAsync("EchoResponse", ...)` để trả lời riêng cho người gọi.
  - Phía máy khách (`Ping.Client`): Xây dựng `HubConnectionBuilder` trỏ tới `http://localhost:5200/hubs/ping`, đăng ký lắng nghe `connection.On<string>("EchoResponse", ...)` trước khi gọi `StartAsync()`, sau đó gọi `InvokeAsync("Echo", ...)`.
- **Kết quả:** Máy khách kết nối thành công tới `PingApi` và nhận được phản hồi `[Echo] Echo: Xin chao SignalR tu An`.

![Bước 4.2 - Chạy Echo giữa Server và Client](PingSignalR/buoc4.2.png)

---

### Bước 5: Phát rộng (`Broadcast`) và Nhóm (`Group`)
- **Việc đã làm:** Bổ sung ba phương thức `Broadcast` (`Clients.All`), `JoinGroup` (`Groups.AddToGroupAsync`), và `SendToGroup` (`Clients.Group`) vào `PingHub`. Mở đồng thời 3 cửa sổ Terminal: Máy chủ (chạy trước), máy khách **An** (chạy thứ hai), và máy khách **Bình** (chạy sau cùng).
- **Kết quả:**
  - Cửa sổ của **An** nhận được thông báo `[Group]` (khi Bình vào nhóm `lop-hoc-01` và gửi tin nhắn nhóm) cùng tin `[Broadcast]` từ Bình, nhưng **không nhận** được tin `[Echo]` của Bình (do `Clients.Caller` chỉ gửi trả riêng cho Bình).
  - Cửa sổ của **Bình** nhận được đầy đủ `[Echo]`, `[Group]`, `[Broadcast]` của chính mình và không nhận các tin An đã gửi trước thời điểm Bình kết nối.

![Bước 5 - Cài đặt Broadcast và Group](PingSignalR/buoc5.png)

![Bước 5.3 - Kiểm tra 3 terminal Server, An và Bình](PingSignalR/buoc5.3.png)

---

### Bước 6: Vòng đời kết nối và tự động kết nối lại (`WithAutomaticReconnect`)
- **Việc đã làm:** Ghi đè `OnConnectedAsync` và `OnDisconnectedAsync` trong `PingHub` để máy chủ ghi log `[Hub] + ... ket noi` / `[Hub] - ... ngat` và phát `SystemMessage`. Đăng ký các sự kiện `Reconnecting`, `Reconnected`, `Closed` trên `Ping.Client`. Tiến hành tắt máy chủ bằng `Ctrl + C` khi An đang kết nối, sau đó khởi động lại máy chủ trong vòng 30 giây.
- **Kết quả & Giải thích:**
  - Khi máy chủ tắt, máy khách in `[Lifecycle] Dang ket noi lai...` và tự động thử lại theo lịch mặc định **0 giây → 2 giây → 10 giây → 30 giây**.
  - Khi máy chủ bật lại, máy khách in `[Lifecycle] Da ket noi lai` kèm một **`ConnectionId` mới** khác ban đầu. Các nhóm đã tham gia trước đó bị mất (vì gắn với `ConnectionId` cũ) và các tin phát sinh trong lúc mất mạng không được gửi lại.

![Bước 6 - Thí nghiệm ngắt máy chủ và tự kết nối lại](PingSignalR/buoc6.png)

---

### Bước 7: Ba thí nghiệm lỗi điển hình trong kết nối phân tán

| STT | Thí nghiệm (Làm gì) | Hiện tượng quan sát được (Thấy gì) | Giải thích nguyên nhân (Vì sao) |
| :--- | :--- | :--- | :--- |
| **1** | **Máy chủ chưa chạy:** Tắt hẳn `Ping.Api` ở Terminal 1 rồi chạy `Ping.Client`. | Chương trình dừng ngay và văng ngoại lệ `HttpRequestException: Connection refused (localhost:5200)`. | `StartAsync()` ném lỗi ngay khi không thể mở kết nối ban đầu. `WithAutomaticReconnect()` chỉ cứu các kết nối **đã mở thành công** trước đó. |
| **2** | **Sai đường dẫn Hub:** Sửa `.WithUrl` ở máy khách thành `/hub/ping` (thiếu chữ `s`). | Máy khách báo lỗi `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`. | Lỗi xảy ra ngay bước thương lượng đầu tiên (`negotiate`). Tuyến URL ở `WithUrl` bắt buộc phải khớp chính xác với tuyến khai báo trong `MapHub`. |
| **3** | **Sai tên thông điệp / phương thức:** Sửa tên sự kiện nhận thành `On("EchoRespone")` (thiếu chữ `s`) hoặc gọi sai tên hàm Hub. | - Sai tên nhận chiều xuống (`On`): Chương trình chạy bình thường, **không báo lỗi**, nhưng không in dòng `[Echo]`.<br>- Sai tên hàm chiều lên (`InvokeAsync`): Ném lỗi `HubException: Method does not exist`. | SignalR bỏ qua trong im lặng nếu máy khách chưa đăng ký đúng tên thông điệp máy chủ gửi về. Do đó, đối chiếu tên hai phía là bước kiểm tra đầu tiên khi không nhận được dữ liệu. |

![Bước 7 - Thí nghiệm lỗi kết nối và sai đường dẫn](PingSignalR/buoc7.png)

![Bước 7 - Thí nghiệm sai tên thông điệp](PingSignalR/buoc7saiTD.png)

---

## III. Tổng hợp 4 phạm vi gửi thông điệp trong SignalR

| Phạm vi | Đối tượng nhận | Ý nghĩa sử dụng |
| :--- | :--- | :--- |
| `Clients.All` | Mọi kết nối đang mở (bao gồm cả người gửi) | Phát thông báo chung toàn hệ thống (bảo trì, thông báo mới). |
| `Clients.Caller` | Chỉ kết nối vừa gọi phương thức Hub | Phản hồi riêng cho người gửi yêu cầu (ví dụ: `Echo`, xác nhận thao tác). |
| `Clients.Group(tenNhom)` | Tất cả thành viên hiện có trong nhóm `tenNhom` | Gửi thông tin theo phòng chat, lớp học hoặc theo dõi từng đối tượng cụ thể, đảm bảo riêng tư và tiết kiệm băng thông. |
| `Clients.Client(connectionId)` | Duy nhất một kết nối có `ConnectionId` chỉ định | Gửi tin trực tiếp tới một phiên làm việc cụ thể. |

---

## IV. Câu hỏi ôn tập

1. **Polling khác Push ở số yêu cầu và độ trễ thế nào?**
   - **Polling:** Máy khách phải gửi yêu cầu định kỳ liên tục nên sinh ra rất nhiều yêu cầu thừa (khi dữ liệu chưa đổi) và luôn có độ trễ tối đa bằng chu kỳ hỏi.
   - **Push:** Chỉ mở 1 kết nối duy nhất; máy chủ chủ động gửi ngay khi có sự kiện nên 100% thông điệp đều có ích và độ trễ gần như bằng không.

2. **Phân biệt 3 mô hình trao đổi thông điệp (MEP):**
   - **Yêu cầu – Phản hồi (Request/Response):** Máy khách khởi tạo, 1 yêu cầu nhận 1 phản hồi (REST).
   - **Đẩy dữ liệu (Push):** Máy chủ khởi tạo khi có sự kiện, gửi từng thông điệp lẻ không đều (SignalR).
   - **Truyền luồng (Streaming):** Một hoặc cả hai phía khởi tạo, gửi chuỗi nhiều thông điệp liên tục (tiến độ %, log, dữ liệu cảm biến).

3. **WebSocket và SignalR khác nhau như thế nào? SignalR có các cơ chế dự phòng nào?**
   - **WebSocket** là **giao thức** tầng truyền thông giúp giữ kênh kết nối hai chiều sau bước bắt tay HTTP.
   - **SignalR** là **thư viện** cấp cao của ASP.NET Core xây dựng trên WebSocket, cung cấp sẵn Hub, quản lý nhóm, tự động kết nối lại và tự động chuyển đổi giữa 3 cơ chế truyền tải theo thứ tự ưu tiên: **WebSockets → Server-Sent Events (SSE) → Long Polling**.

4. **`On<T>` dùng để làm gì? Bỏ đi thì sao?**
   - `connection.On<T>("TenThongDiep", handler)` đăng ký hàm xử lý phía máy khách để đón nhận và giải tuần tự hóa (deserialize) dữ liệu JSON khi máy chủ gọi `SendAsync("TenThongDiep", data)`. Nếu bỏ đi hoặc đặt sai tên, máy khách vẫn chạy bình thường nhưng sẽ bỏ qua thông điệp trong im lặng.