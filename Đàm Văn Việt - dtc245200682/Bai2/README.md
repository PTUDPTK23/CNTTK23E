# Bài 2 – RESTful API

Sinh viên: **Đàm Văn Việt** · MSSV: **dtc245200682** · GitHub: **Viet0810**
Email: dtc245200682@ictu.edu.vn

Thực hiện theo `Bai2_RESTful-API_Send.pdf`: Catalog bước 6–10 (trang 54–63) và EduSubmit phần 3 (trang 64–90).

## Chạy Catalog

Mở terminal tại `D:\ptudpt\bai2\CatalogApi`.

Terminal 1:
```powershell
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```
Terminal 2:
```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```
Nếu API đã chạy ở terminal khác thì dùng luôn API đó. Client in 4 bước: danh sách → tạo sản phẩm → GET theo Location → thử dữ liệu sai và đọc INVALID_PRODUCT.

Mở `CatalogApi/requests.http` bằng extension REST Client trong VS Code để gửi 8 ca thử. Hai file JSON của bạn giữ nguyên vị trí; lệnh từ PDF cần đổi đường dẫn:
```powershell
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@src/Catalog.Api/sp-hop-le.json"
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@src/Catalog.Api/Properties/sp-sai.json"
curl.exe -i -X POST http://localhost:5100/api/products -d "@src/Catalog.Api/sp-hop-le.json"
```
Ba mã mong đợi lần lượt: 201, 400, 415. Lệnh cuối cố ý bỏ header JSON để quan sát framework từ chối, không cần sửa thành công.

## Chạy EduSubmit

Mở hai terminal tại `D:\ptudpt\bai2\RestfulClientServer`.

Terminal 1:
```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```
Terminal 2:
```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```
Client xem bài đang mở → nộp bài → đọc lại theo Location → gửi dữ liệu rỗng để đọc INVALID_SUBMISSION. Bài 1 và 2 mở; bài 3 đóng để thử ASSIGNMENT_CLOSED.

| Phương thức | URI | Thành công | Lỗi nghiệp vụ |
|---|---|---|---|
| GET | /api/assignments | 200 danh sách | — |
| GET | /api/assignments/{assignmentId:int} | 200 bài tập | 404 ASSIGNMENT_NOT_FOUND |
| POST | /api/assignments/{assignmentId:int}/submissions | 201 + Location | 404 ASSIGNMENT_NOT_FOUND; 400 ASSIGNMENT_CLOSED hoặc INVALID_SUBMISSION |
| GET | /api/submissions/{submissionId:guid} | 200 lượt nộp | 404 SUBMISSION_NOT_FOUND |

`RestfulClientServer/requests.http` có 14 request, gồm 7 ca chính và các ca phá thử. Gửi request có tên `createSubmission` trước request đọc lại để lấy ID tự động. Hai file `sv001.json`, `rong.json` phục vụ curl.

## Thiết kế EduSubmit theo 5 câu hỏi

1. Tài nguyên: bài tập (assignment), lượt nộp (submission).
2. Hành động: xem danh sách/chi tiết (GET), tạo lượt nộp thuộc bài tập (POST).
3. Đầu vào: StudentCode, RepositoryUrl. Server tự sinh Guid, trạng thái received, thời gian UTC; AssignmentId lấy từ route.
4. Thất bại: bài không tồn tại, đã đóng, thiếu mã sinh viên/link; ngoài ra có lỗi route, phương thức, kiểu nội dung, JSON hoặc kết nối.
5. Trình tự client: xem bài mở → nộp → đọc lại Location → thử lỗi.

## Nhật ký và kiểm chứng

