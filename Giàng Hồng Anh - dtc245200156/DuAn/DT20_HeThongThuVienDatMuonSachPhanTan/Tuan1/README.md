# BÀI 2 – RESTFUL API CHO ĐỀ TÀI 20: HỆ THỐNG THƯ VIỆN VÀ ĐẶT MƯỢN SÁCH PHÂN TÁN

**Môn học:** Phát triển ứng dụng phân tán  
**Lớp:** K23.K1.D2.2627.CNTT K23E  
**Sinh viên thực hiện:** Giàng Hồng Anh  
**Mã sinh viên:** dtc245200156  

---

## 1. Giới thiệu đề tài
- **Tên đề tài:** Đề tài 20 – Hệ thống thư viện và đặt mượn sách phân tán.
- **Mô tả bài toán:** Xây dựng hệ thống phục vụ việc quản lý thư viện sách và nhu cầu đặt mượn sách của bạn đọc trong môi trường phân tán. Hệ thống cho phép người dùng tra cứu danh mục sách, kiểm tra số bản sách còn khả dụng, gửi yêu cầu đặt mượn và theo dõi tiến trình xử lý yêu cầu mượn sách.

## 2. Mục tiêu Bài 2
- Xây dựng dịch vụ **RESTful API** chuẩn mực bằng **ASP.NET Core Minimal API** trên nền tảng **.NET 10**.
- Thiết kế hệ thống dữ liệu In-Memory thread-safe mô phỏng quản lý sách và các yêu cầu đặt mượn.
- Hiện thực hoá các nguyên tắc nền tảng của REST: Resource-oriented URI (danh từ), HTTP Methods (GET, POST), Stateless, phân tách DTO Request/Response, chuẩn hoá cấu trúc lỗi với `ApiError`, và trả về header `Location` cho mã `201 Created`.
- Xây dựng ứng dụng **Library.Client** bằng **HttpClient** và `System.Net.Http.Json` thực hiện toàn bộ kịch bản nghiệp vụ và xử lý lỗi mượt mà mà không bị crash.
- Cung cấp bộ kiểm thử tự động hoá qua file `requests.http` và các câu lệnh `curl.exe` trên Windows PowerShell.

## 3. Công nghệ sử dụng
- **Ngôn ngữ:** C# 13 / .NET 10 (`10.0.403`).
- **Framework API:** ASP.NET Core Minimal API.
- **Client:** Console Application, `System.Net.Http.HttpClient`, `System.Net.Http.Json`.
- **Định dạng dữ liệu:** JSON (`System.Text.Json`).
- **Kiểm thử API:** REST Client extension (`requests.http`) và Windows PowerShell `curl.exe`.
- **Hệ thống quản trị mã nguồn:** Git & GitHub.

## 4. Kiến trúc hệ thống
Hệ thống được thiết kế theo định hướng kiến trúc phân tán hướng thông điệp (Event-Driven / Messaging Architecture):

```text
[ Client (Console / Web) ]
           │ (HTTP / JSON RESTful API)
           ▼
   [ Library.Api ]
     │        │
     │        ▼
     │   [ RabbitMQ Message Broker ]  ──(Giai đoạn sau)
     │                │
     │                ▼
     │     [ Reservation Worker ]     ──(Giai đoạn sau)
     │                │
     ▼                ▼
   [ Database (PostgreSQL / SQL Server) ]
```

- **Phạm vi Bài 2:** Tập trung hoàn thiện giao tiếp Client ↔ Library.Api qua RESTful API chuẩn. Dữ liệu được quản lý an toàn trong bộ nhớ (In-Memory Data Store) với khoá đồng bộ đa luồng (`lock`), tạo tiền đề sẵn sàng tích hợp RabbitMQ và Worker ở các bài tiếp theo.

## 5. Cấu trúc thư mục
Solution được tổ chức chuẩn theo cấu trúc phân tầng:

