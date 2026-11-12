# Bài 2 - RESTful API cho tương tác client-server

Solution này hiện thực bài thực hành EduSubmit trong PDF bằng .NET 10 Minimal API và một console client.

## Cấu trúc

- `src/Submission.Api`: máy chủ API, chạy ở `http://localhost:5000`.
- `src/Submission.Client`: máy khách gọi API qua `HttpClient`.
- `requests.http`: các request mẫu, gồm luồng thành công, lỗi nghiệp vụ và lỗi do framework.
- `RestfulClientServer.slnx`: solution chứa cả hai project.

## Chạy

Yêu cầu .NET 10 SDK.

```powershell
dotnet build
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Mở terminal thứ hai để chạy client:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Client thực hiện lần lượt: liệt kê bài tập đang mở, tạo lượt nộp, đọc lại lượt nộp theo `Location`, rồi gửi dữ liệu không hợp lệ và in `ApiError`. Đổi URL cuối lệnh để quan sát lỗi kết nối, ví dụ `http://localhost:5999`.

Có thể chạy từng request trong `requests.http` bằng REST Client của VS Code. Chạy lần lượt từ trên xuống để request tạo lượt nộp có thể được đọc, phát hiện trùng và xóa ở các request sau.

## Hợp đồng API

| Phương thức | URI | Kết quả chính |
| --- | --- | --- |
| `GET` | `/api/assignments` | `200` và danh sách bài tập |
| `GET` | `/api/assignments/{assignmentId:int}` | `200`, hoặc `404 ASSIGNMENT_NOT_FOUND` |
| `POST` | `/api/assignments/{assignmentId:int}/submissions` | `201` + `Location`; lỗi nghiệp vụ trả `400` hoặc `404` |
| `GET` | `/api/submissions/{submissionId:guid}` | `200`, hoặc `404 SUBMISSION_NOT_FOUND` |
| `DELETE` | `/api/submissions/{submissionId:guid}` | `204` khi xóa, hoặc `404 SUBMISSION_NOT_FOUND` |

Mọi lỗi do handler trả về theo dạng `{ "code": "...", "message": "..." }`. Bài tập 1 đang mở; bài tập 2 đóng để thử `ASSIGNMENT_CLOSED`. Lượt nộp và dữ liệu mẫu nằm trong bộ nhớ tiến trình, nên sẽ mất khi dừng hoặc khởi động lại API.

## Ca kiểm tra và kết quả mong đợi

| Ca | Kết quả |
| --- | --- |
| `GET /` và `GET /api/assignments` | `200 OK` |
| `GET /api/assignments/1` | `200 OK` |
| `GET /api/assignments/999` | `404` + `ASSIGNMENT_NOT_FOUND` |
| `GET /api/assignments/abc` | `404` từ routing, body rỗng |
| Nộp hợp lệ cho bài 1 | `201 Created`, body `SubmissionDto`, header `Location` |
| Nộp thiếu mã sinh viên hoặc repository URL | `400` + `INVALID_SUBMISSION` |
| Nộp bài 2 đã đóng | `400` + `ASSIGNMENT_CLOSED` |
| Nộp lần nữa cùng sinh viên và bài tập | `409` + `DUPLICATE_SUBMISSION` |
| Đọc lượt nộp không tồn tại | `404` + `SUBMISSION_NOT_FOUND` |
| Xóa lượt nộp đã tạo | `204 No Content`, không có body |
| Thiếu `Content-Type: application/json` | `415` từ framework |
| JSON sai cú pháp | `400` từ framework |
| `PUT /api/assignments/1` | `405 Method Not Allowed` |
| Chạy client khi API dừng hoặc URL sai | Lỗi kết nối, không có phản hồi HTTP |

Các ca ở trên được mô tả trong `requests.http`; ca tạo/đọc/xóa phụ thuộc thứ tự chạy vì dữ liệu được giữ trong RAM.

## Câu hỏi giải thích

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?** URI định danh tài nguyên, ví dụ bài tập hoặc lượt nộp. Phương thức biểu đạt thao tác trên tài nguyên (`GET` đọc, `POST` tạo, `DELETE` xóa), nên URL ổn định và ý nghĩa request rõ ràng.
2. **“Stateless” thể hiện ở đâu trong code?** Mỗi request mang đủ thông tin để xử lý: `assignmentId` nằm trên route, dữ liệu nộp nằm trong JSON body. Server không dựa vào phiên client trước đó. Các lượt nộp được lưu trong RAM là trạng thái tài nguyên phía server, không phải session giao tiếp của client.
3. **Vì sao client và server khai báo DTO riêng mà vẫn khớp nhau?** Hai phía độc lập định nghĩa các kiểu cục bộ nhưng tuân theo cùng hợp đồng JSON: tên trường, kiểu dữ liệu và cấu trúc phải tương ứng. DTO đầu vào chỉ gồm `StudentCode` và `RepositoryUrl`; server tự tạo `Id`, `AssignmentId`, `Status` và `ReceivedAt` trong DTO phản hồi.

## Tự kiểm tra

Đã chạy `dotnet build RestfulClientServer.slnx --no-restore -m:1 -p:UseSharedCompilation=false`: build thành công, 0 warning, 0 error.

Đã chạy client khi API đang hoạt động; đầu ra gồm:

```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
Da tao luot nop: <id>
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api
Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Ma sinh vien va repository URL khong duoc de trong.
```

Đã gửi request HTTP trực tiếp: `200` cho health/list, `201` kèm `Location`, `400` cho bài đã đóng và JSON sai cú pháp, `404` cho tài nguyên thiếu, `409` cho lượt nộp trùng, `204` khi xóa, `415` khi thiếu Content-Type, và `405` với `Allow: GET` cho `PUT` không hỗ trợ. Chạy API theo lệnh ở trên rồi gửi các request trong `requests.http` theo thứ tự để lặp lại.

Không có thay đổi nào được push lên remote.
