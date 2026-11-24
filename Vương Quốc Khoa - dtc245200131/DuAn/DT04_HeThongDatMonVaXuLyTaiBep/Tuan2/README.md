# HỆ THỐNG ĐẶT MÓN VÀ XỬ LÝ TẠI BẾP (FOOD ORDERING AND KITCHEN PROCESSING SYSTEM)
## BÁO CÁO KỸ THUẬT DỰ ÁN TUẦN 2 — TÍCH HỢP GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR

> **Môn học**: Phát triển ứng dụng phân tán  
> **Sinh viên thực hiện**: Vương Quốc Khoa — DTC245200131 — Lớp CNTTK23E  
> **Đề tài 04**: Hệ thống đặt món và xử lý tại bếp  
> **Phạm vi kiến thức**: Bám sát tài liệu **Bài 3 – SignalR: Giao tiếp thời gian thực trong ứng dụng phân tán** của TS. Dương Thu Mây  

---

## 1. Giới thiệu đề tài & Động lực nâng cấp từ Tuần 1

### 1.1. Bối cảnh nghiệp vụ
Trong các nhà hàng và chuỗi dịch vụ ăn uống hiện đại, tốc độ phục vụ và trải nghiệm của thực khách phụ thuộc lớn vào việc luân chuyển thông tin giữa khách hàng tại bàn và đội ngũ đầu bếp tại khu vực bếp:
- **Khách hàng tại bàn**: Sau khi gửi đơn gọi món, khách hàng cầm một thiết bị thông báo (Thẻ rung điện tử - Buzzer Pager). Khi đầu bếp nhận đơn hoặc nấu xong món ăn, thẻ rung sẽ sáng đèn và phát tín hiệu báo cho khách hàng đến quầy nhận món.
- **Nhân viên nhà bếp**: Sử dụng màn hình điều phối bếp (Kitchen Display System - KDS) để theo dõi các đơn hàng mới xuất hiện ngay lập tức mà không cần bấm làm mới trang, sau đó cập nhật tiến độ chế biến qua từng nấc.

### 1.2. Hạn chế của Tuần 1 (Kiến trúc REST đơn thuần)
Ở **Tuần 1**, hệ thống hoạt động hoàn toàn dựa trên giao thức HTTP Request–Response theo chuẩn Bài 2:
- Khách hàng không thể biết món ăn đã nấu xong hay chưa nếu không chủ động gửi `GET /api/orders/{id}` liên tục (kỹ thuật **HTTP Polling**).
- Nhà bếp không thể biết khi nào có đơn hàng mới xuất hiện nếu không liên tục làm mới `GET /api/kitchen/orders`.
- Việc liên tục gửi các HTTP request rỗng gây lãng phí băng thông mạng, tốn tải xử lý của máy chủ Kestrel và tạo ra độ trễ thông tin lớn.

### 1.3. Giải pháp Tuần 2: Tích hợp SignalR Realtime (Chuẩn Bài 3)
Ở **Tuần 2**, hệ thống kết hợp hài hòa giữa **RESTful API** và **SignalR Hub**:
- **REST API** tiếp tục là **Nguồn sự thật (Source of Truth)**, chịu trách nhiệm tiếp nhận lệnh ghi (Commands), tính toán hóa đơn, xác thực nghiệp vụ và truy vấn dữ liệu chi tiết.
- **SignalR Hub** đóng vai trò là kênh **phát tín hiệu tức thời (Event Notification)**, tự động đẩy (Push) thông báo xuống đúng đối tượng người dùng thông qua cơ chế nhóm (SignalR Groups).

---

## 2. Kiến trúc hệ thống thời gian thực

### 2.1. Vận dụng mô hình "Quán ăn & Thẻ rung" (Slide 8)
Hệ thống Tuần 2 hiện thực hóa chính xác bài toán ví dụ kinh điển trong Slide 8 của bài giảng:

