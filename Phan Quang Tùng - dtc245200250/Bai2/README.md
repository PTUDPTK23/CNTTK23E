# Báo cáo Thực hành Bài 2: RESTful API cho tương tác client–server

**Sinh viên thực hiện:** Phan Quang Tùng  
**Mã sinh viên:** DTC245200250  
**Lớp:** K23.K1.D2.2627.CNTT K23E  
**Học phần:** Phát triển ứng dụng phân tán  

---

## 1. Giới thiệu tổng quan

Bài thực hành số 2 tập trung vào cơ chế truyền thông đồng bộ thông qua **RESTful API** giữa hai chương trình độc lập (máy chủ và máy khách) chạy ở hai tiến trình riêng biệt.

Bài thực hành gồm hai hệ thống chính:
1. **CatalogApi**: Dịch vụ quản lý danh mục sản phẩm (cổng 5100).
2. **EduSubmit**: Dịch vụ nộp bài tập phân tán (cổng 5000) với tài nguyên phân cấp cha - con và mã định danh duy nhất GUID.

---

## 2. Phần 1: Catalog API

### 2.1 Cấu trúc thư mục & thiết kế hợp đồng
- Máy chủ `Catalog.Api` lắng nghe tại cổng `5100` (`http://localhost:5100`).
- Máy khách `Catalog.Client` chạy độc lập, nhận URL máy chủ qua tham số dòng lệnh `args[0]`.

| Phương thức | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| `GET` | `/` | `200 OK` + `{"service":"Catalog.Api","status":"running"}` | – |
| `GET` | `/api/products` | `200 OK` + danh sách sản phẩm | – |
| `GET` | `/api/products/{id:int}` | `200 OK` + thông tin sản phẩm | `404 Not Found` (PRODUCT_NOT_FOUND hoặc rỗng nếu id không phải số) |
| `POST` | `/api/products` | `201 Created` + Location + sản phẩm mới | `400 Bad Request` (INVALID_PRODUCT), `415 Unsupported Media Type` |

### 2.2 Bảng 4 thí nghiệm phân tán (Bước 9)

| STT | Thí nghiệm | Dự đoán | Kết quả thực tế | Giải thích / Bài học |
|:---:|---|---|---|---|
| 1 | Chạy máy khách lần 2 (máy chủ vẫn chạy) | Danh sách sản phẩm có 4 mục, tạo mới ra mục thứ 5 | Danh sách có 4 sản phẩm, tạo mới ra [5], Doc lai tai: `/api/products/5` | Trạng thái (state) nằm ở máy chủ, không ở máy khách. Cùng một client chạy nhiều lần hoặc nhiều client khác nhau đều thấy dữ liệu chung cập nhật trên máy chủ. |
| 2 | Tắt máy chủ (Ctrl+C), chạy máy khách | Lỗi kết nối vì máy chủ không còn chạy | `Unhandled exception: HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5100)` | Lỗi đặc thù chỉ có ở hệ phân tán: bên kia (máy chủ) có thể vắng mặt do sập, tắt, mất mạng hoặc sai địa chỉ. Hoàn toàn khác với gọi hàm nội bộ (in-process). |
| 3 | Chạy lại máy chủ rồi kiểm tra lại danh sách sản phẩm | Dữ liệu reset về ban đầu (3 sản phẩm) | Danh sách chỉ còn 3 sản phẩm ban đầu, sản phẩm 4 và 5 đã mất | Dữ liệu lưu trong bộ nhớ RAM (`List<T>`) của tiến trình máy chủ sẽ mất khi tiến trình kết thúc. Trong hệ thống phân tán thực tế cần cơ sở dữ liệu bền vững. |
| 4 | Chạy máy khách với cổng sai (5999) | Lỗi không kết nối được | `HttpRequestException: Connection refused (localhost:5999)` | Hai bên bắt buộc phải thống nhất địa chỉ giao tiếp (URL/port). Sai cổng là máy khách không thể tìm thấy máy chủ. |

### 2.3 Phiếu 10 ô tự kiểm tra (Bước 10)

