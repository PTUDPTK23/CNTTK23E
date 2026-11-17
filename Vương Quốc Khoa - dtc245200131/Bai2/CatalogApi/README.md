# Báo Cáo Thực Hành Bài 2 – RESTful API cho tương tác Client–Server

## 1. Giới thiệu dự án
- **Học phần:** Phát triển ứng dụng phân tán (PTUDPT)
- **Tên bài thực hành:** Bài 2: RESTful API cho tương tác client–server (Catalog API)
- **Mục tiêu của dự án:**
  - Hiểu và áp dụng các nguyên tắc kiến trúc REST: tài nguyên, định danh URI, phương thức HTTP, kiến trúc Stateless và mã trạng thái HTTP chuẩn.
  - Xây dựng dịch vụ **Catalog.Api** (máy chủ) sử dụng ASP.NET Core Minimal API trên nền tảng .NET 10 lắng nghe tại cổng `http://localhost:5100`.
  - Xây dựng ứng dụng **Catalog.Client** (máy khách Console) sử dụng `HttpClient` để thực hiện giao tiếp đồng bộ qua mạng theo hợp đồng dữ liệu DTO và xử lý lỗi mạng an toàn.
  - Phân tích ranh giới phân tán: kiểm tra các mã phản hồi HTTP (`200 OK`, `201 Created`, `400 Bad Request`, `404 Not Found`, `415 Unsupported Media Type`) và phân biệt lỗi ứng dụng với lỗi mạng khi máy chủ vắng mặt.

---

## 2. Công nghệ sử dụng
- **Ngôn ngữ lập trình:** C# 14 / C# 13 (hỗ trợ Top-level statements, C# Record types, Pattern matching).
- **Nền tảng / SDK:** .NET 10 SDK (phiên bản `10.0.403`).
- **Framework Web:** ASP.NET Core 10.0 Minimal APIs (`Microsoft.NET.Sdk.Web`).
- **Web Server:** Kestrel HTTP Server tích hợp sẵn trong ASP.NET Core.
- **Thư viện HTTP Client:** `System.Net.Http.Json` (`HttpClient`, `GetFromJsonAsync`, `PostAsJsonAsync`).
- **Công cụ kiểm thử & phát triển:** Visual Studio Code, extension REST Client (Huachao Mao), `curl.exe` trên PowerShell.

---

## 3. Cấu trúc thư mục
Cấu trúc tổ chức source code trong `F:\Code_demo\Bai2\CatalogApi`:

```text
CatalogApi/
├── CatalogApi.slnx             # Solution quản lý danh bạ 2 project (.NET 10)
├── .gitignore                  # Bỏ qua thư mục bin/, obj/ khi đưa lên Git
├── requests.http               # Bộ kịch bản kiểm thử API bằng REST Client (8 requests)
├── README.md                   # Tài liệu hướng dẫn & báo cáo thực hành Bài 2
└── src/
    ├── Catalog.Api/            # Project Web API (Máy chủ - Cổng 5100)
    │   ├── Catalog.Api.csproj  # File cấu hình project Web
    │   ├── Program.cs          # Khởi tạo Kestrel, dữ liệu mẫu, các endpoint Minimal API & DTO
    │   ├── appsettings.json    # Cấu hình môi trường Kestrel
    │   ├── sp-hop-le.json      # File mẫu dữ liệu JSON hợp lệ để test với curl
    │   ├── sp-sai.json         # File mẫu dữ liệu JSON lỗi để test với curl
    │   └── requests.http       # Bản sao requests.http trong thư mục API
    └── Catalog.Client/         # Project Console (Máy khách)
        ├── Catalog.Client.csproj # File cấu hình project Console
        └── Program.cs          # HttpClient gọi tuần tự 4 nấc & bọc xử lý HttpRequestException
```

---

## 4. Hướng dẫn chạy dự án

### Bước 4.1: Mở terminal tại thư mục gốc của dự án
Mở PowerShell trong VS Code tại đường dẫn:
```powershell
cd F:\Code_demo\Bai2\CatalogApi
```

### Bước 4.2: Biên dịch toàn bộ Solution
```powershell
dotnet build
```
*Kết quả kỳ vọng:* `Build succeeded. 0 Warning(s) 0 Error(s)`.