```text
LibrarySystem/
│
├── LibrarySystem.slnx                  # Solution file (.NET 10 XML format)
├── .gitignore                          # File loai tru file rac (bin, obj, vs...)
├── README.md                           # Tai lieu huong dan toan dien
├── requests.http                       # File kiem thu API voi REST Client
│
└── src/
    ├── Library.Api/                    # RESTful Minimal API Server
    │   ├── Program.cs                  # Endpoints, Models, DTOs & In-Memory Store
    │   └── Library.Api.csproj
    │
    └── Library.Client/                 # HTTP Console Client
        ├── Program.cs                  # Kich ban goi API, kiem tra HTTP Status & ApiError
        └── Library.Client.csproj
```

## 6. Mô tả các DTO (Data Transfer Objects)

### 6.1. BookDto
Đại diện cho thông tin một cuốn sách trong thư viện:
- `Id` (`int`): Mã định danh duy nhất của cuốn sách.
- `Title` (`string`): Tên cuốn sách.
- `Author` (`string`): Tác giả.
- `ISBN` (`string`): Mã tiêu chuẩn quốc tế cho sách.
- `Category` (`string`): Thể loại sách.
- `Description` (`string`): Tóm tắt nội dung sách.
- `TotalCopies` (`int`): Tổng số bản in mà thư viện sở hữu.
- `AvailableCopies` (`int`): Số bản sách hiện đang có sẵn trên kệ để mượn.

### 6.2. ReservationDto
Đại diện cho yêu cầu đặt mượn sách của độc giả:
- `Id` (`Guid`): Mã định danh duy nhất của yêu cầu đặt mượn (Server tự sinh).
- `BookId` (`int`): Mã cuốn sách được đặt mượn.
- `UserId` (`string`): Mã sinh viên / tài khoản người đặt.
- `UserName` (`string`): Họ tên người đặt mượn.
- `Status` (`string`): Trạng thái xử lý (`Pending`, `Reserved`, `Rejected`, `Borrowed`, `Returned`).
- `RequestedAt` (`DateTimeOffset`): Thời điểm gửi yêu cầu đặt mượn (UTC, Server tự sinh).

### 6.3. CreateReservationRequest
Dữ liệu từ Body JSON mà người dùng gửi lên khi yêu cầu đặt mượn:
- `UserId` (`string`): Bắt buộc, không để trống.
- `UserName` (`string`): Bắt buộc, không để trống.
*(Lưu ý: `BookId` được truyền trực tiếp qua URI, không gửi trong body; `Id`, `Status`, `RequestedAt` do Server quản lý).*

### 6.4. ApiError
Cấu trúc phản hồi lỗi nghiệp vụ thống nhất toàn hệ thống:
```json
{
  "code": "BOOK_NOT_FOUND",
  "message": "Khong tim thay sach."
}
```

## 7. Danh sách API

| STT | Phương thức | URI | Mô tả chức năng | Mã trạng thái thành công | Mã lỗi khả dĩ |
|:---:|:---:|:---|:---|:---:|:---:|
| 1 | `GET` | `/` | Kiểm tra trạng thái hoạt động của Library.Api | `200 OK` | - |
| 2 | `GET` | `/books` | Lấy danh sách toàn bộ sách trong thư viện | `200 OK` | - |
| 3 | `GET` | `/books/{id}` | Lấy thông tin chi tiết của 1 cuốn sách | `200 OK` | `404 Not Found` |
| 4 | `POST` | `/books/{bookId}/reservations` | Gửi yêu cầu đặt mượn một cuốn sách | `201 Created` | `400 Bad Request`, `404 Not Found` |
| 5 | `GET` | `/reservations/{id}` | Tra cứu thông tin yêu cầu đặt mượn theo Guid | `200 OK` | `404 Not Found` |
| 6 | `GET` | `/users/{userId}/reservations` | Lấy danh sách yêu cầu đặt mượn của một người dùng | `200 OK` | - |

## 8. Bảng HTTP Status Code

