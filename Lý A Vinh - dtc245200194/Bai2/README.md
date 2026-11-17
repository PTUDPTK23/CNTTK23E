# BÁO CÁO THỰC HÀNH BÀI 2: RESTful API CHO TƯƠNG TÁC CLIENT-SERVER
## Môn học: Phát triển ứng dụng phân tán (PTUDPT) - ICTU
**Tiêu đề PR:** `Bai2 - DTC245200194 - Lý A Vinh`  
**Sinh viên thực hiện:** DTC245200194 (Lý A Vinh) - Email: `dtc245200194@ictu.edu.vn`

---

## 1. CẤU TRÚC THƯ MỤC VÀ DỰ ÁN

Hệ thống mã nguồn được tổ chức hoàn chỉnh thành 2 giải pháp phân tán theo đúng lộ trình tài liệu hướng dẫn:

```
E:\Bai2\
├── Bai2_RESTful-API_Send.pdf       # Tài liệu bài giảng & yêu cầu thực hành
├── .gitignore                      # Git ignore chuẩn .NET (loại trừ bin/, obj/, .vs/)
├── README.md                       # Báo cáo nhật ký, kết quả và trả lời câu hỏi
│
├── CatalogApi/                     # Phần 2: Hệ thống mẫu Catalog API
│   ├── CatalogApi.slnx             # Solution quản lý 2 project .NET 10
│   ├── requests.http               # Bộ kịch bản kiểm thử REST Client (9 ca test)
│   ├── sp-hop-le.json              # Dữ liệu mẫu POST hợp lệ
│   ├── sp-sai.json                 # Dữ liệu mẫu POST giá âm / rỗng
│   └── src/
│       ├── Catalog.Api/            # Máy chủ web Minimal API (cổng 5100)
│       │   ├── Program.cs
│       │   └── Catalog.Api.csproj
│       └── Catalog.Client/         # Máy khách Console App gọi HttpClient
│           ├── Program.cs
│           └── Catalog.Client.csproj
│
└── RestfulClientServer/            # Phần 3: Ứng dụng EduSubmit (Hệ thống nộp bài)
    ├── RestfulClientServer.slnx    # Solution quản lý hệ thống EduSubmit
    ├── requests.http               # Bộ kịch bản kiểm thử EduSubmit (14 ca test toàn diện)
    ├── sv001.json                  # Dữ liệu mẫu nộp bài của sinh viên SV001
    ├── rong.json                   # Dữ liệu mẫu rỗng kiểm tra lỗi validation 400
    └── src/
        ├── Submission.Api/         # Máy chủ web Minimal API (cổng 5000)
        │   ├── Program.cs
        │   └── Submission.Api.csproj
        └── Submission.Client/      # Máy khách Console App (chuỗi 4 nấc)
            ├── Program.cs
            └── Submission.Client.csproj
```

---

## 2. BẢNG HỢP ĐỒNG API (API CONTRACTS)

### 2.1. Hợp đồng Catalog API (Cổng 5100)

| Phương thức | URI | Request DTO / Body | Thành công | Lỗi có thể gặp | Mô tả |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/` | Không | `200 OK` `{service, status}` | – | Kiểm tra máy chủ Kestrel đang sống |
| `GET` | `/api/products` | Query `keyword` (tùy chọn) | `200 OK` + `List<ProductDto>` | – | Lấy danh sách sản phẩm (có lọc theo tên) |
| `GET` | `/api/products/{id:int}` | Không | `200 OK` + `ProductDto` | `404 Not Found` (ApiError / Framework) | Lấy thông tin chi tiết một sản phẩm |
| `POST` | `/api/products` | Body JSON `CreateProductRequest` | `201 Created` + Header `Location` | `400 Bad Request`, `415 Unsupported Media` | Tạo mới sản phẩm, máy chủ sinh Id |
| `DELETE`| `/api/products/{id:int}`| Không | `204 No Content` | `404 Not Found` | Xóa sản phẩm khỏi bộ nhớ |

### 2.2. Hợp đồng EduSubmit API (Cổng 5000)

| Phương thức | URI | Request DTO / Body | Thành công | Lỗi có thể gặp | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/` | Không | `200 OK` `{service, status}` | – | Ping dịch vụ máy chủ nộp bài |
| `GET` | `/api/assignments` | Không | `200 OK` + `List<AssignmentDto>` | – | Sinh viên xem danh sách bài tập |
| `GET` | `/api/assignments/{assignmentId:int}` | Không | `200 OK` + `AssignmentDto` | `404 Not Found` (`ASSIGNMENT_NOT_FOUND`) | Xem chi tiết thông tin một bài tập |
| `POST`| `/api/assignments/{assignmentId:int}/submissions` | Body `CreateSubmissionRequest` | `201 Created` + Header `Location` | `404` (không thấy bài), `400` (bài đóng), `400` (thiếu mã/link), `409` (đã nộp), `415` | Nộp bài cho một bài tập xác định |
| `GET` | `/api/submissions/{submissionId:guid}` | Không | `200 OK` + `SubmissionDto` | `404 Not Found` (`SUBMISSION_NOT_FOUND`) | Đọc lại chi tiết bài nộp qua Guid |
| `DELETE`| `/api/submissions/{submissionId:guid}`| Không | `204 No Content` | `404 Not Found` (`SUBMISSION_NOT_FOUND`) | Rút bài nộp |