### Bước 4.3: Chạy máy chủ Catalog.Api (Terminal 1)
Chạy Web API lắng nghe cố định tại cổng 5100:
```powershell
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```
*Kết quả:* Terminal hiển thị:
```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5100
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Bước 4.4: Chạy máy khách Catalog.Client (Terminal 2)
Mở một cửa sổ Terminal thứ hai và chạy:
```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```
*Ghi chú:* Tham số `-- http://localhost:5100` được chuyển trực tiếp vào `args[0]` của Client, giúp thay đổi URL API linh hoạt mà không cần sửa code.

### Bước 4.5: Kiểm thử bằng file `requests.http`
1. Mở file `requests.http` trong VS Code.
2. Phía trên mỗi khối yêu cầu xuất hiện nút **Send Request**.
3. Bấm lần lượt từng request từ 1 đến 8 để quan sát kết quả phản hồi ở khung bên phải.

---

## 5. Danh sách API (Hợp đồng giao tiếp)

| Phương thức | URI Endpoint | Header yêu cầu | Chức năng | Mã trạng thái & Kết quả phản hồi |
|:---:|---|---|---|---|
| **GET** | `/` | — | Kiểm tra trạng thái máy chủ | `200 OK` + `{"service":"Catalog.Api","status":"Running"}` |
| **GET** | `/api/products` | — | Lấy toàn bộ danh sách sản phẩm | `200 OK` + Mảng JSON các sản phẩm |
| **GET** | `/api/products/{id:int}` | — | Lấy chi tiết một sản phẩm theo ID | `200 OK` + JSON sản phẩm (nếu tìm thấy)<br>`404 Not Found` + `ApiError` code `PRODUCT_NOT_FOUND` (nếu không có) |
| **GET** | `/api/products/{id:int}` | — | Ràng buộc kiểu dữ liệu số nguyên | `404 Not Found` body rỗng khi truyền tham số chữ (vd: `/api/products/abc`) |
| **POST** | `/api/products` | `Content-Type: application/json` | Thêm mới sản phẩm vào danh mục | `201 Created` + header `Location: /api/products/{id}` + JSON sản phẩm vừa tạo<br>`400 Bad Request` + `ApiError` code `INVALID_PRODUCT` (khi tên rỗng hoặc giá $\le$ 0)<br>`415 Unsupported Media Type` (nếu thiếu header `Content-Type`) |

---

## 6. Bảng kết quả kiểm thử thực tế

Bảng tổng hợp kết quả chạy kiểm thử thực tế 10 ca kiểm thử theo yêu cầu:

| STT | Nội dung kiểm thử | Lệnh / Request thực hiện | Kết quả mong đợi | Kết quả thực tế quan sát được | Trạng thái |
|:---:|---|---|---|---|:---:|
| **1** | Kiểm tra trang chủ | `GET http://localhost:5100/` | 200 OK, JSON service running | `HTTP/1.1 200 OK`<br>`{"service":"Catalog.Api","status":"Running"}` | **PASS** |
| **2** | Lấy danh sách sản phẩm | `GET http://localhost:5100/api/products` | 200 OK, mảng 3 sản phẩm ban đầu | `HTTP/1.1 200 OK`<br>Mảng JSON gồm 3 sản phẩm mẫu ban đầu | **PASS** |
| **3** | Lấy chi tiết sản phẩm 1 | `GET http://localhost:5100/api/products/1` | 200 OK, đúng thông tin sản phẩm 1 | `HTTP/1.1 200 OK`<br>`{"id":1,"name":"Giao dich lap trinh phan tan",...}` | **PASS** |
| **4** | Lấy sản phẩm không tồn tại | `GET http://localhost:5100/api/products/999` | 404 Not Found kèm ApiError | `HTTP/1.1 404 Not Found`<br>`{"code":"PRODUCT_NOT_FOUND","message":"Khong tim thay san pham."}` | **PASS** |
| **5** | Ràng buộc kiểu route constraint | `GET http://localhost:5100/api/products/abc` | 404 Not Found, body rỗng | `HTTP/1.1 404 Not Found`<br>`Content-Length: 0` (Framework từ chối tuyến) | **PASS** |
| **6** | POST sản phẩm hợp lệ | `POST /api/products`<br>Body: `{"name":"Lap trinh gRPC", "price":80000}` | 201 Created kèm header Location | `HTTP/1.1 201 Created`<br>`Location: /api/products/4`<br>`{"id":4,"name":"Lap trinh gRPC",...}` | **PASS** |
| **7** | POST sản phẩm giá âm | `POST /api/products`<br>Body: `{"name":"San pham loi", "price":-1}` | 400 Bad Request kèm ApiError | `HTTP/1.1 400 Bad Request`<br>`{"code":"INVALID_PRODUCT","message":"Ten san pham khong duoc de trong va gia phai lon hon 0."}` | **PASS** |
| **8** | POST thiếu Content-Type | `POST /api/products`<br>Không gửi header Content-Type | 415 Unsupported Media Type | `HTTP/1.1 415 Unsupported Media Type`<br>`Content-Length: 0` | **PASS** |
| **9** | Chạy Catalog.Client | `dotnet run --project src/Catalog.Client` | Chạy tuần tự 4 nấc không crash | Client hoàn thành 4 nấc: lấy danh sách $\rightarrow$ tạo mới (201) $\rightarrow$ đọc lại chi tiết $\rightarrow$ bắt lỗi 400 và in ra thông báo lỗi nghiệp vụ. | **PASS** |
| **10** | Tắt Server rồi chạy Client | Tắt API, chạy lại Catalog.Client | Bắt ngoại lệ mạng, thông báo thân thiện | Client in thông báo `Loi ket noi: Khong the ket noi den Server tai http://localhost:5100...` và kết thúc bình thường, không crash. | **PASS** |

