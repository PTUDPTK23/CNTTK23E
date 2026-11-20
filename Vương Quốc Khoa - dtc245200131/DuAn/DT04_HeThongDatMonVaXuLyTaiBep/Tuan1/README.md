# HỆ THỐNG ĐẶT MÓN VÀ XỬ LÝ TẠI BẾP (FOOD ORDERING AND KITCHEN PROCESSING SYSTEM)

Dự án thực hành môn **Phát triển ứng dụng phân tán** — Minh họa mô hình kiến trúc Client–Server tương tác qua RESTful API theo chuẩn **Bài 2**.

---

## 1. Giới thiệu đề tài

### 1.1. Bối cảnh nghiệp vụ
Trong các nhà hàng và quán ăn hiện đại, quy trình gọi món và chế biến đóng vai trò cốt lõi:
- **Khách hàng tại bàn**: Xem thực đơn hiện có, giá cả và trạng thái còn món, sau đó tạo đơn đặt món gửi trực tiếp tới hệ thống.
- **Nhân viên nhà bếp**: Theo dõi danh sách đơn hàng được gửi vào, nhận đơn chuyển sang trạng thái đang chế biến (`Preparing`), và khi nấu xong chuyển sang trạng thái sẵn sàng phục vụ (`Ready`).

### 1.2. Lý do lựa chọn kiến trúc Client–Server RESTful
- **Phân tách trách nhiệm (Separation of Concerns)**: Client (máy khách - thiết bị tại bàn hoặc tablet nhân viên) chỉ quan tâm đến giao diện tương tác và gửi/nhận dữ liệu. Server (máy chủ trung tâm) đảm nhận toàn bộ việc kiểm tra tính hợp lệ dữ liệu, tính toán hóa đơn, duy trì trạng thái đơn hàng.
- **Phi trạng thái (Stateless - Ở cấp độ giao thức và phiên làm việc)**:
  - Máy chủ không lưu trữ trạng thái phiên làm việc (session state / conversational state) của máy khách giữa các HTTP Request độc lập. Mỗi request gửi từ Client lên Server phải tự chứa đầy đủ các tham số và dữ liệu cần thiết để Server xử lý trọn vẹn mà không cần nhớ Client đó là ai từ request trước.
  - *Lưu ý quan trọng*: Tính chất "Stateless" trong kiến trúc REST không đồng nghĩa với việc Server không được lưu dữ liệu. Server vẫn quản lý và duy trì trạng thái của các tài nguyên hệ thống (Resource State) — trong dự án này là danh sách thực đơn và danh sách đơn hàng được lưu trữ trong bộ nhớ RAM (`List<T>` In-Memory).
- **Khả năng mở rộng và độc lập nền tảng**: Giao tiếp chuẩn hóa qua giao thức HTTP và định dạng JSON giúp nhiều loại máy khách khác nhau (Console, Web, Mobile) kết nối chung vào cùng một hệ thống API.

---

## 2. Giới hạn phạm vi kiến thức (Chuẩn Bài 2)

Dự án được xây dựng tuân thủ nghiêm ngặt **phạm vi kiến thức Bài 2: RESTful API cho tương tác Client–Server**:
- **Ngôn ngữ & Nền tảng**: C# 14, .NET 10 (`net10.0`).
- **Server API**: ASP.NET Core Minimal API (`MapGet`, `MapPost`).
- **Client**: Ứng dụng Console .NET 10 sử dụng `HttpClient` và thư viện chuẩn `System.Net.Http.Json`.
- **Lưu trữ dữ liệu**: Lưu trữ trong bộ nhớ RAM (`List<T>` In-Memory) theo đúng tinh thần bài thực hành Bài 2.
- **Mô hình DTO**: Định nghĩa độc lập phía Client và phía Server, chuyển đổi dữ liệu qua lại dưới định dạng JSON.
- **Xử lý lỗi**: Chuẩn hóa cấu trúc thông báo lỗi qua record `ApiError(string Code, string Message)`.
- **Độ bền mạng**: Xử lý ngoại lệ `HttpRequestException` tại Client khi máy chủ không phản hồi, đảm bảo ứng dụng không crash đột ngột.

