# EduSubmit — Bài 2

Sinh viên: **Lê Hải Anh — DTC245200851 — CNTT K23E**.

Triển khai phần cơ bản trang 64–90 của PDF: 4 endpoint nghiệp vụ, client 4 bước và requests.http. Có thêm GET / kiểm tra dịch vụ. Bài tập 1 và 2 đang mở; bài 3 đóng để kiểm tra ASSIGNMENT_CLOSED. Dữ liệu chỉ lưu trong RAM, khởi động lại API sẽ mất các lượt nộp.

## Chạy

Mở terminal tại `Bai2_/RestfulClientServer`:

```powershell
dotnet build
```

Terminal 1:

```powershell
dotnet run --project .\src\Submission.Api --no-launch-profile --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project .\src\Submission.Client -- http://localhost:5000
```

Client xem bài đang mở → nộp với mã DTC245200851 → đọc lại theo Location → gửi dữ liệu rỗng để đọc lỗi 400. Client kiểm tra mã trạng thái trước khi đọc DTO, xử lý lỗi kết nối và timeout. Nhấn Ctrl+C ở terminal API trước khi build hoặc chạy lại. Catalog dùng cổng 5100, EduSubmit dùng 5000.

## Hợp đồng API

| Phương thức | URI | Thành công | Lỗi nghiệp vụ |
|---|---|---|---|
| GET | /api/assignments | 200, danh sách | — |
| GET | /api/assignments/{assignmentId:int} | 200 | 404 ASSIGNMENT_NOT_FOUND |
| POST | /api/assignments/{assignmentId:int}/submissions | 201 + Location | 404 ASSIGNMENT_NOT_FOUND; 400 ASSIGNMENT_CLOSED; 400 INVALID_SUBMISSION |
| GET | /api/submissions/{submissionId:guid} | 200 | 404 SUBMISSION_NOT_FOUND |

POST kiểm tra bài tồn tại → còn mở → mã sinh viên và URL repo không trống. Server Trim dữ liệu và tự sinh Guid, trạng thái received, ReceivedAt UTC. Body đầu vào chỉ có StudentCode và RepositoryUrl. Mọi lỗi nghiệp vụ trả ApiError(Code, Message). Không kiểm tra URL truy cập được vì PDF chỉ yêu cầu trường không trống.

## Kiểm tra

Mở `requests.http` với extension REST Client. Bảy request đầu bao phủ yêu cầu nộp cơ bản; gửi request createSubmission trước request đọc lại. Các request bổ sung kiểm tra bài đóng, thiếu trường, JSON hỏng, 415, 405 và route sai kiểu.

```powershell
curl.exe -i http://localhost:5000/api/assignments
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@sv001.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@rong.json"
```

## Giải thích

1. URI dùng danh từ để xác định bài tập và lượt nộp; GET đọc dữ liệu, POST tạo tài nguyên. Lượt nộp nằm dưới bài tập vì assignmentId thể hiện tài nguyên cha.
2. Stateless: từng request chứa đủ assignmentId trên route và dữ liệu trong body; không phụ thuộc bước trước hoặc phiên của client. Danh sách trong RAM là dữ liệu nghiệp vụ.
3. Hai bên khai báo DTO riêng nhưng dùng cùng tên thuộc tính và kiểu JSON. Không cần tham chiếu project API từ client; thay đổi hợp đồng phải thống nhất hai bên.

## Kết quả đã chạy kiểm tra

- Build solution: 0 cảnh báo, 0 lỗi.
- GET danh sách, chi tiết và GET /: 200.
- POST hợp lệ: 201; kiểm tra Guid, assignmentId, status received, thời gian nhận, Trim dữ liệu và Location. GET Location trả đúng dữ liệu vừa tạo.
- Bài không tồn tại: 404 ASSIGNMENT_NOT_FOUND; lượt nộp không tồn tại: 404 SUBMISSION_NOT_FOUND.
- Bài đóng: 400 ASSIGNMENT_CLOSED. Body rỗng hoặc thiếu repositoryUrl: 400 INVALID_SUBMISSION.
- Sai Content-Type: 415; JSON hỏng: 400; PUT bài tập: 405 và Allow: GET; route abc: 404 rỗng.
- Client chạy đủ 4 bước, dùng mã DTC245200851 và đọc lại theo Location.
- Tắt API và sai cổng 5999: client báo lỗi kết nối, mã thoát 1.
- Khởi động lại API: id lượt nộp đã tạo trước đó trả 404 SUBMISSION_NOT_FOUND.

## Phá thử

| Thử nghiệm | Ý nghĩa |
|---|---|
| Tắt API rồi chạy client | Lỗi kết nối, không có phản hồi HTTP |
| Chạy client với cổng 5999 | Sai địa chỉ dịch vụ gây lỗi kết nối |
| Khởi động lại API rồi GET id lượt nộp cũ | 404 SUBMISSION_NOT_FOUND vì dữ liệu RAM bị mất |
| Gửi /api/assignments/abc | 404 rỗng do framework, khác /999 trả ApiError |

Chưa triển khai mở rộng tùy chọn 409/DELETE 204 ở phụ lục. Cần bổ sung ảnh minh chứng chạy thực tế trước khi nộp; phần này chưa được commit/push lên GitHub.
