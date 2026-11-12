# EduSubmit – Bài 2

EduSubmit là bài thực hành nộp bài qua REST API. API giữ dữ liệu trong bộ nhớ và có hai bài tập mở (ID 1, 2), cùng một bài tập đã đóng (ID 3) để thử quy tắc nộp bài.

## Chạy trên Windows

Dùng .NET 10 SDK. Từ thư mục chứa `EduSubmit.slnx`, chạy trong terminal thứ nhất:

```powershell
dotnet build .\EduSubmit.slnx
dotnet run --project .\src\Submission.Api --urls http://localhost:5000
```

Giữ máy chủ chạy rồi mở terminal thứ hai ở cùng thư mục:

```powershell
dotnet run --project .\src\Submission.Client -- http://localhost:5000
```

Máy khách thực hiện bốn thao tác: liệt kê bài tập đang mở, nộp bài và đọc mã `201`, đọc lại lượt nộp qua header `Location`, rồi gửi dữ liệu rỗng để đọc lỗi `400 INVALID_SUBMISSION`.

## Kiểm tra API

Mở `requests.http` bằng REST Client trong VS Code. Gửi yêu cầu **Nop bai hop le** trước yêu cầu **Doc lai luot nop** để VS Code lấy ID từ phản hồi.

| Yêu cầu | Phản hồi đã kiểm tra |
| --- | --- |
| `GET /`, danh sách bài tập, chi tiết bài tập 1 | `200 OK` |
| Xem hoặc nộp vào bài tập không tồn tại | `404 ASSIGNMENT_NOT_FOUND` |
| Tra bài tập với ID `abc` | `404` với body rỗng |
| Nộp bài hợp lệ | `201 Created`, `Location: /api/submissions/{id}`, trạng thái `received` |
| Đọc lại lượt nộp mới hoặc tra một GUID chưa tồn tại | `200` cho lượt đã tạo, `404 SUBMISSION_NOT_FOUND` cho GUID chưa tồn tại |
| Nộp dữ liệu rỗng hoặc thiếu `repositoryUrl` | `400 INVALID_SUBMISSION` |
| Nộp vào bài tập đã đóng | `400 ASSIGNMENT_CLOSED` |
| Gửi JSON sai cú pháp hoặc thiếu header JSON | Lần lượt `400` và `415` |
| Gửi `PUT /api/assignments/1` | `405 Method Not Allowed`, `Allow: GET` |

## Thử lỗi kết nối và khởi động lại

| Thao tác | Kết quả |
| --- | --- |
| Tắt API rồi chạy client tới cổng 5000 | Client báo lỗi kết nối. |
| Giữ API chạy ở cổng 5000 rồi chạy client tới cổng 5999 | Client báo lỗi kết nối tới 5999. |
| Khởi động lại API rồi gửi GET tới `Location` cũ | API trả `404 SUBMISSION_NOT_FOUND` vì lượt nộp nằm trong bộ nhớ. |

## Ba câu hỏi

1. **Vì sao URI dùng danh từ?** URI chỉ tài nguyên, ví dụ `/api/assignments/1/submissions`. Phương thức `POST` cho biết thao tác là tạo một lượt nộp cho bài tập 1.
2. **Stateless thể hiện ở đâu?** Mỗi yêu cầu gửi đủ ID và dữ liệu để API xử lý. Khi đọc lại lượt nộp, client gửi GUID trong URL. API tra dữ liệu bằng GUID mà không cần phiên đăng nhập hay ghi nhớ yêu cầu trước của client.
3. **Vì sao client và API khai báo DTO riêng?** Hai chương trình dùng chung hợp đồng JSON. Miễn tên và kiểu của các trường như `id`, `assignmentId` và `status` khớp nhau, client có thể đọc phản hồi mà không cần tham chiếu project API.
