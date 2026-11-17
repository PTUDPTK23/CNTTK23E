# Bài 2: RESTful API cho tương tác Client–Server

- **Sinh viên:** Trịnh Ngọc Tường
- **MSSV:** `dtc245200211`
- **Môn học:** Phát triển ứng dụng phân tán (CNTT K23E)

## Cấu trúc thư mục nộp bài

| Thư mục / Tệp | Nội dung |
|---|---|
| [`RestfulClientServer/`](RestfulClientServer/) | **Phần 3 — EduSubmit**: `Submission.Api` (cổng `5000`) + `Submission.Client` + `requests.http` (12 ca, gồm mở rộng `409 Conflict` và `DELETE 204 No Content`) |
| [`CatalogApi/`](CatalogApi/) | **Phần 2 & Vận dụng (Slide 104 #1–4)**: `Catalog.Api` (cổng `5100`) + `Catalog.Client` + `requests.http` (12 ca, gồm `?keyword=`, `DELETE`, `NAME_TOO_LONG`) |
| [`LibraryApi/`](LibraryApi/) | **Vận dụng & Đổi miền (Slide 104 #5 & Slide 105)**: `Library.Api` (cổng `5200`) + `Library.Client` + bảng 5 câu hỏi thiết kế + bảng hợp đồng API + `requests.http` (11 ca) |
| [`docs/bai2.md`](docs/bai2.md) | Nhật ký thực hành chi tiết (lệnh đã chạy, bảng thí nghiệm, bảng phá thử, giải thích 3 câu hỏi Slide 90) |

---

## Lệnh build & chạy nhanh (EduSubmit — `RestfulClientServer`)

```powershell
cd RestfulClientServer
dotnet build
```

Terminal 1 (máy chủ):
```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2 (máy khách):
```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Kết quả chạy `Submission.Client` lần 1 (đủ 4 nấc):
```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client

Da tao luot nop: e4186c74-399a-4edf-8ed8-85475651c711
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

Kết quả chạy `Submission.Client` lần 2 (kiểm tra `409 Conflict`):
```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client

Khong the nop bai: 409 Conflict
DUPLICATE_SUBMISSION: Sinh vien da nop bai tap nay.
```

---

## Bảng phá thử & các mã trạng thái đã kiểm thử (`RestfulClientServer`)

| Ca | Yêu cầu | Kết quả quan sát được | Bên trả lời |
|---|---|---|---|
| `2` | `GET /` | `200 OK` (`{"service":"Submission.Api","status":"running"}`) | Handler |
| `3` | `GET /api/assignments` | `200 OK` (3 bài tập: 1, 2 mở; 3 đóng) | Handler |
| `4a` | `GET /api/assignments/1` | `200 OK` | Handler |
| `4b` | `GET /api/assignments/999` | `404 Not Found` + `ASSIGNMENT_NOT_FOUND` | Handler |
| `4c` | `GET /api/assignments/abc` | `404 Not Found`, `Content-Length: 0` (body rỗng) | Framework (`:int`) |
| `5a` | `POST /api/assignments/1/submissions` (`sv001.json`) | `201 Created` + header `Location: /api/submissions/{id}` | Handler |
| `5b` | `POST /api/assignments/1/submissions` (`rong.json`) | `400 Bad Request` + `INVALID_SUBMISSION` | Handler |
| `5c` | `POST /api/assignments/999/submissions` | `404 Not Found` + `ASSIGNMENT_NOT_FOUND` | Handler |
| `5d` | `POST /api/assignments/1/submissions` (thiếu `Content-Type`) | `415 Unsupported Media Type` | Framework |
| `5e` | `POST /api/assignments/1/submissions` (JSON hỏng) | `400 Bad Request` (`BadHttpRequestException` text) | Framework |
| `5f` | `POST /api/assignments/1/submissions` (thiếu `repositoryUrl`) | `400 Bad Request` + `INVALID_SUBMISSION` | Handler |
| `5g` | `PUT /api/assignments/1` | `405 Method Not Allowed`, `Allow: GET` | Framework |
| `5h / 9a` | `POST /api/assignments/3/submissions` (bài đã đóng) | `400 Bad Request` + `ASSIGNMENT_CLOSED` | Handler |
| `6a` | `GET` theo `Location` | `200 OK` + đúng `SubmissionDto` | Handler |
| `6b` | `GET /api/submissions/00000000-0000-0000-0000-000000000000` | `404 Not Found` + `SUBMISSION_NOT_FOUND` | Handler |
| `9c` | `POST` trùng `SV001` vào bài tập 1 | `409 Conflict` + `DUPLICATE_SUBMISSION` | Handler |
| `9d` | `DELETE /api/submissions/{id}` | `204 No Content` | Handler |
| `9e` | `DELETE` lại lượt nộp đã xóa | `404 Not Found` + `SUBMISSION_NOT_FOUND` | Handler |
| `9g` | Chạy `Submission.Client` lần 1 → lần 2 | Lần 1: 4 nấc thành công; Lần 2: `409 Conflict` (`DUPLICATE_SUBMISSION`) | Client + Handler |

---

## Trả lời 3 câu hỏi giải thích (Slide 90)

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   URI định danh *tài nguyên* mà API quản lý (ví dụ `/api/assignments`, `/api/assignments/{assignmentId}/submissions`, `/api/submissions/{submissionId}`), giữ cấu trúc địa chỉ nhất quán theo cây tài nguyên. Còn hành động trên tài nguyên đó (đọc `GET`, tạo mới `POST`, xóa `DELETE`) đã có ngữ nghĩa chuẩn của HTTP (kèm tính chất an toàn và lũy đẳng), nên không lặp lại động từ như `/getAssignments` hay `/doSubmit` trong URI.

2. **“Stateless” thể hiện ở đâu trong code đã viết?**
   Trong `Submission.Api`, server không lưu phiên làm việc (session) hay trạng thái "sinh viên đang chọn bài tập nào" giữa các request. Mỗi request tự mang đủ thông tin để server xử lý độc lập: `assignmentId` nằm ngay trên URI (`POST /api/assignments/{assignmentId:int}/submissions`), thông tin nộp nằm trong body JSON, hoặc `submissionId` nằm trên URI (`GET/DELETE /api/submissions/{submissionId:guid}`). Server vẫn lưu dữ liệu tài nguyên (`assignments`, `submissions`), nhưng không nhớ ngữ cảnh hội thoại trước đó của client.

3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   `Submission.Client` và `Submission.Api` là hai chương trình chạy ở hai tiến trình độc lập, không dùng chung bộ nhớ hay mã nguồn. Hai bên chỉ ràng buộc qua **hợp đồng API** (chuỗi JSON với tên trường camelCase tương ứng với các thuộc tính của `AssignmentDto`, `CreateSubmissionRequest`, `SubmissionDto`, `ApiError`). Khi truyền qua HTTP, `System.Text.Json` tuần tự hóa/giải tuần tự hóa theo tên thuộc tính (không phân biệt hoa/thường mặc định trong `HttpClientJsonExtensions`), nên chỉ cần tên và kiểu trường khớp hợp đồng thì hai tiến trình hiểu đúng nhau.