> **Cam kết giới hạn**: Tuyệt đối **không** sử dụng các công nghệ nâng cao ngoài phạm vi Bài 2 như Database/Entity Framework Core, Message Broker (RabbitMQ, Kafka), SignalR/WebSocket, Docker/Kubernetes, Microservices hay xác thực nâng cao (JWT/OAuth).

---

## 3. Cấu trúc thư mục dự án

```text
Vương Quốc Khoa - dtc245200131/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/
├── FoodOrderingKitchen.slnx                 # Solution quản lý các project (.NET 10)
├── .gitignore                               # Quy tắc bỏ qua file tạm build / IDE
├── requests.http                            # Kịch bản kiểm thử API REST Client (13 ca test)
├── README.md                                # Tài liệu kỹ thuật, đặc tả API & báo cáo kiểm thử
└── src/
    ├── FoodOrdering.Api/                    # Máy chủ RESTful API (Minimal API)
    │   ├── FoodOrdering.Api.csproj
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── Properties/
    │   │   └── launchSettings.json
    │   └── Program.cs                       # Toàn bộ mã nguồn Server, 8 endpoints & Server DTOs
    │
    └── FoodOrdering.Client/                 # Ứng dụng máy khách Console
        ├── FoodOrdering.Client.csproj
        └── Program.cs                       # Mã nguồn Client (8 bước nghiệp vụ & Client DTOs)
```

---

## 4. Hướng dẫn cài đặt và chạy ứng dụng

### 4.1. Yêu cầu môi trường
- .NET SDK 10.0 trở lên.
- PowerShell hoặc Command Prompt trên Windows.

### 4.2. Biên dịch toàn bộ Solution
Mở terminal tại thư mục gốc của dự án:
```powershell
cd "Vương Quốc Khoa - dtc245200131/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1"
dotnet build FoodOrderingKitchen.slnx
```

### 4.3. Khởi chạy Server (FoodOrdering.Api)
Khởi động máy chủ API tại cổng mặc định `5227` (được cấu hình trong `launchSettings.json`):
```powershell
# Chạy máy chủ (Kestrel tự động lắng nghe cổng http://localhost:5227 theo profile http trong launchSettings.json):
dotnet run --project src/FoodOrdering.Api

# Hoặc chỉ định rõ cổng 5227 qua tham số --urls:
dotnet run --project src/FoodOrdering.Api --urls http://localhost:5227
```
*Khi khởi động thành công, màn hình sẽ hiển thị `Now listening on: http://localhost:5227`.*

### 4.4. Khởi chạy Client (FoodOrdering.Client)
Mở một cửa sổ terminal mới và kết nối đến Server tại cổng `5227`:
```powershell
# Chạy Client kết nối trực tiếp đến cổng thực tế 5227:
dotnet run --project src/FoodOrdering.Client -- http://localhost:5227
```

### 4.5. Kiểm thử bằng file `requests.http`
Sử dụng extension **REST Client** (Huachao Mao) trong Visual Studio Code:
1. Mở file `requests.http` tại thư mục dự án `Tuan1`.
2. Kiểm tra biến địa chỉ đầu file trỏ đúng `http://localhost:5227`:
   ```http
   @baseUrl = http://localhost:5227
   ```
3. Bấm nút **Send Request** trên từng khối request để gửi yêu cầu và quan sát mã phản hồi HTTP kèm dữ liệu JSON.

---

## 5. Bảng đặc tả RESTful API

Toàn bộ hệ thống máy chủ bao gồm **8 endpoints** được phân chia theo từng nhóm nghiệp vụ:

| STT | HTTP Method | Route URI | Mô tả nghiệp vụ | DTO Request | DTO Response | Status Code thành công | Status Code & ApiError khi lỗi |
|:---:|:---:|:---|:---|:---|:---|:---:|:---|
| 1 | `GET` | `/` | Kiểm tra trạng thái hoạt động của máy chủ | *Không* | `{ service, status }` | `200 OK` | — |
| 2 | `GET` | `/api/menu-items` | Lấy danh sách toàn bộ món ăn trong thực đơn | *Không* | `List<MenuItemDto>` | `200 OK` | — |
| 3 | `GET` | `/api/menu-items/{id:int}` | Lấy thông tin chi tiết một món ăn theo ID | *Không* | `MenuItemDto` | `200 OK` | `404 Not Found`<br>`ITEM_NOT_FOUND` |
| 4 | `POST` | `/api/orders` | Khách hàng tạo đơn đặt món mới | `CreateOrderRequest` | `OrderDto`<br>*(Location Header)* | `201 Created` | `400 Bad Request`<br>`INVALID_ORDER`<br>`415 Unsupported Media Type` |
| 5 | `GET` | `/api/orders/{id:int}` | Xem chi tiết thông tin đơn hàng theo ID | *Không* | `OrderDto` | `200 OK` | `404 Not Found`<br>`ORDER_NOT_FOUND` |
| 6 | `GET` | `/api/kitchen/orders` | Nhân viên bếp xem danh sách toàn bộ đơn hàng | *Không* | `List<OrderDto>` | `200 OK` | — |
| 7 | `POST` | `/api/kitchen/orders/{id:int}/start` | Bếp xác nhận bắt đầu chế biến đơn hàng | *Không* | `OrderDto` (Status: Preparing) | `200 OK` | `404 ORDER_NOT_FOUND`<br>`400 INVALID_ORDER_STATE` |
| 8 | `POST` | `/api/kitchen/orders/{id:int}/complete` | Bếp xác nhận đã nấu xong món ăn | *Không* | `OrderDto` (Status: Ready) | `200 OK` | `404 ORDER_NOT_FOUND`<br>`400 INVALID_ORDER_STATE` |

---

## 6. Bảng kết quả kiểm thử thực tế

> **Phân loại các ca kiểm thử**:
> - **TC-01 đến TC-12**: Là các kịch bản kiểm thử API qua giao thức HTTP (được định nghĩa trong 13 request của file `requests.http`, bao gồm cả request số 6 sử dụng cú pháp Response Chaining của VS Code REST Client và request số 13 kiểm tra thiếu Content-Type). Toàn bộ các request này đã được thực thi và xác minh thực tế tới máy chủ tại `http://localhost:5227`.
> - **TC-13**: Ca kiểm thử tích hợp toàn diện của ứng dụng Console Client (`FoodOrdering.Client`) chạy 8 bước tuần tự qua `HttpClient`.
> - **TC-14**: Ca kiểm thử độ bền mạng của ứng dụng Console Client (`FoodOrdering.Client`) khi Server tắt, bắt ngoại lệ `HttpRequestException` và in thông báo thân thiện không để ứng dụng bị crash.