| # | Yêu cầu | Mong đợi | Tự đánh giá |
|:---:|---|---|:---:|
| 1 | `GET /` | `200 OK` | Đạt [x] |
| 2 | `GET /api/products` | `200 OK`, 3 phần tử ban đầu | Đạt [x] |
| 3 | `GET /api/products/1` | `200 OK` | Đạt [x] |
| 4 | `GET /api/products/999` | `404 Not Found` + `PRODUCT_NOT_FOUND` | Đạt [x] |
| 5 | `GET /api/products/abc` | `404 Not Found` rỗng (framework chặn trước handler) | Đạt [x] |
| 6 | `POST` hợp lệ | `201 Created` + header `Location` | Đạt [x] |
| 7 | `POST` giá âm / rỗng | `400 Bad Request` + `INVALID_PRODUCT` | Đạt [x] |
| 8 | `POST` thiếu `Content-Type` | `415 Unsupported Media Type` | Đạt [x] |
| 9 | Máy khách chạy | Đủ 4 nấc (in danh sách, tạo mới, đọc lại, xử lý lỗi 400) | Đạt [x] |
| 10 | Tắt máy chủ, chạy máy khách | Báo lỗi kết nối (`Connection refused`) | Đạt [x] |

---

## 3. Phần 2: Áp dụng sang EduSubmit

### 3.1 Thiết kế và Hợp đồng API
EduSubmit quản lý việc nộp bài tập phân tán tại cổng `5000` (`http://localhost:5000`):
- Tài nguyên cha: `/api/assignments` (Bài tập)
- Tài nguyên con: `/api/assignments/{assignmentId}/submissions` (Lượt nộp của bài tập)
- Đọc lại lượt nộp: `/api/submissions/{submissionId}` (Định danh toàn cục qua `Guid`)

| Phương thức | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| `GET` | `/` | `200 OK` + `{"service":"Submission.Api","status":"running"}` | – |
| `GET` | `/api/assignments` | `200 OK` + danh sách bài tập | – |
| `GET` | `/api/assignments/{assignmentId:int}` | `200 OK` + chi tiết bài tập | `404 Not Found` (ASSIGNMENT_NOT_FOUND) |
| `POST` | `/api/assignments/{assignmentId:int}/submissions` | `201 Created` + Location + SubmissionDto | `404 Not Found` (ASSIGNMENT_NOT_FOUND)<br>`400 Bad Request` (ASSIGNMENT_CLOSED)<br>`400 Bad Request` (INVALID_SUBMISSION)<br>`409 Conflict` (DUPLICATE_SUBMISSION) |
| `GET` | `/api/submissions/{submissionId:guid}` | `200 OK` + chi tiết lượt nộp | `404 Not Found` (SUBMISSION_NOT_FOUND) |
| `DELETE` | `/api/submissions/{submissionId:guid}` | `204 NoContent` (rút bài thành công) | `404 Not Found` (SUBMISSION_NOT_FOUND) |

### 3.2 Quy trình kiểm tra 3 cổng + mở rộng (POST submissions)
Khi client gửi yêu cầu nộp bài, máy chủ thực hiện kiểm tra theo thứ tự:
1. **Bài tập có tồn tại không?** Nếu `assignment is null` → `404 ASSIGNMENT_NOT_FOUND`.
2. **Bài tập còn mở không?** Nếu `!assignment.IsOpen` → `400 ASSIGNMENT_CLOSED`.
3. **Dữ liệu có hợp lệ không?** Nếu thiếu `StudentCode` hoặc `RepositoryUrl` → `400 INVALID_SUBMISSION`.
4. **Có bị nộp trùng không?** Nếu sinh viên đã có lượt nộp cho bài tập này → `409 DUPLICATE_SUBMISSION`.
5. **Thành công:** Tự sinh `Id` (GUID), `Status` ("received"), thời điểm `ReceivedAt` (UtcNow), lưu vào danh sách và trả về `201 Created` kèm header `Location`.