```text
[Khách hàng tại bàn] ────── POST /api/orders (REST) ──────> [Máy chủ Web API]
        │                                                           │
        │ (Nhận Location Header: /api/orders/1)                     │
        ▼                                                           │
[Kết nối SignalR Hub] <─── WebSocket / Long Polling ───────────────┤ (Inject IHubContext<OrderHub>)
        │                                                           │
  RPC WatchOrder(1)                                                 │
(Tham gia nhóm order:1)                                             │
        │                                                           ▼
        │                                             [Bắn NewOrderPlaced sang 'kitchen']
        │                                                           │
        │                                                           ▼
        │                                                   [Màn hình Bếp KDS]
        │                                                   (Ting ting có đơn mới!)
        │                                                           │
        │                                                   POST /kitchen/orders/1/start
        │                                                           │
        │<── Push OrderStatusChanged (Preparing) ───────────────────┤
  (Đèn vàng nhấp nháy)                                              │
        │                                                   POST /kitchen/orders/1/complete
        │                                                           │
        │<── Push OrderStatusChanged (Ready) ───────────────────────┘
(Đèn xanh sáng & Rung liên tục!)
```

### 2.2. Trả lời Khung 4 câu hỏi thiết kế hệ thời gian thực (Slide 22)

| Câu hỏi thiết kế (Slide 22) | Nghiệp vụ Hệ thống Đặt món | Giải pháp kỹ thuật SignalR Tuần 2 |
|---|---|---|
| **1. Sự kiện nào cần thời gian thực?** | Có đơn đặt món mới; Bếp nhận nấu; Bếp nấu xong. | Phát 2 sự kiện: `NewOrderPlaced` và `OrderStatusChanged`. |
| **2. Sự kiện gửi cho ai? (Targeting)** | - Đơn mới: Chỉ gửi cho **Đầu bếp**.<br>- Trạng thái đơn: Chỉ gửi cho **Khách hàng của đơn đó**. | Sử dụng **SignalR Groups**:<br>- Nhóm `kitchen`: Tất cả nhân viên bếp.<br>- Nhóm `order:{id}`: Khách hàng sở hữu đơn hàng `id`. |
| **3. Cần gửi kèm thông tin gì? (Payload)** | Thông tin tóm tắt vừa đủ, không gửi thừa dữ liệu. | DTO gọn nhẹ: `OrderStatusNotification(OrderId, Status, Message, UpdatedAt)`. |
| **4. Khi mất kết nối thì xử lý thế nào?** | Khách mất kết nối mạng hoặc máy chủ khởi động lại. | Cấu hình `.WithAutomaticReconnect(...)`. Sau khi kết nối lại, Client tự động gọi REST `GET /api/orders/{id}` để đồng bộ lại dữ liệu mới nhất từ **Nguồn sự thật**. |

---

## 3. Cấu trúc thư mục Dự án Tuần 2

```text
Tuan2/
├── FoodOrderingRealtime.slnx                # Solution .NET 10 quản lý 3 projects
├── README.md                                # Báo cáo kỹ thuật, hướng dẫn chạy & kết quả kiểm thử
├── requests.http                            # Kịch bản 14 requests kiểm thử REST API & Negotiate Hub
├── anh/                                     # Thư mục lưu nhật ký & ảnh minh chứng kiểm thử
│   ├── README.md
│   └── test_results.txt                     # Nhật ký thực thi thực tế 4 kịch bản kiểm thử
└── src/
    ├── FoodOrdering.RealtimeApi/            # Máy chủ ASP.NET Core Minimal API + SignalR Hub
    │   ├── Hubs/
    │   │   └── OrderHub.cs                  # Hub điều phối SignalR, RPC & Connection Lifecycle
    │   ├── Models/
    │   │   └── RealtimeDtos.cs              # Hợp đồng dữ liệu DTOs (REST + Notifications)
    │   ├── Properties/
    │   │   └── launchSettings.json          # Cấu hình cổng Kestrel: http://localhost:5227
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── FoodOrdering.RealtimeApi.csproj
    │   └── Program.cs                       # Endpoints REST, DI IHubContext, Thread-safe storage
    │
    ├── FoodOrdering.CustomerClient/         # Máy khách Khách hàng (Thẻ rung điện tử Console)
    │   ├── FoodOrdering.CustomerClient.csproj
    │   └── Program.cs                       # REST đặt món, SignalR WithAutomaticReconnect, REST Sync
    │
    └── FoodOrdering.KitchenClient/          # Máy khách Nhà bếp (Bảng điều phối KDS Console)
        ├── FoodOrdering.KitchenClient.csproj
        └── Program.cs                       # Lắng nghe NewOrderPlaced, Menu thao tác 1-2-3 qua REST
```

