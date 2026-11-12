# Báo Cáo Thực Hành – EduSubmit (Bài 2: RESTful API)

## 1. Giới thiệu dự án
- **Học phần:** Phát triển ứng dụng phân tán (PTUDPT)
- **Tên dự án:** EduSubmit – Lát cắt 2: RESTful API cho hệ thống nộp bài tập phân tán.
- **Mục tiêu:**
  - Áp dụng phong cách kiến trúc REST để chuyển đổi bài toán nộp bài thành mô hình tài nguyên cha–con: Bài tập (`/api/assignments`) $\rightarrow$ Lượt nộp bài (`/api/assignments/{id}/submissions`).
  - Xây dựng **Submission.Api** (máy chủ ASP.NET Core Minimal API trên .NET 10) tại cổng `http://localhost:5000` với 4 endpoint nghiệp vụ đầy đủ: lấy danh sách, xem chi tiết, nộp bài, đọc lại lượt nộp.
  - Xây dựng **Submission.Client** (máy khách Console) sử dụng `HttpClient` để thực thi chuỗi kịch bản: Liệt kê bài tập $\rightarrow$ Nộp bài tập đang mở $\rightarrow$ Kiểm tra mã trạng thái $\rightarrow$ Đọc lại lượt nộp qua GUID $\rightarrow$ Bắt lỗi nghiệp vụ $\rightarrow$ Xử lý an toàn lỗi mạng.
  - Áp dụng nguyên tắc Stateless, hợp đồng dữ liệu DTO độc lập và cấu trúc lỗi chung `ApiError`.

---

## 2. Công nghệ sử dụng
- **Nền tảng & SDK:** .NET 10 SDK (phiên bản `10.0.403`).
- **Ngôn ngữ:** C# 14 / C# 13 (Top-level statements, positional record types, pattern matching).
- **Web Server:** Kestrel tích hợp sẵn trong ASP.NET Core 10.
- **Lưu trữ dữ liệu:** Danh sách bộ nhớ trong (`List<AssignmentDto>`, `List<SubmissionDto>`), đúng chuẩn lát cắt thực hành Bài 2, không phụ thuộc database ngoài.
- **Client & Serialization:** `HttpClient`, `System.Net.Http.Json` (`GetAsync`, `PostAsJsonAsync`, `ReadFromJsonAsync`).
- **Công cụ kiểm thử:** VS Code REST Client extension, `curl.exe` trên PowerShell.

---

## 3. Cấu trúc thư mục
Dự án `EduSubmit` nằm ngang hàng với `CatalogApi` bên trong `F:\Code_demo\Bai2`:

```text
Bai2/
├── CatalogApi/                     # Dự án mẫu Catalog API (giữ nguyên vẹn)
└── EduSubmit/                      # Dự án EduSubmit hoàn chỉnh
    ├── RestfulClientServer.slnx    # Solution quản lý 2 project (.NET 10)
    ├── .gitignore                  # Bỏ qua bin/, obj/
    ├── requests.http               # Bộ kịch bản kiểm thử 12 requests chuẩn REST Client
    ├── README.md                   # Báo cáo thực hành chi tiết EduSubmit
    └── src/
        ├── Submission.Api/         # Máy chủ Web API (Cổng 5000)
        │   ├── Submission.Api.csproj
        │   ├── Program.cs          # 4 endpoint RESTful, 3 cổng kiểm tra, DTOs & ApiError
        │   └── appsettings.json
        └── Submission.Client/      # Ứng dụng Console máy khách
            ├── Submission.Client.csproj
            └── Program.cs          # Luồng nộp bài 5 bước, bắt lỗi mạng & PrintApiErrorAsync
```

---

## 4. Hướng dẫn chạy dự án

### Bước 4.1: Mở terminal tại thư mục dự án
```powershell
cd F:\Code_demo\Bai2\EduSubmit
```

### Bước 4.2: Biên dịch toàn bộ Solution
```powershell
dotnet build
```
*Kết quả kỳ vọng:* `Build succeeded. 0 Warning(s) 0 Error(s)`.