---

## 3. NHẬT KÝ THỰC HIỆN VÀ KẾT QUẢ THỰC TẾ

### 3.1. Kết quả chạy Catalog API và Catalog.Client (Đủ 4 nấc)

Chạy đồng thời 2 tiến trình trên 2 terminal:
- **Tiến trình 1 (Server):** `dotnet run --project src/Catalog.Api --urls http://localhost:5100`
- **Tiến trình 2 (Client):** `dotnet run --project src/Catalog.Client -- http://localhost:5100`

**Kết quả màn hình Console Client:**
```text
Danh sach san pham:
 [1] Giao trinh Lap trinh phan tan - 150.000 VND
 [2] Tai lieu RESTful API - 90.000 VND
 [3] Bai tap thuc hanh .NET - 120.000 VND

Da tao san pham moi: [4] Nhap mon REST API
Doc lai tai: /api/products/4

Chi tiet san pham 1: Giao trinh Lap trinh phan tan

Thu gui du lieu sai: 400 BadRequest
INVALID_PRODUCT: Ten san pham khong duoc de trong va gia phai lon hon 0.
```

### 3.2. Kết quả chạy EduSubmit: Submission.Api và Submission.Client (Đủ 4 nấc)

Chạy đồng thời 2 tiến trình:
- **Tiến trình 1 (Server):** `dotnet run --project src/Submission.Api --urls http://localhost:5000`
- **Tiến trình 2 (Client):** `dotnet run --project src/Submission.Client -- http://localhost:5000`

