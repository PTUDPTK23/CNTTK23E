## Kết quả thực hành

### 1. Build Solution thành công
Chạy lệnh `dotnet build` biên dịch thành công hai project `Ping.Api` và `Ping.Client`:

![Build Solution thành công](images/01-dotnet-build.png)

---

### 2. Kiểm tra Endpoint và quy trình Negotiate bằng cURL
Kiểm tra trạng thái dịch vụ tại `/` và thực hiện handshake lấy `connectionToken` tại `/hubs/ping/negotiate`:

![Kiểm tra cURL và Negotiate](images/02-curl-negotiate.png)

---

### 3. Kết nối Client - Server và kiểm thử Echo
Khởi chạy máy chủ `Ping.Api` tại cổng `5200` và máy khách `An` gửi nhận tin nhắn Echo:

![Kết nối Server và Client Echo](images/03-server-client-echo.png)

---

### 4. Giao tiếp Nhóm (Group) và Quảng bá (Broadcast) giữa 2 Client
Hai người dùng `An` và `Binh` cùng tham gia nhóm `lop-hoc-01`, trao đổi tin nhắn nhóm và nhận thông báo Broadcast:

![Giao tiếp Group và Broadcast](images/04-group-broadcast-two-clients.png)

---

### 5. Kiểm thử cơ chế tự động kết nối lại (Automatic Reconnect)
Khi máy chủ khởi động lại, Client tự động phát hiện mất kết nối và khôi phục phiên làm việc mới:

![Tự động kết nối lại](images/05-automatic-reconnect.png)

---

### 6. Ghi nhận log kết nối / ngắt kết nối tại Server Hub
Phía máy chủ theo dõi trạng thái `ket noi` và `ngat` theo từng `ConnectionId` của Client:

![Log kết nối tại Server Hub](images/06-server-hub-connection-log.png)

---

### 7. Cấu hình lắng nghe sự kiện phía Client
Đăng ký các handler `EchoRespone`, `BroadcastMessage`, `GroupMessage` trên `HubConnection`:

![Code xử lý sự kiện Client](images/07-client-event-handlers.png)