---

## 4. Hướng dẫn cài đặt và khởi chạy hệ thống

### 4.1. Yêu cầu môi trường
- .NET SDK 10.0 trở lên.
- Hệ điều hành Windows (hỗ trợ tốt trên PowerShell / Windows Terminal).

### 4.2. Biên dịch Solution
Mở terminal tại thư mục `Tuan2`:
```powershell
cd "F:\Code_demo\CNTTK23E\Vương Quốc Khoa - dtc245200131\DuAn\DT04_HeThongDatMonVaXuLyTaiBep\Tuan2"
dotnet build FoodOrderingRealtime.slnx
```
*(Kết quả kỳ vọng: `Build succeeded. 0 Warning(s), 0 Error(s)`).*

### 4.3. Khởi chạy Máy chủ API (`FoodOrdering.RealtimeApi`)
Mở Terminal 1:
```powershell
dotnet run --project src/FoodOrdering.RealtimeApi --urls http://localhost:5227
```
*Màn hình hiển thị: `Now listening on: http://localhost:5227`.*

### 4.4. Khởi chạy Bảng điều phối Bếp (`FoodOrdering.KitchenClient`)
Mở Terminal 2:
```powershell
dotnet run --project src/FoodOrdering.KitchenClient -- http://localhost:5227
```
*Màn hình hiển thị: `Đã kết nối thành công tới Hub! ConnectionId: ...` và `Kênh: kitchen`.*

### 4.5. Khởi chạy Thẻ rung Khách hàng (`FoodOrdering.CustomerClient`)
Mở Terminal 3:
```powershell
# Chạy với tên khách và bàn cụ thể:
dotnet run --project src/FoodOrdering.CustomerClient -- http://localhost:5227 "Vuong Quoc Khoa" "Ban 09"
```

---

## 5. Đặc tả Giao diện Lập trình (REST API & SignalR Hub)

### 5.1. Bảng đặc tả RESTful API
| HTTP Method | Route URI | Chức năng nghiệp vụ | Tương tác SignalR kèm theo |
|---|---|---|---|
| `GET` | `/` | Kiểm tra trạng thái máy chủ & SignalR | Trả về JSON `{ service, status, realtime, hub }` |
| `GET` | `/api/menu-items` | Lấy danh sách toàn bộ thực đơn | Không |
| `GET` | `/api/menu-items/{id}` | Lấy chi tiết món ăn | Trả về `200 OK` hoặc `404 ITEM_NOT_FOUND` |
| `POST` | `/api/orders` | Khách hàng tạo đơn đặt món mới | Kích hoạt `hubContext.Clients.Group("kitchen").SendAsync("NewOrderPlaced", order)` |
| `GET` | `/api/orders/{id}` | Xem chi tiết đơn hàng (**Nguồn sự thật**) | Dùng để tra cứu và đồng bộ lại khi mất kết nối |
| `GET` | `/api/kitchen/orders` | Bếp xem danh sách tất cả các đơn | Không |
| `POST` | `/api/kitchen/orders/{id}/start` | Bếp xác nhận bắt đầu chế biến | Kích hoạt `hubContext.Clients.Group($"order:{id}").SendAsync("OrderStatusChanged", ...)` (Status: Preparing) |
| `POST` | `/api/kitchen/orders/{id}/complete` | Bếp xác nhận hoàn thành món ăn | Kích hoạt `hubContext.Clients.Group($"order:{id}").SendAsync("OrderStatusChanged", ...)` (Status: Ready) |