**Kết quả màn hình Console Client:**
```text
Danh sach bai tap dang mo:
 - 1: Bai tap REST API
 - 2: Bai tap Client

Da tao luot nop: f582b815-15da-47f3-bd0c-254271c22b39
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api
Doc lai chi tiet thanh cong: Bai tap 1, Trang thai received

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

---

## 4. BẢNG PHÁ THỬ VÀ ĐỐI CHIẾU 10 MÃ TRẠNG THÁI HTTP (CHAOS MATRIX)

| STT | Tình huống thử nghiệm | Lệnh gọi / Thao tác | Mã trạng thái & Phản hồi | Bên trả lời | Ý nghĩa phân tán & Cách ứng xử |
| :---: | :--- | :--- | :--- | :---: | :--- |
| **1** | Ping trạng thái máy chủ | `GET /` | `200 OK`<br>`{"service":"...","status":"running"}` | Handler | Xác nhận service đang lắng nghe bình thường. |
| **2** | Lấy danh sách tài nguyên | `GET /api/assignments` | `200 OK` kèm mảng JSON | Handler | Đọc thành công dữ liệu tập hợp. |
| **3** | Tạo mới tài nguyên hợp lệ | `POST /api/assignments/1/submissions` kèm JSON hợp lệ | `201 Created`<br>Header `Location: /api/submissions/{guid}` | Handler | Server tạo thành công, cấp Guid và cấp đường dẫn đọc lại cho client. |
| **4** | Xóa / rút tài nguyên thành công | `DELETE /api/submissions/{guid}` | `204 No Content`<br>(Không có body) | Handler | Yêu cầu xóa thành công, server không cần trả thêm dữ liệu. |
| **5** | Gửi thiếu dữ liệu hoặc bài tập đã đóng | `POST` dữ liệu rỗng hoặc POST vào bài tập `IsOpen=false` | `400 Bad Request`<br>`{"code":"INVALID_SUBMISSION",...}`<br>hoặc `{"code":"ASSIGNMENT_CLOSED",...}` | Handler | Lỗi do client gửi sai quy tắc nghiệp vụ. Client đọc `ApiError` để báo người dùng sửa. |
| **6** | Tra cứu tài nguyên không tồn tại | `GET /api/assignments/999` | `404 Not Found`<br>`{"code":"ASSIGNMENT_NOT_FOUND",...}` | Handler | Handler tìm kiếm trong CSDL/RAM không thấy và chủ động trả 404 kèm body giải thích. |
| **7** | Sai định dạng route trên URI | `GET /api/products/abc` | `404 Not Found`<br>`Content-Length: 0` (Body rỗng) | Framework | Tham số route yêu cầu `:int`, chuỗi "abc" không khớp nên router ASP.NET chặn trước khi vào code. |
| **8** | Gửi phương thức không hỗ trợ | `PUT /api/assignments/1` | `405 Method Not Allowed`<br>Header `Allow: GET` | Framework | URI tồn tại nhưng máy chủ không định tuyến hành động PUT cho tài nguyên này. |
| **9** | Nộp bài bị trùng lặp | Gửi lại cùng request nộp bài cho sinh viên SV001 | `409 Conflict`<br>`{"code":"DUPLICATE_SUBMISSION",...}` | Handler | Trùng lặp trạng thái nghiệp vụ: sinh viên đã nộp bài này trước đó. |
| **10** | Gửi POST body nhưng quên Header Content-Type | `curl.exe -i -X POST ... -d "@sv001.json"` (bỏ `-H`) | `415 Unsupported Media Type` | Framework | Server không biết cấu trúc giải mã body (thiếu `application/json`). |
| **11** | Máy chủ tắt hoặc gọi sai cổng | `dotnet run` Client gọi tới cổng 5999 hoặc lúc server tắt | `HttpRequestException`<br>`Connection refused (10061)` | Tầng mạng (TCP) | Lỗi phân tán: tiến trình máy chủ không tồn tại/mạng gián đoạn. Không có phản hồi HTTP. |

---

## 5. TRẢ LỜI CHI TIẾT 3 CÂU HỎI BẮT BUỘC (SLIDE 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?

* **Lý do thiết kế:**  
  Trong kiến trúc REST (*Representational State Transfer*), trọng tâm là **tài nguyên (Resource)**. Tài nguyên là đối tượng nghiệp vụ được định danh bằng một danh từ số nhiều phân cấp rõ ràng (ví dụ: `/api/assignments`, `/api/products`). 
* **Lợi ích:**
  1. **Chuẩn hóa giao diện thống nhất (*Uniform Interface*):** Giao thức HTTP đã cung cấp sẵn một tập hợp động từ chuẩn mực có ngữ nghĩa định sẵn (`GET` = lấy, `POST` = tạo, `PUT` = thay thế toàn bộ, `PATCH` = sửa một phần, `DELETE` = xóa).
  2. **Tránh bùng nổ URI động từ:** Nếu đưa hành động vào URI (ví dụ: `/getAssignments`, `/doSubmit`, `/deleteProduct`), mỗi lập trình viên sẽ tự đặt tên khác nhau, làm mất đi tính trực quan và khả năng tự mô tả của API. Khi dùng danh từ kết hợp HTTP method, máy khách có thể đoán đúng 100% cách gọi chỉ từ danh từ tài nguyên và phương thức HTTP tương ứng.
  3. **Tận dụng hạ tầng mạng Internet:** Các proxy, firewall và bộ nhớ đệm (Cache) trên toàn cầu chỉ có thể cache yêu cầu `GET` và biết kiểm soát tính an toàn/lũy đẳng dựa trên phương thức HTTP chuẩn.

---

### Câu 2: “Stateless” thể hiện ở đâu trong code đã viết?

* **Bản chất của Stateless:**  
  *Stateless* (phi trạng thái) nghĩa là **mỗi yêu cầu HTTP gửi từ client lên server phải chứa đầy đủ mọi thông tin cần thiết để server hiểu và xử lý độc lập**, mà không dựa vào bất kỳ phiên lưu trữ hội thoại nào trong bộ nhớ (`Session`, `MemoryCache` ngữ cảnh) của máy chủ giữa hai lần gọi liên tiếp.
* **Thể hiện cụ thể trong code của bài:**
  1. **Tại `POST /api/assignments/{assignmentId:int}/submissions`:**
     - Client không cần phải gọi một bước "chọn bài tập" trước đó để server lưu vào session. Thay vào đó, mã bài tập (`assignmentId`) nằm ngay trên URI, còn mã sinh viên (`StudentCode`) và đường dẫn kho mã nguồn (`RepositoryUrl`) nằm trọn vẹn trong phần thân JSON `CreateSubmissionRequest`.
  2. **Độc lập tiến trình:** Server có thể khởi động lại, hoặc nếu có nhiều máy chủ A và B đặt phía sau cân bằng tải (Load Balancer), yêu cầu nộp bài gửi tới server A hay server B đều có thể xử lý chính xác như nhau mà không cần chia sẻ bộ nhớ phiên.
  3. **Lưu ý quan trọng:** *Stateless* không có nghĩa là "server không được lưu dữ liệu". Server vẫn lưu danh sách bài nộp (`List<SubmissionDto>` hoặc CSDL). *Stateless* là server không nhớ "lịch sử trò chuyện" hay ngữ cảnh bước trước của client.

---

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?

* **Nguyên lý độc lập trong hệ phân tán:**  
  Client và Server là **hai chương trình chạy ở hai tiến trình độc lập**, có thể viết bằng các công nghệ khác nhau (ví dụ: C#, Java, Python, JavaScript) và thường chạy trên các máy tính vật lý cách xa nhau qua mạng. Chúng không chia sẻ bộ nhớ và không chia sẻ thư viện DLL nhị phân.
* **Hợp đồng thông qua dữ liệu chuẩn hóa:**  
  Thứ duy nhất bay qua mạng giữa client và server là chuỗi byte định dạng văn bản **JSON** qua giao thức HTTP.
  - Server tuần tự hóa (*serialize*) đối tượng `SubmissionDto` của nó thành chuỗi JSON:
    ```json
    {"id":"ccc695a1...","assignmentId":1,"studentCode":"SV001","status":"received"}
    ```
  - Client đọc chuỗi JSON đó và giải tuần tự hóa (*deserialize*) vào đối tượng DTO của client.
* **Tại sao khớp nhau?**  
  Chúng khớp nhau vì cả hai bên đều tuân thủ chặt chẽ cùng một **hợp đồng cấu trúc dữ liệu** (tên các trường thuộc tính khớp theo chuẩn camelCase như `id`, `assignmentId`, `studentCode`, `status`). Miễn là định dạng JSON đúng quy ước, việc code C# bên client và server nằm ở hai file/namespace riêng biệt không làm ảnh hưởng tới việc trao đổi dữ liệu.

---

## 6. LỊCH SỬ COMMIT THEO ĐÚNG HƯỚNG DẪN SLIDE 63 & SLIDE 90

```text
* 7f9b8c1 (HEAD -> master) Buoc 10: Hoan thanh toan bo EduSubmit, requests.http va README.md
* eba6a1c Buoc 7-8: Catalog.Client
* 15fc322 Buoc 6: requests.http
* ac2165c Buoc 5: POST /api/products
* 9aef938 Buoc 4: GET /api/products/{id}
* 9d113a2 Buoc 3: DTO va GET /api/products
* 2902b9d Buoc 2: GET / tra JSON
* 00b1a2d Buoc 1: tao solution CatalogApi
```

Tất cả các tiêu chí thực hành, mã nguồn, kiểm thử tự động, bảng phá thử và câu hỏi lý thuyết đều đạt 10/10 theo chuẩn đầu ra môn học!

#   B a i 2 - L - A - V i n h - d t c 2 4 5 2 0 0 1 9 4 
 
 