- Đã thêm hai project Catalog vào solution: trước đó solution chưa có project nên build ở gốc không kiểm tra mã nguồn.
- Hai client kiểm tra mã HTTP trước khi đọc DTO và xử lý lỗi mạng/timeout. So với ví dụ PDF, Catalog đọc lại chính sản phẩm vừa tạo bằng Location.
- Chạy lại kiểm tra bằng `python verify-api.py` tại thư mục Bai2 sau khi build cả hai solution. Script dùng cổng riêng 15100/15000, tự khởi động và tắt các tiến trình do nó tạo; không tắt API của bạn.
- Kết quả thực tế lưu trong `verification-results.txt`, gồm mã HTTP, output client, chạy client hai lần, mất kết nối và dữ liệu mất khi khởi động lại.
- Build: `dotnet build CatalogApi/CatalogApi.slnx` và `dotnet build RestfulClientServer/RestfulClientServer.slnx`. Nếu executable Catalog đang bị khóa, dừng API bằng Ctrl+C trước khi build lại.

| Ca phá thử | Mã mong đợi | Giải thích |
|---|---|---|
| Tên rỗng / giá âm ở Catalog | 400 INVALID_PRODUCT | Handler kiểm tra quy tắc |
| Thiếu mã SV hoặc link repo | 400 INVALID_SUBMISSION | Handler từ chối cả trường thiếu/null |
| Bài tập 999 | 404 ASSIGNMENT_NOT_FOUND | Tài nguyên cha không tồn tại |
| Nộp bài 3 | 400 ASSIGNMENT_CLOSED | Bài tồn tại nhưng đã đóng |
| ID lượt nộp Guid rỗng | 404 SUBMISSION_NOT_FOUND | Route hợp lệ, không tìm thấy dữ liệu |
| ID abc với route int | 404 body rỗng | Framework không khớp route |
| PUT bài tập | 405 | Framework không hỗ trợ phương thức |
| Gửi form thay JSON | 415 | Framework không đọc body theo DTO JSON |
| JSON hỏng | 400 | Framework không giải mã được JSON |
| API dừng / sai cổng | Không có HTTP response | Client báo lỗi kết nối |
| Khởi động lại API | Dữ liệu mới mất | List chỉ tồn tại trong RAM |

Chạy client lần hai tạo thêm bản ghi vì bản cơ bản cho phép nộp lại. Đây là hành vi theo phần chính PDF; 409 chống trùng và DELETE 204 ở trang 102–103 là mở rộng, chưa bật để giữ đúng 4 endpoint nghiệp vụ và chuỗi chạy client cơ bản.

## Ba câu trả lời nộp bài

1. **Vì sao URI là danh từ?** URI định danh tài nguyên; phương thức HTTP chỉ hành động. Cùng `/api/products` dùng GET để đọc và POST để tạo, không cần tên URI chứa động từ.
2. **Stateless ở đâu?** Mỗi request gửi đủ thông tin qua route/body. POST nộp bài có assignmentId trên URI và mã SV/link repo trong body, không phụ thuộc bước trước hay phiên đăng nhập. Server vẫn có thể lưu dữ liệu; stateless không có nghĩa là không có List/CSDL.
3. **DTO riêng vẫn khớp thế nào?** Hai tiến trình trao đổi JSON theo cùng tên thuộc tính và kiểu dữ liệu, không truyền object C# trực tiếp. Serializer dùng camelCase. DTO không cần chung assembly; đổi Name thành Ten ở một phía sẽ làm lệch hợp đồng dù vẫn có thể biên dịch.

## Chuẩn bị nộp theo trang 90

Bài nộp nằm trong `Đàm Văn Việt - dtc245200682/Bai2`. Tiêu đề PR: `Bai2 - dtc245200682 - Đàm Văn Việt`. Nhật ký kết quả chạy nằm trong `verification-results.txt`; chưa đính kèm ảnh chụp terminal. Không tạo các commit giả cho những bước đã làm từ trước.

Phụ lục trang 102–105 (409, DELETE, lọc Catalog, giới hạn tên, đổi miền Library/Clinic) là phần mở rộng, tách khỏi bài cơ bản đã triển khai.