### 3.3 Kết quả chạy Submission.Client
Chạy lệnh: `dotnet run --project src/Submission.Client -- http://localhost:5000`
```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client
- 3: Bai tap Mo rong (da dong)

Da tao luot nop: f0c75588-b1ab-495f-a705-349f547bdeb5
Trang thai ban dau: received
Doc lai tai: /api/submissions/f0c75588-b1ab-495f-a705-349f547bdeb5
Doc lai luot nop: f0c75588-b1ab-495f-a705-349f547bdeb5 (SV001 - https://example.com/sv001/rest-api)

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

---

## 4. Trả lời ba câu hỏi giải thích (Slide 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Bản chất của REST:** Kiến trúc REST (Representational State Transfer) được xây dựng xoay quanh khái niệm **Tài nguyên (Resources)**. Mỗi tài nguyên là một thực thể dữ liệu trong hệ thống và được định danh duy nhất bằng một **URI** (Uniform Resource Identifier). Do đó, URI phải là các danh từ (ví dụ: `/api/assignments`, `/api/products`, `/api/submissions`), chỉ đóng vai trò "gọi tên" tài nguyên.
- **Vai trò của HTTP Method:** Mọi thao tác hay hành vi nghiệp vụ đối với tài nguyên đó được chuẩn hóa thông qua các động từ có sẵn trong giao thức HTTP (`GET` để lấy biểu diễn, `POST` để tạo mới tài nguyên con, `PUT` để cập nhật toàn bộ, `PATCH` để sửa một phần, `DELETE` để xóa).
- **Lợi ích:** Tránh việc bùng nổ các URI kiểu gọi hàm RPC (`/doSubmit`, `/getAssignments`, `/deleteProduct`), giúp giao diện API nhất quán (Uniform Interface), dễ hiểu, chuẩn hóa việc xử lý lỗi, đồng thời cho phép các thành phần trung gian (như Web Cache, Proxy, API Gateway) hiểu ngữ nghĩa của yêu cầu mà không phụ thuộc vào mã nguồn bên trong của server.

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Đặc trưng Stateless (Phi trạng thái):** Mỗi yêu cầu HTTP từ máy khách gửi đến máy chủ phải chứa đựng đầy đủ toàn bộ thông tin cần thiết để máy chủ hiểu và thực thi yêu cầu đó một cách độc lập. Máy chủ không lưu giữ bất kỳ ngữ cảnh hội thoại hay biến phiên (Session state) nào giữa các yêu cầu trong bộ nhớ.
- **Thể hiện trong code:**
  - Ở Catalog API: Endpoint `POST /api/products` nhận toàn bộ thuộc tính sản phẩm (`Name`, `Price`, `Description`) ngay trong body JSON của mỗi request. Server không cần biết trước đó client đã làm gì.
  - Ở EduSubmit: Endpoint `POST /api/assignments/{assignmentId}/submissions` nhận `assignmentId` trực tiếp từ URI và `StudentCode`, `RepositoryUrl` từ body JSON. Client không cần một request "chọn bài tập" trước đó để server lưu vào session; mỗi request nộp bài là hoàn toàn tự thân.
  - **Lưu ý quan trọng:** Stateless không có nghĩa là "server không lưu dữ liệu". Dữ liệu nghiệp vụ (danh sách bài tập, sản phẩm, các lượt nộp) vẫn được lưu trong bộ nhớ hoặc CSDL; Stateless ở đây là server không duy trì trạng thái cuộc trò chuyện với client. Nhờ đó, hệ thống có thể mở rộng (scale-out) dễ dàng bằng cách thêm các instance server phía sau bộ cân bằng tải (load balancer).

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Biên hệ thống và sự tiến hóa độc lập:** Máy khách và máy chủ là hai chương trình hoàn toàn riêng biệt, biên dịch độc lập, chạy trên hai tiến trình khác nhau (và có thể chạy trên hai máy khác nhau ở môi trường thực tế). Hai bên không dùng chung mã nguồn và không thể tham chiếu lẫn nhau qua bộ nhớ C#.
- **Hợp đồng dữ liệu qua mạng:** Cầu nối duy nhất giữa hai bên là thông điệp HTTP và dữ liệu được tuần tự hóa (serialized) dưới dạng chuỗi byte **JSON**. Hợp đồng API quy định cấu trúc các trường JSON (tên trường dạng camelCase, kiểu dữ liệu chuỗi/số/boolean).
- **Độc lập kiểu dữ liệu:** Server định nghĩa DTO của mình (`ProductDto`, `SubmissionDto`) và Client cũng tự định nghĩa DTO của mình. Miễn là tên thuộc tính ánh xạ tương ứng với các trường trong chuỗi JSON, thư viện JSON serializer (`System.Net.Http.Json`) sẽ tự động chuyển đổi qua lại chính xác. Điều này giúp ngăn lộ các thực thể hay thông tin nội bộ của máy chủ (như điểm số `RawScore`, ghi chú của giáo viên `GraderNote`) và cho phép một bên cập nhật mà không làm hỏng phía bên kia.
