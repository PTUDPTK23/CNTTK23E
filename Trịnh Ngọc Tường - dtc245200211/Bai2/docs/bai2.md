# Nhật ký thực hành Bài 2: RESTful API cho tương tác Client–Server

- **Sinh viên:** Trịnh Ngọc Tường — **MSSV:** `dtc245200211`
- **Các dự án đã thực hiện:**
  - `CatalogApi/`: Phần 2 (10 bước Catalog API) + Phần Vận dụng Slide 104 (`?keyword=`, `DELETE`, `NAME_TOO_LONG`, so sánh 2 kiểu `404`)
  - `RestfulClientServer/`: Phần 3 (EduSubmit — `Submission.Api` & `Submission.Client` + mở rộng `409 Conflict` & `DELETE 204 No Content`)
  - `LibraryApi/`: Bài 5 (Slide 104) & Nhiệm vụ Đổi miền (Slide 105)

---

## 1. Bảng thí nghiệm phân tán (Bước 9 — Catalog API & EduSubmit)

| # | Thí nghiệm | Dự đoán | Kết quả quan sát được | Giải thích / Bài học |
|---|---|---|---|---|
| 1 | Chạy `Catalog.Client` lần 2 khi `Catalog.Api` vẫn đang chạy | Danh sách in thêm sản phẩm vừa tạo ở lần 1 | Danh sách có 4 sản phẩm (có `[4] Nhap mon REST API`), lần 2 tạo tiếp `[5]` | Trạng thái (`products` / `submissions`) nằm trong bộ nhớ của tiến trình máy chủ, không nằm ở máy khách |
| 2 | Tắt máy chủ (`Ctrl+C`) rồi chạy Client | Client không kết nối được, báo lỗi mạng | `HttpRequestException: No connection could be made because the target machine actively refused it.` | Lỗi đặc trưng của hệ phân tán: tiến trình bên kia có thể vắng mặt hoặc chưa chạy |
| 3 | Chạy lại máy chủ rồi `GET /api/products` | Dữ liệu tạo ở phiên trước không còn | Danh sách quay về 3 sản phẩm mặc định ban đầu | Dữ liệu lưu trong RAM bị mất khi tiến trình máy chủ dừng |
| 4 | Chạy Client trỏ nhầm cổng (`http://localhost:5999`) | Không gặp được máy chủ ở cổng đúng | `HttpRequestException: Connection refused (localhost:5999)` | Hai tiến trình giao tiếp qua mạng bắt buộc phải thống nhất đúng URL và cổng |

---

## 2. Kết quả kiểm thử EduSubmit (`RestfulClientServer`)

Lệnh build & chạy:
```powershell
cd RestfulClientServer
dotnet build
dotnet run --project src/Submission.Api --urls http://localhost:5000
dotnet run --project src/Submission.Client -- http://localhost:5000
```

| Ca | Yêu cầu | Kết quả quan sát được | Bên trả lời |
|---|---|---|---|
| `2` | `GET /` | `200 OK` (`{"service":"Submission.Api","status":"running"}`) | Handler |
| `3` | `GET /api/assignments` | `200 OK` (3 bài tập: 1, 2 mở; 3 đóng) | Handler |
| `4a` | `GET /api/assignments/1` | `200 OK` | Handler |
| `4b` | `GET /api/assignments/999` | `404 Not Found` + `ASSIGNMENT_NOT_FOUND` | Handler |
| `4c` | `GET /api/assignments/abc` | `404 Not Found`, `Content-Length: 0` (body rỗng) | Framework (`:int`) |
| `5a` | `POST /api/assignments/1/submissions` | `201 Created` + header `Location: /api/submissions/{id}` | Handler |
| `5b` | `POST /api/assignments/1/submissions` (rỗng) | `400 Bad Request` + `INVALID_SUBMISSION` | Handler |
| `5c` | `POST /api/assignments/999/submissions` | `404 Not Found` + `ASSIGNMENT_NOT_FOUND` | Handler |
| `5d` | `POST` thiếu `Content-Type` | `415 Unsupported Media Type` | Framework |
| `5e` | `POST` body JSON hỏng | `400 Bad Request` | Framework |
| `5f` | `POST` thiếu `repositoryUrl` | `400 Bad Request` + `INVALID_SUBMISSION` | Handler |
| `5g` | `PUT /api/assignments/1` | `405 Method Not Allowed`, `Allow: GET` | Framework |
| `5h / 9a` | `POST /api/assignments/3/submissions` (đã đóng) | `400 Bad Request` + `ASSIGNMENT_CLOSED` | Handler |
| `6a` | `GET` theo `Location` | `200 OK` + đúng `SubmissionDto` | Handler |
| `6b` | `GET /api/submissions/00000000-0000-0000-0000-000000000000` | `404 Not Found` + `SUBMISSION_NOT_FOUND` | Handler |
| `9c` | `POST` trùng `SV001` vào bài 1 | `409 Conflict` + `DUPLICATE_SUBMISSION` | Handler |
| `9d` | `DELETE /api/submissions/{id}` | `204 No Content` | Handler |
| `9e` | `DELETE` lại lượt nộp đã xóa | `404 Not Found` + `SUBMISSION_NOT_FOUND` | Handler |

---

## 3. Trả lời 3 câu hỏi giải thích (Slide 90)

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   URI định danh *tài nguyên* mà API quản lý (`/api/assignments`, `/api/assignments/{assignmentId}/submissions`, `/api/submissions/{submissionId}`), giữ cây địa chỉ rõ ràng và nhất quán. Hành động trên tài nguyên do phương thức HTTP (`GET`, `POST`, `DELETE`) đảm nhận theo ngữ nghĩa chuẩn (an toàn, lũy đẳng), tránh đặt động từ vào URI.
2. **“Stateless” thể hiện ở đâu trong code đã viết?**
   Mỗi HTTP request tự mang đủ dữ liệu để `Submission.Api` xử lý độc lập (`assignmentId` hoặc `submissionId` trên route, dữ liệu nộp trong JSON body); server không lưu session trạng thái hội thoại giữa các request của client.
3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   Hai tiến trình tách biệt chỉ chia sẻ hợp đồng JSON qua HTTP; `System.Text.Json` khớp dữ liệu theo tên và kiểu trường của hợp đồng chứ không phụ thuộc vào cùng một class/assembly.

---

## 4. Phần mở rộng Vận dụng (Slide 104) & Đổi miền (Slide 105)

- **CatalogApi (`CatalogApi/`)**: Bổ sung `GET /api/products?keyword=REST` (`200 OK`), `DELETE /api/products/{id:int}` (`204 No Content` / `404 PRODUCT_NOT_FOUND`), kiểm tra `NAME_TOO_LONG` (`400`) đặt sau `INVALID_PRODUCT`, và đối chiếu header/body hai kiểu `404` (`/api/products/999` vs `/api/products/abc`) trong [`CatalogApi/README.md`](../CatalogApi/README.md).
- **LibraryApi (`LibraryApi/`)**: Thiết kế và chạy thử ứng dụng quản lý sách (`/api/books`) gồm bảng 5 câu hỏi thiết kế ①–⑤, bảng hợp đồng API, `requests.http` 11 ca bao phủ `200`, `201`, `400`, `404`, `409`, `405`, `415` và `Library.Client` 4 nấc trong [`LibraryApi/README.md`](../LibraryApi/README.md).