### Bước 4.3: Chạy máy chủ Submission.Api (Terminal 1)
```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```
*Kết quả:* Terminal báo `Now listening on: http://localhost:5000`.

### Bước 4.4: Chạy máy khách Submission.Client (Terminal 2)
Mở một Terminal khác và chạy:
```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```
*Ghi chú:* Tham số `-- http://localhost:5000` được nạp vào `args[0]` để cấu hình BaseAddress động cho HttpClient.

---

## 5. Bảng mô tả 4 Endpoint API (Hợp đồng giao tiếp)

| Phương thức | URI Endpoint | Header / Payload | Chức năng nghiệp vụ & 3 cổng kiểm tra | Mã HTTP & Kết quả phản hồi |
|:---:|---|---|---|---|
| **GET** | `/` | — | Kiểm tra trạng thái máy chủ | `200 OK`<br>`{"service":"Submission.Api","status":"running"}` |
| **GET** | `/api/assignments` | — | Lấy danh sách bài tập | `200 OK` + Mảng JSON các bài tập (`AssignmentDto`) |
| **GET** | `/api/assignments/{assignmentId:int}` | — | Xem chi tiết bài tập theo ID | `200 OK` + JSON bài tập (nếu tồn tại)<br>`404 Not Found` + `ASSIGNMENT_NOT_FOUND` (nếu không có) |
| **POST** | `/api/assignments/{assignmentId:int}/submissions` | `Content-Type: application/json`<br>`CreateSubmissionRequest` | Nộp bài tập (Tài nguyên con).<br>1. Kiểm tra tồn tại?<br>2. Bài tập còn mở (`IsOpen`)?<br>3. Đủ mã SV & URL repo? | `201 Created` + header `Location: /api/submissions/{guid}` + `SubmissionDto`<br>`404 Not Found` + `ASSIGNMENT_NOT_FOUND` (nếu sai ID bài tập)<br>`400 Bad Request` + `ASSIGNMENT_CLOSED` (nếu bài đã đóng)<br>`400 Bad Request` + `INVALID_SUBMISSION` (nếu thiếu mã SV hoặc URL repo)<br>`415 Unsupported Media Type` (nếu thiếu header `Content-Type`) |
| **GET** | `/api/submissions/{submissionId:guid}` | — | Đọc lại lượt nộp theo GUID (theo đường dẫn trong header Location) | `200 OK` + JSON `SubmissionDto`<br>`404 Not Found` + `SUBMISSION_NOT_FOUND` (nếu không tìm thấy GUID) |

---

## 6. Các lệnh và kịch bản kiểm thử đã thực hiện

File `requests.http` chứa đầy đủ 12 kịch bản kiểm thử:
- Kiểm tra trang chủ `/` (200).
- Lấy danh sách bài tập `/api/assignments` (200).
- Xem chi tiết bài tập 1 (200).
- Xem chi tiết bài tập 999 (404 `ASSIGNMENT_NOT_FOUND`).
- Kiểm tra route constraint `/api/assignments/abc` (404 body rỗng).
- Nộp bài tập 1 hợp lệ với SV001 (201 Created kèm header Location).
- Response chaining đọc lại lượt nộp theo Guid (`GET /api/submissions/{id}`) (200 OK).
- Nộp bài tập 2 đã đóng (400 `ASSIGNMENT_CLOSED`).
- Nộp bài tập 999 không tồn tại (404 `ASSIGNMENT_NOT_FOUND`).
- Nộp bài dữ liệu rỗng (400 `INVALID_SUBMISSION`).
- Đọc lượt nộp với Guid giả (404 `SUBMISSION_NOT_FOUND`).
- POST thiếu header Content-Type (415 `Unsupported Media Type`).

---

## 7. Bảng kết quả kiểm thử thực tế

Toàn bộ các ca kiểm thử đã được chạy thực tế qua `curl.exe` và `dotnet run`:

| STT | Nội dung kiểm thử | Lệnh / Request thực tế | Kết quả mong đợi | Kết quả thực tế quan sát được | Trạng thái |
|:---:|---|---|---|---|:---:|
| **1** | Kiểm tra dịch vụ | `GET http://localhost:5000/` | 200 OK, JSON status running | `HTTP/1.1 200 OK`<br>`{"service":"Submission.Api","status":"running"}` | **PASS** |
| **2** | Lấy danh sách bài tập | `GET http://localhost:5000/api/assignments` | 200 OK, mảng 2 bài tập | `HTTP/1.1 200 OK`<br>Mảng JSON gồm bài tập 1 (IsOpen: true) và bài tập 2 (IsOpen: false) | **PASS** |
| **3** | Xem chi tiết bài tập 1 | `GET http://localhost:5000/api/assignments/1` | 200 OK, thông tin bài 1 | `HTTP/1.1 200 OK`<br>`{"id":1,"title":"Bai tap REST API",...}` | **PASS** |
| **4** | Xem bài tập không tồn tại | `GET http://localhost:5000/api/assignments/999` | 404 Not Found kèm ApiError | `HTTP/1.1 404 Not Found`<br>`{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | **PASS** |
| **5** | Ràng buộc kiểu route `:int` | `GET http://localhost:5000/api/assignments/abc` | 404 Not Found, body rỗng | `HTTP/1.1 404 Not Found`<br>`Content-Length: 0` | **PASS** |
| **6** | POST nộp bài hợp lệ bài 1 | `POST /api/assignments/1/submissions`<br>`{"studentCode":"SV001", "repositoryUrl":"..."}` | 201 Created kèm header Location | `HTTP/1.1 201 Created`<br>`Location: /api/submissions/01f1a47e-...`<br>`SubmissionDto` trạng thái "received" | **PASS** |
| **7** | POST bài tập đã đóng | `POST /api/assignments/2/submissions` | 400 Bad Request kèm ASSIGNMENT_CLOSED | `HTTP/1.1 400 Bad Request`<br>`{"code":"ASSIGNMENT_CLOSED","message":"Bai tap da dong, khong nhan bai nop."}` | **PASS** |
| **8** | POST bài tập không tồn tại | `POST /api/assignments/999/submissions` | 404 Not Found kèm ASSIGNMENT_NOT_FOUND | `HTTP/1.1 404 Not Found`<br>`{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | **PASS** |
| **9** | POST dữ liệu rỗng | `POST /api/assignments/1/submissions`<br>`{"studentCode":"", "repositoryUrl":""}` | 400 Bad Request kèm INVALID_SUBMISSION | `HTTP/1.1 400 Bad Request`<br>`{"code":"INVALID_SUBMISSION","message":"Thieu ma sinh vien hoac kho ma nguon."}` | **PASS** |
| **10** | Đọc lại lượt nộp theo GUID | `GET /api/submissions/{guid}` | 200 OK, đúng lượt nộp vừa tạo | `HTTP/1.1 200 OK`<br>Trả về đúng thông tin `SubmissionDto` đã lưu | **PASS** |
| **11** | Đọc lượt nộp GUID giả | `GET /api/submissions/00000000-0000-...` | 404 Not Found kèm SUBMISSION_NOT_FOUND | `HTTP/1.1 404 Not Found`<br>`{"code":"SUBMISSION_NOT_FOUND","message":"Khong tim thay luot nop."}` | **PASS** |
| **12** | POST thiếu Content-Type | `POST` không gửi Content-Type | 415 Unsupported Media Type | `HTTP/1.1 415 Unsupported Media Type`<br>`Content-Length: 0` | **PASS** |
| **13** | Chạy Submission.Client | `dotnet run --project src/Submission.Client` | Chạy tuần tự luồng: liệt kê $\rightarrow$ nộp $\rightarrow$ đọc lại | Client lấy danh sách bài $\rightarrow$ chọn bài đang mở $\rightarrow$ nộp thành công (201) $\rightarrow$ đọc lại qua GUID (200) $\rightarrow$ bắt lỗi 400 không crash | **PASS** |
| **14** | Tắt Server rồi chạy Client | Tắt API, chạy lại Submission.Client | Không crash, thông báo lỗi mạng | Client in: `Loi ket noi: Khong the ket noi den Server tai http://localhost:5000...` và kết thúc với Exit Code 0 | **PASS** |