| Mã ca test | Kịch bản kiểm thử | Endpoint | Kỳ vọng | Kết quả thực tế | Trạng thái |
|:---:|:---|:---|:---|:---|:---:|
| **TC-01** | Kiểm tra trạng thái server | `GET /` | `200 OK`, JSON status | `200 OK`, `{"service":"FoodOrdering.Api","status":"running"}` | **PASS** |
| **TC-02** | Xem danh sách thực đơn | `GET /api/menu-items` | `200 OK`, 4 món ăn mẫu | `200 OK`, Trả về đủ 4 món kèm trạng thái còn/hết | **PASS** |
| **TC-03** | Xem chi tiết món hợp lệ | `GET /api/menu-items/1` | `200 OK`, thông tin món 1 | `200 OK`, Món Pho Bo Tai Lan (55.000 VNĐ) | **PASS** |
| **TC-04** | Xem chi tiết món không tồn tại | `GET /api/menu-items/999` | `404 Not Found`, `ITEM_NOT_FOUND` | `404 Not Found`, `{"code":"ITEM_NOT_FOUND", ...}` | **PASS** |
| **TC-05** | Tạo đơn hàng hợp lệ | `POST /api/orders` | `201 Created`, header `Location` | `201 Created`, `Location: /api/orders/1`, Tổng 160.000 VNĐ | **PASS** |
| **TC-06** | Tạo đơn hàng chứa món đã hết | `POST /api/orders` | `400 Bad Request`, `INVALID_ORDER` | `400 Bad Request`, thông báo Bánh Mì Chảo hết hàng | **PASS** |
| **TC-07** | Tạo đơn hàng có số lượng <= 0 | `POST /api/orders` | `400 Bad Request`, `INVALID_ORDER` | `400 Bad Request`, thông báo số lượng phải > 0 | **PASS** |
| **TC-08** | Tạo đơn thiếu Content-Type | `POST /api/orders` | `415 Unsupported Media Type` | `415 Unsupported Media Type` (mã trạng thái 415) | **PASS** |
| **TC-09** | Bếp xem danh sách đơn | `GET /api/kitchen/orders` | `200 OK`, danh sách đơn | `200 OK`, hiển thị các đơn kèm trạng thái hiện tại | **PASS** |
| **TC-10** | Bếp bắt đầu chế biến | `POST .../start` | `200 OK`, Status: `Preparing` | `200 OK`, Trạng thái đơn chuyển sang `Preparing` | **PASS** |
| **TC-11** | Bếp hoàn thành chế biến | `POST .../complete` | `200 OK`, Status: `Ready` | `200 OK`, Trạng thái đơn chuyển sang `Ready` | **PASS** |
| **TC-12** | Phá lỗi: Hoàn thành đơn đã Ready | `POST .../complete` | `400 Bad Request`, `INVALID_ORDER_STATE` | `400 Bad Request`, từ chối vì đơn không ở Preparing | **PASS** |
| **TC-13** | Khách chạy Client tuần tự (8 bước) | Toàn bộ quy trình | Hoàn thành trôi chảy không lỗi | Đặt món thành công, bếp xử lý, phá lỗi bắt chuẩn | **PASS** |
| **TC-14** | Client xử lý khi Server tắt | Server OFF | Bắt `HttpRequestException`, không crash | In thông báo mất kết nối thân thiện, thoát êm đẹp | **PASS** |

### 6.1. Nhật ký chạy thực tế của Console Client (Một phiên chạy độc lập hoàn chỉnh):
> **Ghi chú về mã đơn hàng trong nhật ký**:
> Trong một lần chạy từ máy chủ mới khởi động (danh sách `orders` ban đầu rỗng), Client thực hiện tạo đơn hàng đầu tiên (Đơn `#1`) và toàn bộ các bước sau (khách tra cứu, bếp nhận nấu, bếp hoàn thành, phá lỗi nghiệp vụ) đều tương tác xuyên suốt trên chính đơn hàng `#1` này.
> Nếu Server tiếp tục chạy và Client được gọi thêm các lần sau, số lượng đơn hàng và mã số đơn mới (`#2`, `#3`,...) sẽ tăng lũy tiến do dữ liệu được lưu trong bộ nhớ RAM (`List<OrderDto>`) trong suốt thời gian tiến trình máy chủ hoạt động.