| HTTP Status Code | Ý nghĩa trong hệ thống | Trường hợp áp dụng |
|:---|:---|:---|
| **`200 OK`** | Yêu cầu đọc dữ liệu thành công | Khi `GET /books`, `GET /books/{id}`, `GET /reservations/{id}`, `GET /users/{userId}/reservations` thành công. |
| **`201 Created`** | Tài nguyên mới đã được tạo thành công | Khi `POST /books/{bookId}/reservations` thành công. Kèm theo header `Location` chỉ định URI của tài nguyên vừa tạo (`/reservations/{id}`). |
| **`400 Bad Request`** | Dữ liệu yêu cầu không hợp lệ hoặc vi phạm nghiệp vụ | Thiếu `UserId`/`UserName` (`INVALID_RESERVATION`) hoặc sách đã hết bản khả dụng (`BOOK_UNAVAILABLE`). |
| **`404 Not Found`** | Không tìm thấy tài nguyên | Tra cứu sách không có (`BOOK_NOT_FOUND`) hoặc đặt mượn mã sách không tồn tại, hoặc mã reservation không tồn tại (`RESERVATION_NOT_FOUND`). |

## 9. Hướng dẫn cài đặt
Yêu cầu môi trường:
- Đã cài đặt **.NET 10 SDK** (kiểm tra bằng lệnh `dotnet --version`).
- Trình soạn thảo: Visual Studio Code hoặc Antigravity IDE.

Clone hoặc mở thư mục bài tập:
```bash
cd "D:/Bai2/CNTTK23E/Giàng Hồng Anh - dtc245200156/Bai2/LibrarySystem"
```

Khôi phục dependencies và biên dịch Solution:
```bash
dotnet build
```