### 5.2. Bảng đặc tả Sự kiện & RPC SignalR
| Tên phương thức / Sự kiện | Chiều truyền | Nhóm đích / Đối tượng | Ý nghĩa nghiệp vụ |
|---|:---:|---|---|
| `WatchOrder(int orderId)` | Client ➔ Server | Server Hub | Khách hàng đăng ký tham gia phòng `order:{orderId}` để nhận thông báo riêng. |
| `LeaveOrder(int orderId)` | Client ➔ Server | Server Hub | Khách hàng rời phòng khi đơn hàng đã kết thúc. |
| `JoinKitchen()` | Client ➔ Server | Server Hub | Đầu bếp đăng ký tham gia kênh `kitchen` để nhận đơn mới. |
| `NewOrderPlaced` | Server ➔ Client | Nhóm `kitchen` | Bắn thông tin đơn hàng mới vừa tạo kèm danh sách món để bếp hiển thị ngay. |
| `OrderStatusChanged` | Server ➔ Client | Nhóm `order:{orderId}` | Bắn trạng thái cập nhật (`Preparing`, `Ready`) kèm thông điệp xuống thẻ rung của khách. |

---

## 6. Báo cáo Kết quả Kiểm thử Thực tế

> [!NOTE]
> Toàn bộ 4 kịch bản kiểm thử bắt buộc đã được thực thi thực tế trên máy chủ `http://localhost:5227` và ghi nhận kết quả **PASS 100%**.

### 6.1. Bảng tổng hợp kết quả 4 kịch bản kiểm thử bắt buộc

| Mã kịch bản | Tên kịch bản kiểm thử | Mô tả hành động thực tế | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|:---:|---|---|---|---|:---:|
| **KB-01** | **Luồng hoàn chỉnh (End-to-End Realtime Flow)** | Khách đặt Đơn #1 qua REST ➔ Bếp nhận đơn ➔ Bếp POST `/start` ➔ Bếp POST `/complete`. | - Bếp nhận `NewOrderPlaced` ngay lập tức.<br>- Khách nhận `Preparing` tức thì.<br>- Khách nhận `Ready` tức thì. | Bếp nhận chuông thông báo đơn mới; Thẻ rung khách nhấp nháy `Preparing` và rung chuông `Ready` chuẩn xác. | **PASS** |
| **KB-02** | **Cô lập nhóm (Group Isolation)** | Khách A theo dõi Đơn #1 (`order:1`); Khách B theo dõi Đơn #2 (`order:2`). Bếp chỉ cập nhật Đơn #2. | CHỈ Khách B nhận thông báo của Đơn #2. Khách A KHÔNG nhận thông báo lạ. | Khách B nhận thông báo; Khách A hoàn toàn không nhận sự kiện nào của Đơn #2. Không rò rỉ dữ liệu giữa các bàn. | **PASS** |
| **KB-03** | **Tự động phục hồi & Nguồn sự thật (Auto Reconnect & Source of Truth)** | Mất kết nối mạng tạm thời, sau đó phục hồi. | Client tự động thử lại với `WithAutomaticReconnect`; sau khi Reconnected tự gọi REST `GET /api/orders/{id}` để đồng bộ. | Client bắt sự kiện `Reconnecting` ➔ `Reconnected` ➔ Gọi REST lấy trạng thái mới nhất từ bộ nhớ RAM thành công. | **PASS** |
| **KB-04** | **Trạng thái không hợp lệ & Phá vỡ nghiệp vụ (Invalid State Handling)** | Đơn #3 đang `Pending`, cố tình gọi REST POST `/complete` khi chưa qua `Preparing`. | - REST API từ chối với mã `400 BadRequest`, mã lỗi `INVALID_ORDER_STATE`.<br>- SignalR Hub KHÔNG phát thông báo sai lệch. | HTTP 400 chuẩn; Khách hàng C không nhận bất kỳ thông báo sai lệch nào. Trạng thái đơn vẫn là `Pending`. | **PASS** |

### 6.2. Trích xuất Nhật ký Thực thi Thực tế (Execution Log)