```text
=================================================================
  HỆ THỐNG ĐẶT MÓN & XỬ LÝ TẠI BẾP - CONSOLE CLIENT (.NET 10)
  Kết nối tới Server: http://localhost:5227
=================================================================

>>> [BƯỚC 1] Khách hàng xem danh sách thực đơn (GET /api/menu-items):
  [1] Pho Bo Tai Lan - 55,000 VNĐ (Còn món)
      Mô tả: Dac san Ha Noi thom ngon gia truyen
  [2] Bun Cha Ha Noi - 50,000 VNĐ (Còn món)
      Mô tả: Thit nuong than hoa dam vi kem nuoc cham
  [3] Com Rang Dua Bo - 45,000 VNĐ (Còn món)
      Mô tả: Com rang gion rum, dua chua bo xao dam da
  [4] Banh Mi Chao - 40,000 VNĐ (Hết món)
      Mô tả: Day du pate, trung op la, xuc xich nong hoi

>>> [BƯỚC 2] Xem chi tiết món ăn ID = 1 (GET /api/menu-items/1):
  Món: Pho Bo Tai Lan | Đơn giá: 55,000 VNĐ | Trạng thái: Sẵn sàng

>>> [BƯỚC 3] Khách hàng gửi yêu cầu đặt món (POST /api/orders):
  [THÀNH CÔNG] Đã tạo đơn hàng #1!
  Khách: Anh Nguyen | Bàn: Ban 05
  Tổng tiền: 160,000 VNĐ
  Trạng thái ban đầu: Pending
  Đường dẫn theo dõi (Location): /api/orders/1

>>> [BƯỚC 4] Kiểm tra lại đơn hàng #1 (GET /api/orders/1):
  Đơn #1 hiện đang ở trạng thái: [Pending]
    - Pho Bo Tai Lan x2 = 110,000 VNĐ
    - Bun Cha Ha Noi x1 = 50,000 VNĐ

>>> [BƯỚC 5] Bếp xem danh sách đơn hàng (GET /api/kitchen/orders):
  Số lượng đơn hiện có: 1
  * Đơn #1 (Ban 05 - Anh Nguyen) -> Trạng thái: Pending

>>> [BƯỚC 6] Bếp bắt đầu chế biến đơn #1 (POST /api/kitchen/orders/1/start):
  [BẾP ĐÃ NHẬN ĐƠN] Trạng thái đơn #1 đã chuyển sang: [Preparing]

>>> [BƯỚC 7] Bếp chế biến xong đơn #1 (POST /api/kitchen/orders/1/complete):
  [MÓN ĐÃ NẤU XONG] Đơn #1 đã sẵn sàng phục vụ: [Ready]!

=================================================================
>>> [BƯỚC 8] THỬ NGHIỆM PHÁ LỖI VÀ XỬ LÝ LỖI (ERROR HANDLING):

  [8a] Thử bấm 'hoàn thành' lần nữa cho đơn đã Ready:
  Mã phản hồi HTTP: 400 BadRequest
  [Lỗi API] Mã: INVALID_ORDER_STATE | Thông báo: Don hang ID 1 phai o trang thai 'Preparing' moi co the hoan thanh (hien tai la 'Ready').

  [8b] Thử đặt món đã hết hàng (Món ID = 4):
  Mã phản hồi HTTP: 400 BadRequest
  [Lỗi API] Mã: INVALID_ORDER | Thông báo: Mon an 'Banh Mi Chao' hien tai da het hang.

  [8c] Thử tìm món ăn không tồn tại (ID = 999):
  Mã phản hồi HTTP: 404 NotFound
  [Lỗi API] Mã: ITEM_NOT_FOUND | Thông báo: Khong tim thay mon an voi ID = 999.

=================================================================
  TẤT CẢ CÁC NẤC THỰC THI VÀ XỬ LÝ LỖI ĐÃ HOÀN TẤT THÀNH CÔNG!
=================================================================
```

---

## 7. Giải đáp 3 câu hỏi lý thuyết cốt lõi của Bài 2