## 10. Hướng dẫn chạy API
Chạy Server API trên cổng `http://localhost:5000`:
```bash
dotnet run --project src/Library.Api --urls http://localhost:5000
```
Server sẽ hiển thị thông báo lắng nghe:
```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

## 11. Hướng dẫn chạy Client
Mở một cửa sổ Terminal khác và thực hiện chạy ứng dụng Client:
```bash
dotnet run --project src/Library.Client -- http://localhost:5000
```
Client sẽ tự động thực hiện tuần tự kịch bản:
1. `GET /books`: In bảng danh mục sách.
2. `GET /books/1`: In chi tiết cuốn sách ID = 1.
3. `POST /books/1/reservations`: Gửi yêu cầu đặt mượn cho bạn đọc `dtc245200156`.
4. Đọc `Location` header và `GET` lại để kiểm chứng trạng thái `Pending`.
5. `GET /users/dtc245200156/reservations`: In danh sách các đơn đã đặt của người dùng.
6. Chạy bộ kiểm thử lỗi nghiệp vụ (400, 404) mượt mà không crash.

## 12. Hướng dẫn test bằng curl.exe trên Windows PowerShell

Mở Windows PowerShell và thực hiện các lệnh sau:

### 12.1. Tra cứu danh sách sách (GET /books)
```powershell
curl.exe -i http://localhost:5000/books
```

### 12.2. Tra cứu chi tiết sách tồn tại (GET /books/1)
```powershell
curl.exe -i http://localhost:5000/books/1
```

### 12.3. Tra cứu sách không tồn tại (GET /books/999 -> 404)
```powershell
curl.exe -i http://localhost:5000/books/999
```

### 12.4. Đặt mượn sách hợp lệ (POST /books/1/reservations -> 201)
```powershell
'{"userId":"dtc245200156","userName":"Giang Hong Anh"}' | curl.exe -s -i -X POST http://localhost:5000/books/1/reservations -H "Content-Type: application/json" -d '@-'
```

### 12.5. Đặt mượn thiếu UserId (POST -> 400 INVALID_RESERVATION)
```powershell
'{"userId":"","userName":"Giang Hong Anh"}' | curl.exe -s -i -X POST http://localhost:5000/books/1/reservations -H "Content-Type: application/json" -d '@-'
```

### 12.6. Đặt mượn sách không tồn tại (POST /books/999/reservations -> 404 BOOK_NOT_FOUND)
```powershell
'{"userId":"dtc245200156","userName":"Giang Hong Anh"}' | curl.exe -s -i -X POST http://localhost:5000/books/999/reservations -H "Content-Type: application/json" -d '@-'
```

### 12.7. Đặt mượn sách hết bản khả dụng (Book 3 có AvailableCopies = 0 -> 400 BOOK_UNAVAILABLE)
```powershell
'{"userId":"dtc245200156","userName":"Giang Hong Anh"}' | curl.exe -s -i -X POST http://localhost:5000/books/3/reservations -H "Content-Type: application/json" -d '@-'
```

### 12.8. Tra cứu yêu cầu đặt mượn theo Guid
*(Thay guid bằng Id thực tế nhận được ở bước 12.4)*
```powershell
curl.exe -i http://localhost:5000/reservations/<reservation-id-guid>
```

### 12.9. Tra cứu danh sách yêu cầu của một người dùng
```powershell
curl.exe -i http://localhost:5000/users/dtc245200156/reservations
```

## 13. Hướng dẫn test bằng requests.http
1. Cài đặt tiện ích mở rộng **REST Client** trên VS Code / Antigravity IDE.
2. Mở file `requests.http`.
3. Bấm vào nút `Send Request` nằm ngay trên mỗi request để gửi kiểm thử trực quan và quan sát response bên cửa sổ cạnh bên.

## 14. Các kết quả kiểm thử mong đợi
- Tất cả các endpoint đều trả đúng định dạng JSON UTF-8.
- Các yêu cầu hợp lệ trả mã `200` hoặc `201`.
- Header `Location` xuất hiện đầy đủ trong phản hồi `201 Created`.
- Khi xảy ra lỗi dữ liệu hoặc nghiệp vụ, phản hồi luôn kèm theo cấu trúc `ApiError` chuẩn mực (`code`, `message`).
- Số bản khả dụng (`AvailableCopies`) được trừ chính xác sau khi một yêu cầu mượn thành công.

## 15. Những giới hạn hiện tại của hệ thống
- **Lưu trữ In-Memory:** Dữ liệu sách và đơn đặt mượn được lưu trong RAM, sẽ mất khi khởi động lại ứng dụng.
- **Xử lý đơn luồng trong tiến trình:** Chưa có RabbitMQ và Reservation Worker tách rời để xếp hàng xử lý bất đồng bộ quy mô lớn.
- **Chưa có chứng thực / phân quyền (Authentication / Authorization):** Chưa sử dụng JWT/OAuth2 để bảo vệ các endpoint.

## 16. Hướng mở rộng tích hợp RabbitMQ, Reservation Worker và Database
- **Tích hợp RabbitMQ:** Khi Client gửi `POST /books/{bookId}/reservations`, Library.Api chỉ đóng vai trò Producer xuất bản sự kiện `ReservationSubmittedEvent` lên RabbitMQ Exchange (`library.reservations`), trả về ngay cho Client mã đơn trạng thái `Pending`.
- **Reservation Worker:** Xây dựng một .NET Worker Service đóng vai trò Consumer lắng nghe Queue từ RabbitMQ, thực hiện kiểm tra sâu trong Database, đối soát kho sách và chuyển trạng thái đơn thành `Reserved` hoặc `Rejected`.
- **Database:** Tích hợp Entity Framework Core với PostgreSQL hoặc SQL Server, sử dụng kỹ thuật Optimistic Concurrency Control hoặc Pessimistic Locking để tránh tranh chấp khi nhiều người cùng đặt cuốn sách cuối cùng.

---

## BỔ SUNG: TRẢ LỜI 3 CÂU HỎI GIẢI THÍCH CHUYÊN SÂU

### Câu hỏi 1: Vì sao URI sử dụng danh từ, còn hành động do HTTP Method đảm nhận?
- **Giải thích:** Trong kiến trúc REST (Representational State Transfer), trung tâm của thiết kế là **Tài nguyên (Resources)**. Tài nguyên là các đối tượng hoặc thực thể thông tin trong hệ thống (như sách `books`, yêu cầu đặt mượn `reservations`, người dùng `users`), do đó URI đóng vai trò định vị danh từ để xác định tài nguyên đó nằm ở đâu trong không gian địa chỉ.
- **Phân định trách nhiệm:** Giao thức HTTP đã cung cấp sẵn một tập hợp các phương thức chuẩn (HTTP Verbs) mang đầy đủ ngữ nghĩa hành vi:
  - `GET`: Đọc tài nguyên (An toàn & Idempotent).
  - `POST`: Tạo mới tài nguyên hoặc xử lý dữ liệu phức tạp.
  - `PUT` / `PATCH`: Cập nhật tài nguyên.
  - `DELETE`: Xoá tài nguyên.
- **Liên hệ với mã nguồn:**
  - Thay vì thiết kế sai theo kiểu RPC như `/api/getAllBooks`, `/api/getBookById?id=1`, `/api/createReservation`, hệ thống sử dụng:
    - `GET /books`: Đọc danh sách sách.
    - `GET /books/1`: Đọc sách ID 1.
    - `POST /books/1/reservations`: Tạo mới yêu cầu mượn cho cuốn sách 1.
  - Việc này giúp URI luôn giữ được tính trong sáng, trực quan, tuân thủ đúng chuẩn RESTful quốc tế và dễ bảo trì.

### Câu hỏi 2: Stateless thể hiện như thế nào trong code?
- **Giải thích:** Tính chất **Stateless (Phi trạng thái)** trong REST quy định rằng Server không lưu giữ bất kỳ trạng thái phiên làm việc (Session State) nào của Client giữa các request liên tiếp. Mọi request gửi từ Client lên Server đều phải mang đầy đủ thông tin độc lập để Server có thể xử lý hoàn chỉnh mà không cần biết request trước đó là gì.
- **Thể hiện trong mã nguồn của dự án:**
  - Trong `Program.cs` của `Library.Api`, không hề sử dụng `HttpContext.Session`, cookie phiên, hoặc bộ nhớ cache để nhớ trạng thái Client:
    ```csharp
    app.MapPost("/books/{bookId:int}/reservations", (int bookId, CreateReservationRequest request) =>
    {
        // Request tự mang đầy đủ BookId (từ URI), UserId và UserName (từ Body JSON)
        var reservation = new ReservationDto(
            Id: Guid.NewGuid(),
            BookId: bookId,
            UserId: request.UserId.Trim(),
            UserName: request.UserName.Trim(),
            Status: ReservationStatus.Pending,
            RequestedAt: DateTimeOffset.UtcNow
        );
        ...
    });
    ```
  - Khi Client gọi `GET /users/{userId}/reservations`, Client phải tự cung cấp `userId` trên URI thay vì trông chờ Server "nhớ" tài khoản đã đăng nhập từ một session trước đó.
  - Nhờ tính chất Stateless, Server có thể dễ dàng mở rộng theo chiều ngang (Scale Out) thành nhiều node đằng sau một Load Balancer mà không gặp vấn đề dính phiên (Sticky Session).

### Câu hỏi 3: Vì sao Client và Server khai báo DTO riêng nhưng vẫn giao tiếp được với nhau?
- **Giải thích:** Client và Server trong hệ thống phân tán là hai tiến trình hoàn toàn độc lập (Decoupled), có thể chạy trên hai máy khác nhau, thậm chí viết bằng hai ngôn ngữ lập trình khác nhau. Chúng giao tiếp với nhau qua **Giao thức chuẩn (HTTP)** và **Định dạng dữ liệu trung gian chuẩn (JSON - JavaScript Object Notation)**.
- **Cơ chế hoạt động:**
  1. Khi Server gửi phản hồi, Server sử dụng bộ tuần tự hoá (JSON Serializer) để chuyển đối tượng DTO của Server thành chuỗi văn bản JSON (Text Representation).
  2. Dữ liệu JSON này được truyền qua môi trường mạng dưới dạng các byte HTTP Content (`Content-Type: application/json`).
  3. Khi Client nhận được gói tin HTTP, Client sử dụng bộ giải tuần tự hoá (JSON Deserializer) để đọc chuỗi JSON và khớp các trường dữ liệu tương ứng vào cấu trúc DTO nội bộ do Client tự định nghĩa.
- **Liên hệ với mã nguồn:**
  - Server trong `Library.Api/Program.cs` khai báo `public record BookDto(...)`.
  - Client trong `Library.Client/Program.cs` tự khai báo riêng `public record BookDto(...)`.
  - Cả hai không chia sẻ bất kỳ file binary dll hay class chung nào, nhưng do các tên trường (như `id`, `title`, `author`, `isbn`, `availableCopies`) tuân theo đúng quy ước đặt tên (CamelCase / PascalCase) trong JSON, `HttpClient.ReadFromJsonAsync<BookDto>()` dễ dàng khôi phục dữ liệu chính xác 100%. Đây chính là bản chất của tính tương thích liên thông (Interoperability) trong các hệ thống phân tán hiện đại.