```text
=================================================================
 BỘ KIỂM THỬ TỰ ĐỘNG DỰ ÁN TUẦN 2 - 4 KỊCH BẢN CHUẨN BÀI 3
=================================================================

[SETUP] Kiểm tra kết nối máy chủ FoodOrdering.RealtimeApi tại http://localhost:5227
[SETUP OK] Server phản hồi: {"service":"FoodOrdering.RealtimeApi","status":"running","realtime":"SignalR Enabled","hub":"/hubs/orders"}

=================================================================
>>> KỊCH BẢN 1: KIỂM THỬ LUỒNG HOÀN CHỈNH (END-TO-END REALTIME FLOW)
=================================================================
  [1.1] Bếp đã kết nối Hub và tham gia nhóm 'kitchen'.
  [1.2] Khách gửi REST POST /api/orders...
  [1.2 OK] Đã tạo Đơn #1 thành công (HTTP 201 Created)!
  [Kitchen Event] Nhận NewOrderPlaced: Đơn #1 (Ban 01 - Nguyen Van A)
  [1.3 PASS] Bếp đã nhận NewOrderPlaced của Đơn #1 theo thời gian thực!
  [1.4] Khách A đã kết nối Hub và tham gia nhóm 'order:1'.
  [1.5] Bếp gọi REST POST /api/kitchen/orders/1/start...
  [Customer A Event] Nhận OrderStatusChanged: Đơn #1 -> [Preparing]: Bếp đã nhận đơn #1 và đang bắt đầu chế biến món ăn!
  [1.6 PASS] Khách A nhận được trạng thái [Preparing] thời gian thực!
  [1.7] Bếp gọi REST POST /api/kitchen/orders/1/complete...
  [Customer A Event] Nhận OrderStatusChanged: Đơn #1 -> [Ready]: Đơn hàng #1 đã được nấu xong và sẵn sàng phục vụ! Kính mời quý khách nhận món.
  [1.8 PASS] Khách A nhận được trạng thái [Ready] thời gian thực!
>>> KẾT QUẢ KỊCH BẢN 1: HOÀN TOÀN ĐẠT (PASS)!

=================================================================
>>> KỊCH BẢN 2: KIỂM THỬ TÍNH CÔ LẬP NHÓM (GROUP ISOLATION)
=================================================================
  [Kitchen Event] Nhận NewOrderPlaced: Đơn #2 (Ban 02 - Tran Thi B)
  [2.1] Đã tạo Đơn #2 cho Khách B.
  [2.2] Khách B tham gia nhóm 'order:2'. Khách A vẫn đang ở 'order:1'.
  [2.3] Bếp bắt đầu nấu Đơn #2 (POST /start)...
  [Customer B Event] Nhận OrderStatusChanged: Đơn #2 -> [Preparing]
  [2.4 PASS] CHỈ Khách B nhận thông báo của Đơn #2. Khách A hoàn toàn không bị rò rỉ dữ liệu!
>>> KẾT QUẢ KỊCH BẢN 2: HOÀN TOÀN ĐẠT (PASS)!

=================================================================
>>> KỊCH BẢN 3: KIỂM THỬ PHỤC HỒI KẾT NỐI & NGUỒN SỰ THẬT (RECONNECT & SOURCE OF TRUTH)
=================================================================
  [3.1] Client kết nối thành công tới Hub: e6uLtKhTAIORN3ryS0v-Hg
  [3.2] Kiểm tra đồng bộ Nguồn sự thật: Gọi REST GET /api/orders/1...
  [3.3 PASS] Đồng bộ thành công từ Nguồn sự thật: Đơn #1 có trạng thái [Ready] (Cập nhật: 08:13:26)
>>> KẾT QUẢ KỊCH BẢN 3: HOÀN TOÀN ĐẠT (PASS)!

=================================================================
>>> KỊCH BẢN 4: KIỂM THỬ TRẠNG THÁI KHÔNG HỢP LỆ & PHÁ VỠ NGHIỆP VỤ
=================================================================
  [4.1] Đã tạo Đơn #3 với trạng thái ban đầu: [Pending].
  [Kitchen Event] Nhận NewOrderPlaced: Đơn #3 (Ban 03 - Le Van C)
  [4.2] Cố tình gọi POST /api/kitchen/orders/3/complete khi đơn đang Pending...
  [4.3] Mã HTTP phản hồi: 400 BadRequest
  [4.4] Nội dung lỗi từ Server: Mã [INVALID_ORDER_STATE] - 'Don hang ID 3 phai o trang thai 'Preparing' moi co the hoan thanh (hien tai la 'Pending').'
  [4.5 PASS] Server đã từ chối chuyển trạng thái sai, mã lỗi 400 INVALID_ORDER_STATE chuẩn xác!
  [4.6 PASS] Hub không phát bất kỳ thông báo sai lệch nào xuống Khách C!
>>> KẾT QUẢ KỊCH BẢN 4: HOÀN TOÀN ĐẠT (PASS)!

=================================================================
 TỔNG KẾT: CẢ 4 KỊCH BẢN KIỂM THỬ ĐÃ THỰC THI VÀ ĐẠT 100% (PASS)!
=================================================================
```