### Câu hỏi 1: Tại sao RESTful API nên dùng danh từ số nhiều cho URI thay vì động từ?
- **Bản chất hướng tài nguyên (Resource-Oriented)**: Trong kiến trúc REST, URI đóng vai trò định danh **tài nguyên** (Resources) chứ không biểu diễn hành động. Ví dụ: `/api/menu-items` đại diện cho tập hợp danh mục món ăn, `/api/orders` đại diện cho tập hợp đơn hàng.
- **Tận dụng HTTP Methods**: Hành động xử lý trên tài nguyên đã được chuẩn hóa bởi các phương thức HTTP (HTTP Verbs):
  - `GET /api/orders`: Đọc danh sách đơn hàng (không dùng `/api/getOrders`).
  - `POST /api/orders`: Tạo mới đơn hàng (không dùng `/api/createOrder`).
  - `DELETE /api/orders/1`: Xóa đơn hàng (không dùng `/api/deleteOrder/1`).
- **Lợi ích**: Giúp cấu trúc API gọn gàng, mang tính phổ quát, trực quan và dễ dự đoán đối với bất kỳ lập trình viên hoặc hệ thống tích hợp nào.

### Câu hỏi 2: Ý nghĩa của việc tách biệt DTO giữa Client và Server trong hệ phân tán là gì? Tại sao Client không nên reference project Api?
- **Độc lập và Giảm phụ thuộc (Loose Coupling)**: Trong hệ thống phân tán thực tế, Client và Server là hai tiến trình độc lập, có thể được phát triển bởi các nhóm khác nhau, bằng các ngôn ngữ khác nhau (ví dụ: Client viết bằng TypeScript/React, Server viết bằng C# Minimal API). Nếu Client trực tiếp reference dll/project của Server, hai thành phần bị gắn chặt chẽ (tight coupling), vi phạm nguyên lý ranh giới phân tán.
- **An toàn bảo mật (Information Hiding)**: Server có thể có các trường dữ liệu nội bộ (thuật toán tính giá, logic quản lý trạng thái, trường kiểm tra) mà Client không được phép truy cập hay can thiệp.
- **Hợp đồng dữ liệu qua mạng (Network Contract via JSON)**: Sự liên kết duy nhất giữa Client và Server là định dạng JSON trao đổi qua HTTP. Mỗi bên tự định nghĩa DTO phù hợp với mục đích sử dụng của riêng mình để parse JSON đó.

### Câu hỏi 3: Khi nào nên trả về HTTP 201 Created thay vì HTTP 200 OK, và tại sao cần kèm header `Location`?
- **Thời điểm sử dụng HTTP 201 Created (theo chuẩn RFC 9110 §15.3.2)**:
  - Mã trạng thái `201 Created` được trả về khi một yêu cầu HTTP (thông thường là `POST`) được thực hiện thành công và dẫn đến việc **tạo mới một hoặc nhiều tài nguyên trên máy chủ**.
  - **Phân biệt với `200 OK`**: `200 OK` thông báo yêu cầu đã xử lý thành công nói chung (ví dụ truy vấn đọc dữ liệu qua `GET`, hoặc cập nhật trạng thái đơn hàng như `POST .../start` mà không sinh ra tài nguyên mới). Trong khi đó, `201 Created` mang ngữ nghĩa (semantic) rõ ràng và chính xác hơn: khẳng định một tài nguyên mới độc lập đã được tạo lập thành công và được cấp phát định danh trong hệ thống.
- **Ý nghĩa của Header `Location` (theo RFC 9110 §15.3.2)**:
  - Chuẩn RFC 9110 quy định khi trả về phản hồi `201 Created`, máy chủ **nên gửi kèm (SHOULD send)** tiêu đề `Location` để xác định URI định danh của tài nguyên chính vừa được tạo (ví dụ: `Location: /api/orders/1`).
  - **Mục đích thực tế**:
    - Tiêu đề `Location` cung cấp trực tiếp địa chỉ URI của tài nguyên mới tạo trong phần header chuẩn của HTTP.
    - Nhờ đó, Client có thể dễ dàng lấy được đường dẫn này để tiếp tục thực hiện các truy vấn tiếp theo (chẳng hạn như gửi `GET /api/orders/1` để theo dõi tiến độ xử lý của đơn hàng) mà không cần phụ thuộc vào việc bóc tách các trường dữ liệu bên trong phần thân (body) JSON.
