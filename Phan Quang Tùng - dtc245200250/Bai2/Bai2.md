# Báo cáo Thực hành Bài 2: RESTful API cho tương tác client–server

**Sinh viên:** Phan Quang Tùng  
**Mã sinh viên:** DTC245200250  
**Lớp:** K23.K1.D2.2627.CNTT K23E  
**Học phần:** Phát triển ứng dụng phân tán  

---

## 1. Tổng quan hệ thống
Bài thực hành Bài 2 triển khai hai hệ thống phân tán theo chuẩn RESTful API bằng ASP.NET Core Minimal API và HttpClient:
- **CatalogApi**: Dịch vụ danh mục sản phẩm (cổng 5100).
- **EduSubmit**: Dịch vụ nộp bài tập phân tán (cổng 5000) với quan hệ cha - con giữa bài tập (`/api/assignments`) và lượt nộp (`/api/assignments/{id}/submissions`), kèm cơ chế truy xuất theo GUID (`/api/submissions/{guid}`).

---

## 2. Bảng thí nghiệm phân tán (Bước 9)

| STT | Thí nghiệm | Dự đoán | Kết quả thực tế | Giải thích / Bài học |
|:---:|---|---|---|---|
| 1 | Chạy máy khách lần 2 (máy chủ vẫn chạy) | Danh sách sản phẩm có 4 mục, tạo mới ra mục thứ 5 | Danh sách có 4 sản phẩm, tạo mới ra [5], Doc lai tai: `/api/products/5` | Trạng thái (state) nằm ở máy chủ, không ở máy khách. Cùng một client chạy nhiều lần hoặc nhiều client khác nhau đều thấy dữ liệu chung cập nhật trên máy chủ. |
| 2 | Tắt máy chủ (Ctrl+C), chạy máy khách | Lỗi kết nối vì máy chủ không còn chạy | `Unhandled exception: HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5100)` | Lỗi đặc thù chỉ có ở hệ phân tán: bên kia (máy chủ) có thể vắng mặt do sập, tắt, mất mạng hoặc sai địa chỉ. Hoàn toàn khác với gọi hàm nội bộ (in-process). |
| 3 | Chạy lại máy chủ rồi kiểm tra lại danh sách sản phẩm | Dữ liệu reset về ban đầu (3 sản phẩm) | Danh sách chỉ còn 3 sản phẩm ban đầu, sản phẩm 4 và 5 đã mất | Dữ liệu lưu trong bộ nhớ RAM (`List<T>`) của tiến trình máy chủ sẽ mất khi tiến trình kết thúc. Trong hệ thống phân tán thực tế cần cơ sở dữ liệu bền vững. |
| 4 | Chạy máy khách với cổng sai (5999) | Lỗi không kết nối được | `HttpRequestException: Connection refused (localhost:5999)` | Hai bên bắt buộc phải thống nhất địa chỉ giao tiếp (URL/port). Sai cổng là máy khách không thể tìm thấy máy chủ. |

---

## 3. Phiếu 10 ô tự kiểm tra (Bước 10)

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

## 4. EduSubmit: Quản lý nộp bài tập phân tán

### 4.1 Bảng hợp đồng API EduSubmit

| Phương thức | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| `GET` | `/` | `200 OK` + `{"service":"Submission.Api","status":"running"}` | – |
| `GET` | `/api/assignments` | `200 OK` + danh sách bài tập | – |
| `GET` | `/api/assignments/{assignmentId:int}` | `200 OK` + chi tiết bài tập | `404 Not Found` (ASSIGNMENT_NOT_FOUND) |
| `POST` | `/api/assignments/{assignmentId:int}/submissions` | `201 Created` + Location + SubmissionDto | `404 Not Found` (ASSIGNMENT_NOT_FOUND)<br>`400 Bad Request` (ASSIGNMENT_CLOSED)<br>`400 Bad Request` (INVALID_SUBMISSION)<br>`409 Conflict` (DUPLICATE_SUBMISSION) |
| `GET` | `/api/submissions/{submissionId:guid}` | `200 OK` + chi tiết lượt nộp | `404 Not Found` (SUBMISSION_NOT_FOUND) |
| `DELETE` | `/api/submissions/{submissionId:guid}` | `204 NoContent` (rút bài thành công) | `404 Not Found` (SUBMISSION_NOT_FOUND) |

### 4.2 Kết quả chạy Submission.Client
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

## 5. Trả lời 3 câu hỏi giải thích (Slide 90)

### 1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- Theo nguyên lý REST, URI định danh tài nguyên (resource identifier) - đại diện cho các thực thể dữ liệu trong hệ thống (`/api/assignments`, `/api/products`), không phải lời gọi hàm RPC.
- Các hành động được chuẩn hóa bằng các phương thức HTTP: `GET` (đọc/lấy biểu diễn), `POST` (tạo mới tài nguyên con), `PUT` (thay thế toàn bộ), `PATCH` (sửa một phần), `DELETE` (xóa).
- Việc phân tách rõ ràng này đảm bảo tính nhất quán (Uniform Interface), dễ hiểu, có thể dự đoán được cách dùng và cho phép các thành phần trung gian (proxy, gateway, cache) vận hành tối ưu.

### 2. "Stateless" thể hiện ở đâu trong code đã viết?
- Ràng buộc Stateless quy định mỗi yêu cầu gửi lên server phải tự chứa đủ mọi thông tin cần thiết để server hiểu và xử lý. Server không lưu giữ ngữ cảnh phiên hay lịch sử hội thoại giữa các yêu cầu.
- Thể hiện trong code:
  - Ở Catalog API: `POST /api/products` truyền toàn bộ thông tin sản phẩm trong body JSON.
  - Ở EduSubmit: `POST /api/assignments/{assignmentId}/submissions` truyền `assignmentId` trên URI và `StudentCode`, `RepositoryUrl` trong body. Server không cần phiên làm việc trước đó của client. Bất kỳ instance server nào cũng có thể xử lý yêu cầu một cách độc lập.
  - Lưu ý: Dữ liệu nghiệp vụ vẫn lưu trong danh sách/CSDL, Stateless chỉ yêu cầu server không nhớ cuộc hội thoại giữa 2 yêu cầu.

### 3. Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- Client và server là 2 chương trình độc lập, biên dịch riêng, chạy trên hai tiến trình khác nhau. Hai bên không dùng chung mã nguồn hay bộ nhớ C#.
- Hai bên trao đổi qua thông điệp HTTP và dữ liệu định dạng JSON theo hợp đồng API đã thống nhất.
- Mỗi bên tự định nghĩa DTO (`ProductDto`, `SubmissionDto`), thư viện JSON serializer sẽ tự động chuyển đổi dữ liệu dựa trên tên và kiểu trường khớp nhau (theo chuẩn camelCase). Nhờ đó, server có thể bổ sung các trường nội bộ mà không làm hỏng client, hai bên hoàn toàn độc lập và linh hoạt tiến hóa.