---

## 7. Ảnh minh chứng cần chụp

Khi hoàn tất bài và nộp bài, hãy chụp các ảnh màn hình thực tế sau đây và chèn vào tài liệu:

1. **Ảnh 1 – API khởi động thành công:** Màn hình Terminal chạy lệnh `dotnet run --project src/Catalog.Api` hiển thị `Now listening on: http://localhost:5100`.
   *(Vị trí lưu đề xuất: `docs/anh1-api-started.png`)*
2. **Ảnh 2 – Lấy danh sách sản phẩm (200 OK):** Màn hình REST Client hoặc trình duyệt gọi `GET /api/products` nhận mảng 3 sản phẩm.
   *(Vị trí lưu đề xuất: `docs/anh2-get-products-200.png`)*
3. **Ảnh 3 – Kiểm tra sản phẩm không tồn tại (404 Not Found):** Màn hình REST Client gọi `GET /api/products/999` nhận mã 404 và body `PRODUCT_NOT_FOUND`.
   *(Vị trí lưu đề xuất: `docs/anh3-get-404-notfound.png`)*
4. **Ảnh 4 – Tạo sản phẩm thành công (201 Created):** Màn hình REST Client gọi `POST /api/products` thành công, có header `Location: /api/products/4`.
   *(Vị trí lưu đề xuất: `docs/anh4-post-201-created.png`)*
5. **Ảnh 5 – Bắt lỗi dữ liệu không hợp lệ (400 Bad Request):** Màn hình REST Client gọi `POST /api/products` với giá âm nhận 400 `INVALID_PRODUCT`.
   *(Vị trí lưu đề xuất: `docs/anh5-post-400-badrequest.png`)*
6. **Ảnh 6 – Catalog.Client chạy đủ 4 bước:** Màn hình Terminal 2 chạy máy khách Console in đầy đủ 4 nấc khi Server đang bật.
   *(Vị trí lưu đề xuất: `docs/anh6-client-4-steps.png`)*
7. **Ảnh 7 – Xử lý lỗi khi Server tắt:** Màn hình Terminal chạy Client khi API chưa bật, hiển thị câu thông báo lỗi kết nối tiếng Việt thân thiện thay vì văng crash.
   *(Vị trí lưu đề xuất: `docs/anh7-client-server-down.png`)*

---

## 8. Trả lời 3 câu hỏi lý thuyết trọng tâm

