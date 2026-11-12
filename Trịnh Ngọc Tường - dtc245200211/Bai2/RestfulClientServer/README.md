# EduSubmit — RestfulClientServer (Phần 3, Bài 2)

Hệ thống nộp bài tập phân tán gồm 2 tiến trình giao tiếp qua HTTP + JSON:
- `src/Submission.Api`: ASP.NET Core Minimal API lắng nghe tại `http://localhost:5000`
- `src/Submission.Client`: Console Client dùng `HttpClient`
- `requests.http`: Bộ 12 ca kiểm thử (bao phủ `200`, `201`, `204`, `400`, `404`, `409`)

## Lệnh build và chạy

Build cả solution:
```powershell
cd RestfulClientServer
dotnet build
```

Terminal 1 — chạy máy chủ:
```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2 — chạy máy khách:
```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

## Kết quả kiểm thử thực tế (bảng phá thử & các mã trạng thái)

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
| `9g` | Chạy `Submission.Client` lần 1 → lần 2 | Lần 1: 4 nấc thành công (`200`, `201`, `200`, `400`); Lần 2: `409 Conflict` (`DUPLICATE_SUBMISSION`) | Client + Handler |

## Trả lời 3 câu hỏi giải thích (Slide 90)

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   URI định danh *tài nguyên* mà API quản lý (ví dụ `/api/assignments`, `/api/assignments/{assignmentId}/submissions`, `/api/submissions/{submissionId}`), giúp giữ cấu trúc địa chỉ thống nhất theo cây tài nguyên. Còn hành động trên tài nguyên đó (đọc `GET`, tạo mới `POST`, xóa `DELETE`) đã có sẵn ngữ nghĩa chuẩn của giao thức HTTP (kèm tính chất an toàn và lũy đẳng), nên không lặp lại động từ như `/getAssignments` hay `/doSubmit` trong URI.

2. **“Stateless” thể hiện ở đâu trong code đã viết?**
   Trong `Submission.Api`, server không lưu phiên làm việc (session) hay trạng thái "sinh viên đang chọn bài tập nào" giữa các request. Mỗi request tự mang đủ thông tin để server xử lý độc lập: `assignmentId` nằm ngay trên đường dẫn (`POST /api/assignments/{assignmentId:int}/submissions`), dữ liệu nộp (`studentCode`, `repositoryUrl`) nằm trong body JSON, hoặc `submissionId` nằm trên URI (`GET/DELETE /api/submissions/{submissionId:guid}`). Server vẫn lưu dữ liệu tài nguyên (`assignments`, `submissions`), nhưng không nhớ hội thoại trước đó của client.

3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   `Submission.Client` và `Submission.Api` là hai chương trình chạy ở hai tiến trình độc lập, không chia sẻ bộ nhớ hay assembly mã nguồn. Hai bên giao tiếp thông qua **hợp đồng API** (chuỗi JSON với tên trường camelCase khớp với các thuộc tính `AssignmentDto`, `CreateSubmissionRequest`, `SubmissionDto`, `ApiError`). Khi gửi/nhận, `System.Text.Json` tuần tự hóa (serialize) và giải tuần tự hóa (deserialize) JSON theo tên thuộc tính (không phân biệt hoa thường mặc định trong `HttpClientJsonExtensions`), nên miễn tên và kiểu dữ liệu khớp hợp đồng thì hai bên vẫn hiểu nhau dù khai báo `record` riêng.