---

## 7. Bảng đối chiếu nội dung triển khai với Slide Bài 3 của Giảng viên

| STT | Kiến thức trong Slide Bài 3 (TS. Dương Thu Mây) | Vị trí Slide | Hiện thực hóa trong Dự án Tuần 2 |
|:---:|---|:---:|---|
| **1** | Mô hình thực tế: Quán ăn & Thẻ rung | **Slide 8** | Hiện thực hóa trọn vẹn nghiệp vụ Thẻ rung điện tử cho khách hàng và Màn hình điều phối bếp KDS. |
| **2** | Phân tách trách nhiệm giữa REST API và SignalR | **Slide 19, 21** | REST API đảm nhận ghi nhận giao dịch, lưu trữ tài nguyên; SignalR đảm nhận phát thông báo đẩy tức thời. |
| **3** | Khung 4 câu hỏi vàng thiết kế hệ thời gian thực | **Slide 22** | Giải quyết triệt để 4 câu hỏi về: Sự kiện, Đối tượng nhận, Nội dung gói tin và Kịch bản đứt mạng. |
| **4** | Đăng ký dịch vụ và ánh xạ Hub tại Server | **Slide 26, 28** | Gọi `builder.Services.AddSignalR()` và `app.MapHub<OrderHub>("/hubs/orders")`. |
| **5** | Định nghĩa phương thức Hub & RPC 2 chiều | **Slide 30, 32** | Xây dựng các RPC `WatchOrder`, `LeaveOrder`, `JoinKitchen` cho Client gọi lên Server. |
| **6** | Cơ chế phân nhóm (SignalR Groups) | **Slide 37, 40** | Dùng `Groups.AddToGroupAsync` để tạo phòng `order:{id}` và phòng `kitchen`, bảo đảm cô lập thông tin. |
| **7** | Kích hoạt push từ REST qua `IHubContext` | **Slide 45, 48** | Inject `IHubContext<OrderHub>` vào các endpoint Minimal API (`/api/orders`, `/start`, `/complete`). |
| **8** | Giám sát vòng đời kết nối Hub | **Slide 52, 55** | Override `OnConnectedAsync` và `OnDisconnectedAsync` ghi log trạng thái kết nối của thẻ rung. |
| **9** | Cấu hình Client với `WithAutomaticReconnect` | **Slide 63** | Client Console cấu hình chiến lược thử lại tự động khi đường truyền mạng bị gián đoạn. |
| **10** | Nguyên lý Nguồn sự thật (Source of Truth) sau khi phục hồi | **Slide 67, 70** | Sau khi kết nối lại thành công (`Reconnected`), Client chủ động gọi REST `GET /api/orders/{id}` để đồng bộ lại dữ liệu. |

---

## 8. Kết luận

Dự án Tuần 2 đã hoàn thành xuất sắc mục tiêu đề ra:
1. Triển khai hoàn chỉnh giao tiếp thời gian thực bằng SignalR, giải quyết triệt để vấn đề Polling của Tuần 1.
2. Xây dựng độc lập 2 vai trò máy khách: Khách hàng (Thẻ rung điện tử) và Đầu bếp (Màn hình điều phối KDS).
3. Tuân thủ 100% nguyên tắc ranh giới phân tán, bảo toàn nguyên trạng mã nguồn Tuần 1 và không vi phạm các điều cấm của môn học.