### Câu 1: Tại sao URI nên sử dụng danh từ (nouns), còn HTTP Methods thể hiện hành động?
- **Bản chất kiến trúc REST:** REST (*Representational State Transfer*) tổ chức hệ thống xoay quanh **tài nguyên (Resources)** chứ không xoay quanh lời gọi hàm RPC (*Remote Procedure Call*).
- **Phân tách trách nhiệm rõ ràng:**
  - **URI đóng vai trò danh từ:** Dùng để định danh duy nhất vị trí của tài nguyên trên mạng (Ví dụ: `/api/products` đại diện cho tập hợp sản phẩm; `/api/products/1` đại diện cho một sản phẩm cụ thể).
  - **HTTP Method đóng vai trò động từ:** Đại diện cho hành động chuẩn hóa muốn tác động lên tài nguyên đó (`GET` để lấy, `POST` để tạo mới, `PUT/PATCH` để sửa đổi, `DELETE` để xóa).
- **Tránh trùng lặp và phân mảnh URL:** Nếu đưa động từ vào URI (như `/api/getProducts`, `/api/createProduct`, `/api/deleteProductById`), hệ thống sẽ sinh ra vô số endpoint rời rạc, làm mất tính trực quan và gây khó khăn cho việc caching, phân quyền cũng như kiểm thử. Trong code Catalog.Api, việc dùng chung một URI `/api/products` nhưng phân biệt `app.MapGet("/api/products", ...)` và `app.MapPost("/api/products", ...)` giúp thiết kế trong sáng, dễ mở rộng và tuân thủ đúng chuẩn REST.

### Câu 2: Tính "Stateless" (Phi trạng thái) được thể hiện ở đâu trong code đã viết?
- **Bản chất của Stateless trong REST:** Máy chủ không duy trì bất kỳ trạng thái phiên làm việc (Session/Conversation) nào của máy khách giữa các lần yêu cầu liên tiếp. Mỗi HTTP Request gửi từ Client lên Server phải tự chứa **đầy đủ thông tin cần thiết** để Server có thể hiểu và xử lý độc lập.
- **Biểu hiện trong code dự án:**
  - Khi Client gọi `POST /api/products`, body gửi lên chứa đầy đủ dữ liệu của sản phẩm (`Name`, `Description`, `Price`). Server không cần nhớ Client này là ai, trước đó đã gọi những API nào, mà chỉ cần căn cứ vào payload của request hiện tại để kiểm tra tính hợp lệ và ghi vào danh sách.
  - Sau khi tạo xong, Server trả về mã `201 Created` kèm header `Location: /api/products/{id}`. Khi Client muốn đọc lại sản phẩm ở Bước 3, Client phải tự gửi một request độc lập `GET /api/products/1`. Server xử lý việc đọc dựa trên ID trên URL mà không cần ghi nhớ phiên nộp trước đó.
  - Nhờ tính chất Stateless này, nếu hệ thống triển khai đằng sau một bộ cân bằng tải (Load Balancer) với nhiều máy chủ API nhân bản, bất kỳ máy chủ nào nhận request cũng đều xử lý ra cùng một kết quả mà không cần đồng bộ bộ nhớ phiên (Session State).

### Câu 3: Tại sao Client và Server mỗi bên khai báo DTO riêng mà vẫn tương thích với nhau?
- **Tính độc lập giữa hai tiến trình phân tán:** Trong hệ thống phân tán, Client và Server là hai tiến trình hoàn toàn độc lập, có thể chạy ở hai máy tính khác nhau và không dùng chung vùng nhớ hay tham chiếu mã nguồn (`.csproj` của Client không hề reference sang Server).
- **Hợp đồng giao tiếp trung gian (Wire Contract):**
  - Thứ duy nhất đi qua đường truyền mạng là dòng byte dữ liệu định dạng chuẩn: **HTTP + JSON**.
  - Khi Server gửi dữ liệu, nó tuần tự hóa đối tượng C# `ProductDto` thành chuỗi JSON:
    `{"id":1,"name":"...","description":"...","price":150000}`.
  - Phía Client nhận chuỗi JSON này và giải tuần tự hóa vào kiểu `ProductDto` mà Client tự khai báo.
- **Khớp nối lỏng (Loose Coupling):** Miễn là tên thuộc tính và kiểu dữ liệu JSON khớp nhau (hoặc được mapping qua thuộc tính JSON), thì Client và Server có thể tự do phát triển, tái cấu trúc hoặc thậm chí viết bằng hai ngôn ngữ lập trình khác nhau (ví dụ Server C#, Client React/Node.js) mà hệ thống vẫn tương thích và hoạt động ổn định.

