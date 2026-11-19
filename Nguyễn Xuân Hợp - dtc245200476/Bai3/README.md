# BÁO CÁO BÀI TẬP 3: GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR
## Học phần: Phát triển ứng dụng phân tán (ICTU)
### Giảng viên hướng dẫn: TS. Dương Thu Mây

---

## 1. THÔNG TIN SINH VIÊN
- **Họ và tên:** Nguyễn Xuân Hợp
- **Mã sinh viên:** dtc245200476
- **Lớp:** CNTTK23E
- **Khoa:** Công nghệ thông tin - Trường Đại học Công nghệ Thông tin & Truyền thông Thái Nguyên
- **Kho lưu trữ (Fork Repo):** [https://github.com/nguyenxuanhoppt478-cmyk/CNTTK23E](https://github.com/nguyenxuanhoppt478-cmyk/CNTTK23E)

---

## 2. CẤU TRÚC THƯ MỤC BÀI 3

```text
Nguyễn Xuân Hợp - dtc245200476/
└── Bai3/
    ├── README.md                          # Báo cáo lý thuyết, thực nghiệm và hướng dẫn chạy
    ├── PingSignalR/                       # Phần 1: Thử nghiệm cơ bản SignalR Hub
    │   ├── PingSignalR.sln
    │   ├── .gitignore
    │   └── src/
    │       ├── Ping.Api/                  # Máy chủ SignalR Hub (Port 5200)
    │       │   ├── Ping.Api.csproj
    │       │   └── Program.cs
    │       └── Ping.Client/               # Client tự động thử nghiệm Echo, Broadcast, Group, Reconnect
    │           ├── Ping.Client.csproj
    │           └── Program.cs
    └── RealtimeSubmissionSystem/          # Phần 2: Hệ thống nộp bài & chấm điểm Realtime (EduSubmit)
        ├── RealtimeSubmissionSystem.sln
        ├── .gitignore
        ├── requests.http                  # 9 kịch bản kiểm thử REST API & SignalR Negotiate
        └── src/
            ├── Submission.RealtimeApi/    # REST API + SubmissionsHub + Chấm điểm ngầm (Port 5000)
            │   ├── Submission.RealtimeApi.csproj
            │   └── Program.cs
            └── Submission.RealtimeClient/ # Client nộp bài REST & theo dõi tiến trình chấm realtime
                ├── Submission.RealtimeClient.csproj
                └── Program.cs
```

---

## 3. TRẢ LỜI CÂU HỎI LÝ THUYẾT (SLIDE 65)

### Câu hỏi 1: Cơ chế hoạt động của SignalR Hub và sự khác nhau giữa Caller, All, Group?

1. **Cơ chế hoạt động của SignalR Hub:**
   - **SignalR Hub** là lớp trừu tượng mức cao (High-Level Abstraction) trong kiến trúc ASP.NET Core SignalR, đóng vai trò như một bộ điều phối RPC (Remote Procedure Call) hai chiều giữa máy chủ (Server) và các ứng dụng khách (Clients).
   - Khi Client kết nối đến Hub thông qua URL định tuyến (ví dụ `/hubs/ping`), quá trình bắt tay ban đầu (Negotiation) sẽ xác định giao thức truyền tối ưu nhất (WebSockets, SSE hoặc Long Polling).
   - Máy chủ quản lý mỗi kết nối bằng một định danh duy nhất gọi là `ConnectionId`.
   - Hub cho phép máy khách gọi trực tiếp các phương thức công khai trên Server (Client-to-Server RPC) bằng cú pháp `connection.InvokeAsync("MethodName", args)`. Ngược lại, máy chủ có thể chủ động đẩy dữ liệu và kích hoạt sự kiện trên máy khách (Server-to-Client RPC) thông qua `Clients.SendAsync("EventName", payload)`.

2. **Sự khác biệt giữa Caller, All và Group:**

| Phạm vi phát tín hiệu | Cú pháp gọi | Đối tượng nhận tin | Tình huống áp dụng thực tế |
| :--- | :--- | :--- | :--- |
| **Caller** | `Clients.Caller.SendAsync(...)` | Chỉ duy nhất máy khách đang gửi request hiện tại đến Hub. | Phản hồi kết quả riêng tư (Echo, xác nhận đã nhận lệnh, thông báo lỗi kiểm tra dữ liệu đầu vào). |
| **All** | `Clients.All.SendAsync(...)` | Toàn bộ tất cả các máy khách đang kết nối tới Hub tại thời điểm đó. | Thông báo khẩn toàn hệ thống, tin tức bảo trì máy chủ, phòng trò chuyện chung (Public Chat). |
| **Group** | `Clients.Group(groupName).SendAsync(...)` | Chỉ các máy khách đã được thêm vào nhóm logic tương ứng (`Groups.AddToGroupAsync`). | Phòng họp riêng, thông báo theo lớp học, hoặc theo dõi tiến trình chấm của từng bài nộp riêng biệt (`submission:{id}`). |

---

### Câu hỏi 2: So sánh các Transport Protocols: WebSockets vs Server-Sent Events (SSE) vs Long Polling?

ASP.NET Core SignalR tự động đàm phán và lựa chọn giao thức truyền tải tốt nhất dựa trên khả năng hỗ trợ của máy chủ, trình duyệt và hạ tầng mạng:

| Tiêu chí | WebSockets | Server-Sent Events (SSE) | Long Polling |
| :--- | :--- | :--- | :--- |
| **Chiều truyền dữ liệu** | Song công toàn phần (Full-duplex, hai chiều đồng thời) | Một chiều (Server $\to$ Client), Client gửi tin phải dùng HTTP POST riêng | Nửa song công giả lập qua các chu kỳ HTTP request/response liên tục |
| **Giao thức nền tảng** | TCP Socket độc lập (sau cú bắt tay HTTP Upgrade) | Giao thức HTTP chuẩn (`Content-Type: text/event-stream`) | Giao thức HTTP 1.1 / HTTP 2 chuẩn |
| **Độ trễ (Latency)** | Cực thấp ($\sim 1 - 5 \text{ ms}$), tin nhắn được truyền ngay khi có sự kiện | Thấp ($\sim 10 - 50 \text{ ms}$) đối với chiều từ server xuống client | Cao ($\sim 100 - 1000 \text{ ms}$) do thời gian mở và đóng kết nối HTTP mới |
| **Chi phí Header (Overhead)** | Rất nhỏ ($\approx 2 - 10 \text{ bytes}$ cho mỗi frame dữ liệu) | Nhỏ, duy trì kết nối HTTP dài và gửi từng dòng text | Rất lớn, mỗi chu kỳ đều phải gửi lại toàn bộ HTTP Header ($\approx 1 - 2 \text{ KB}$) |
| **Định dạng dữ liệu** | Hỗ trợ cả Text (JSON) và Binary (MessagePack) | Chỉ hỗ trợ Text (chuỗi văn bản mã hóa UTF-8) | Hỗ trợ cả Text và Binary (mỗi gói tin là 1 HTTP body) |
| **Khả năng qua Proxy/Firewall** | Có thể bị chặn bởi một số proxy cũ hoặc tường lửa doanh nghiệp | Tương thích tốt với hầu hết Proxy và CDN | Tương thích 100% với mọi hệ thống mạng, Proxy và Firewall |
| **Thứ tự ưu tiên SignalR** | **Mặc định số 1** (Tối ưu nhất) | **Dự phòng số 2** (Fallback khi không có WebSockets) | **Dự phòng cuối cùng** (Fallback an toàn nhất) |

---

### Câu hỏi 3: Tại sao SignalR phù hợp cho thông báo tiến trình nộp bài so với HTTP Polling?

Trong hệ thống nộp bài trực tuyến (EduSubmit Realtime):
1. **Bản chất của bài toán:**
   - Quá trình biên dịch và kiểm thử tự động (Auto-grading) mã nguồn sinh viên thường tốn từ vài giây đến hàng chục giây. Trạng thái bài nộp thay đổi tuần tự: `received` $\to$ `processing` $\to$ `graded`.
2. **Hạn chế nghiêm trọng của HTTP Polling:**
   - Với Polling, máy khách buộc phải gửi request liên tục (ví dụ mỗi 1 giây) để thăm dò: *"Đã chấm xong chưa?"*.
   - Nếu có 100 sinh viên cùng nộp bài và thời gian chấm là 10 giây, máy chủ sẽ phải tiếp nhận hơn $1.000$ HTTP requests, trong đó hơn 90% trả về cùng một nội dung là `processing`. Điều này gây lãng phí băng thông mạng, tốn tài nguyên CPU máy chủ và gây tranh chấp khóa bộ nhớ/database.
3. **Ưu thế vượt trội của SignalR:**
   - **Mô hình Hướng sự kiện (Event-driven / Server-Push):** Client chỉ gửi 1 request REST duy nhất để nộp bài và nhận về `Id`, sau đó lắng nghe qua kênh SignalR.
   - Khi Worker nền hoàn tất việc chấm bài, Server sẽ chủ động **đẩy (Push)** thông báo `SubmissionStatusChanged` kèm điểm số và nhận xét ngay tức thì tới đúng nhóm quan sát viên (`submission:{id}`).
   - **Tối ưu tài nguyên:** Không tạo ra bất kỳ request rác nào, độ trễ cập nhật bằng 0, đảm bảo tính bảo mật và trải nghiệm mượt mà, chuyên nghiệp cho người dùng.

---

## 4. TỔNG HỢP KẾT QUẢ 5 THỰC NGHIỆM (SLIDE 58, 59)

| STT | Tên thực nghiệm | Mục tiêu kiểm thử | Dữ liệu gửi / Nhận | Kết quả quan sát | Trạng thái |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **1** | **Gửi & Nhận Echo** *(PingSignalR)* | Kiểm tra giao tiếp điểm - điểm giữa Client và Server qua `Clients.Caller`. | **Gửi:** `"Hello SignalR!"`<br>**Nhận:** `[Server Echo] Hello SignalR! (lúc 17:23:50)` | Server xử lý tức thì, phản hồi chính xác đến đúng caller kèm timestamp. | **PASSED** |
| **2** | **Gửi Broadcast** *(PingSignalR)* | Phát thông điệp đến tất cả các client đang kết nối qua `Clients.All`. | **Gửi:** `"Chào cả lớp CNTTK23E!"`<br>**Nhận:** `[Broadcast từ PkynS8...] Chào cả lớp CNTTK23E!` | Tất cả client đang kết nối đều nhận được tin nhắn phát thanh đồng thời. | **PASSED** |
| **3** | **Nhóm (Group Messaging)** *(PingSignalR)* | Tham gia nhóm và gửi tin nhắn nội bộ qua `Clients.Group`. | **Thao tác:** Tham gia nhóm `CNTTK23E`<br>**Gửi:** `"Thành viên Nguyễn Xuân Hợp điểm danh!"` | Chỉ các client nằm trong nhóm `CNTTK23E` mới nhận được thông điệp, cô lập với nhóm khác. | **PASSED** |
| **4** | **Nộp bài & Theo dõi Realtime** *(EduSubmit)* | Tích hợp REST API nộp bài với SignalR Hub theo dõi tiến trình chấm bất đồng bộ. | **POST REST:** Nộp bài tập 3<br>**SignalR Group:** `submission:{id}`<br>**Sự kiện nhận:** `processing` $\to$ `graded` | Client nhận cập nhật trạng thái liên tục mà không cần refresh hay polling. | **PASSED** |
| **5** | **Tự động kết nối lại (Auto Reconnect)** | Kiểm thử khả năng chịu lỗi mạng của Client với chính sách retry. | **Chính sách:** Thử lại sau $0\text{s}$, $2\text{s}$, $5\text{s}$, $10\text{s}$.<br>**Sự kiện:** `Reconnecting` $\to$ `Reconnected`. | Khi ngắt kết nối mạng tạm thời, client tự động đàm phán và nối lại kết nối thành công. | **PASSED** |

---

## 5. KẾT QUẢ CHẠY THỰC TẾ CỦA SUBMISSION.REALTIMECLIENT

Dưới đây là 8 dòng đầu ra chuẩn thu được khi chạy `Submission.RealtimeClient` kết nối tới `Submission.RealtimeApi`:

```text
[Client] Kết nối SignalR Hub thành công
[REST] Nộp bài thành công: id = 5e584fd8-bfee-4a11-8d72-fed98f6ac0b7, status = received
[SignalR] Đã tham gia nhóm theo dõi: submission:5e584fd8-bfee-4a11-8d72-fed98f6ac0b7
[SignalR] Nhận thông báo: status = processing (lúc 17:23:35)
[SignalR] Nhận thông báo: status = graded (lúc 17:23:37)
[SignalR] Điểm: 9,5/10 - Nhận xét: Bài làm xuất sắc! Tất cả test case đều passed.
[REST] Đọc lại bài nộp: status = graded, grade = 9,5
[Client] Hoàn thành quy trình nộp và theo dõi bài tập
```

---

## 6. HƯỚNG DẪN BIÊN DỊCH VÀ CHẠY DỰ ÁN

### Yêu cầu môi trường
- .NET SDK version 8.0, 9.0 hoặc 10.0 (Đã kiểm thử tối ưu trên .NET 10.0).

### Cách chạy Phần 1: PingSignalR (Port 5200)
1. **Khởi chạy Máy chủ API:**
   ```powershell
   cd "PingSignalR/src/Ping.Api"
   dotnet run
   ```
2. **Khởi chạy Client kiểm thử:**
   ```powershell
   cd "PingSignalR/src/Ping.Client"
   dotnet run
   ```

### Cách chạy Phần 2: EduSubmit Realtime (Port 5000)
1. **Khởi chạy Máy chủ Realtime API:**
   ```powershell
   cd "RealtimeSubmissionSystem/src/Submission.RealtimeApi"
   dotnet run
   ```
2. **Khởi chạy Client nộp bài & theo dõi:**
   ```powershell
   cd "RealtimeSubmissionSystem/src/Submission.RealtimeClient"
   dotnet run
   ```
3. **Kiểm thử thủ công bằng file HTTP:**
   - Mở file `RealtimeSubmissionSystem/requests.http` bằng Visual Studio hoặc extension REST Client của VS Code để chạy từng yêu cầu từ 1 đến 9.