---

## 8. Trả lời 3 câu hỏi lý thuyết trọng tâm

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Khái niệm định hướng tài nguyên (Resource-Oriented):** REST không xem hệ thống là tập hợp các hàm xử lý từ xa (RPC), mà xem hệ thống là tập hợp các **tài nguyên** có thể định danh.
- **Phân định vai trò:**
  - **URI là danh từ:** Xác định đối tượng/tài nguyên cần thao tác. Ví dụ `/api/assignments` là tập hợp bài tập, `/api/assignments/1/submissions` là các lượt nộp của bài tập 1. 
  - **HTTP Method là động từ:** Thể hiện ý định thao tác lên đối tượng đó (`GET` để truy vấn, `POST` để tạo mới, `DELETE` để xóa).
- **Lợi ích:** Tránh việc sinh ra các URI tùy tiện như `/doSubmit`, `/getAssignmentById`, `/createNewSubmission`, giúp giao diện API chuẩn hóa, dễ hiểu, tận dụng được tính năng routing, caching và bảo mật của hạ tầng mạng HTTP.

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Bản chất của Stateless:** Server không lưu giữ bất kỳ trạng thái hội thoại hay phiên làm việc nào của Client trong bộ nhớ. Mỗi request phải tự mang theo **đầy đủ thông tin** để Server xử lý độc lập.
- **Thể hiện trong code EduSubmit:**
  - Khi gửi `POST /api/assignments/1/submissions`, request mang đủ `studentCode` và `repositoryUrl`. Server không cần biết trước đó Client đã đăng nhập hay đã gọi trang nào, chỉ cần thẩm định payload hiện tại.
  - Khi Server trả về `201 Created` đi kèm header `Location: /api/submissions/{id}`, Server không giữ phiên chờ đọc. Muốn đọc lại, Client phải tự gửi request mới `GET /api/submissions/{id}` mang theo mã GUID độc lập.
  - Hai request nộp bài và đọc bài là hoàn toàn tách biệt. Nếu hệ thống nhân bản ra nhiều máy chủ API phía sau Load Balancer, request 1 rơi vào Server A và request 2 rơi vào Server B thì hệ thống vẫn hoạt động chính xác (miễn là dùng chung dữ liệu).

### Câu 3: Vì sao Client và Server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Tính độc lập giữa hai tiến trình:** `Submission.Api` và `Submission.Client` là hai ứng dụng chạy trên hai tiến trình riêng biệt, không dùng chung bộ nhớ và không tham chiếu project của nhau (`.csproj` của Client không reference sang Server).
- **Hợp đồng dữ liệu qua mạng (Wire Contract):**
  - Cầu nối duy nhất giữa Client và Server là **giao thức HTTP và chuỗi văn bản JSON**.
  - Server tuần tự hóa (serialize) đối tượng `SubmissionDto` thành chuỗi JSON:
    `{"id":"...","assignmentId":1,"studentCode":"SV001","repositoryUrl":"...","status":"received","receivedAt":"..."}`.
  - Client nhận dòng byte JSON này và giải tuần tự hóa (deserialize) vào record `SubmissionDto` do chính Client tự định nghĩa.
- **Lợi ích kiến trúc:** Đảm bảo nguyên tắc khớp nối lỏng (*Loose Coupling*). Server có thể thay đổi cấu trúc mã nguồn nội bộ mà không làm gãy Client, miễn là cấu trúc JSON truyền qua mạng vẫn đúng với hợp đồng đã thống nhất.

