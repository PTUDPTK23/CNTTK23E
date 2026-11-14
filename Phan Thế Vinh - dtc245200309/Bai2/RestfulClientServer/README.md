# Bài 2 – RESTful API cho tương tác Client–Server (EduSubmit)

- **Họ và tên:** Phan Thế Vinh
- **MSSV:** DTC245200309
- **Lớp:** CNTT K23E
- **GitHub:** `phan-the-vinh`
- **Cấu trúc nộp bài (Slide 90):** `<Họ tên - MSSV>/Bai2`
- **Tiêu đề PR dự kiến:** `Bai2 - DTC245200309 - Phan Thế Vinh`

---

## 1. Cấu trúc solution và hợp đồng API

Solution `RestfulClientServer.slnx` gồm 2 project độc lập (.NET 10, không tham chiếu project trực tiếp giữa API và Client):

```text
RestfulClientServer/
├── .gitignore
├── RestfulClientServer.slnx
├── README.md
├── requests.http
├── sv001.json
├── rong.json
├── docs/
│   └── images/
└── src/
    ├── Submission.Api/
    │   ├── Program.cs
    │   ├── Submission.Api.csproj
    │   └── Properties/launchSettings.json
    └── Submission.Client/
        ├── Program.cs
        └── Submission.Client.csproj
```

### Hợp đồng API bắt buộc (`Submission.Api`, cổng `5000`)

| Phương thức | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| `GET` | `/` | `200 OK` + `{ service, status }` | – |
| `GET` | `/api/assignments` | `200 OK` + mảng `AssignmentDto` | – |
| `GET` | `/api/assignments/{assignmentId:int}` | `200 OK` + `AssignmentDto` | `404` `ASSIGNMENT_NOT_FOUND` (hoặc `404` rỗng nếu `assignmentId` không phải số nguyên) |
| `POST` | `/api/assignments/{assignmentId:int}/submissions` | `201 Created` + header `Location` + `SubmissionDto` | `404` `ASSIGNMENT_NOT_FOUND`, `400` `ASSIGNMENT_CLOSED`, `400` `INVALID_SUBMISSION`, `415` nếu thiếu `Content-Type` |
| `GET` | `/api/submissions/{submissionId:guid}` | `200 OK` + `SubmissionDto` | `404` `SUBMISSION_NOT_FOUND` |

Mọi lỗi nghiệp vụ từ handler đều trả thống nhất theo `ApiError(string Code, string Message)`.

---

## 2. Các lệnh đã chạy và kết quả thực tế

### 2.1. Biên dịch solution

```powershell
cd D:\Code_demo\RestfulClientServer
dotnet build RestfulClientServer.slnx
```

Kết quả thực tế:
```text
  Submission.Client -> D:\Code_demo\RestfulClientServer\src\Submission.Client\bin\Debug\net10.0\Submission.Client.dll
  Submission.Api -> D:\Code_demo\RestfulClientServer\src\Submission.Api\bin\Debug\net10.0\Submission.Api.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2.2. Chạy API (Terminal 1) và Client (Terminal 2)

**Terminal 1 – Khởi chạy `Submission.Api`:**
```powershell
cd D:\Code_demo\RestfulClientServer
dotnet run --project src/Submission.Api --urls http://localhost:5000
```
*(Chờ thấy `Now listening on: http://localhost:5000`)*

**Terminal 2 – Chạy `Submission.Client`:**
```powershell
cd D:\Code_demo\RestfulClientServer
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Kết quả thực tế ở Terminal 2 (lần kiểm tra sau build cuối cùng):
```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client

Da tao luot nop: 4b59d4fb-78b9-407d-9096-c81acfa734e2
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

### 2.3. Các lệnh `curl.exe` đã kiểm thử

```powershell
curl.exe -i http://localhost:5000/
curl.exe -i http://localhost:5000/api/assignments
curl.exe -i http://localhost:5000/api/assignments/1
curl.exe -i http://localhost:5000/api/assignments/999
curl.exe -i http://localhost:5000/api/assignments/abc
curl.exe -i -X PUT http://localhost:5000/api/assignments/1
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@sv001.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@rong.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/999/submissions -H "Content-Type: application/json" -d "@sv001.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -d "@sv001.json"
curl.exe -i http://localhost:5000/api/submissions/00000000-0000-0000-0000-000000000000
```

---

## 3. Bảng phá thử và thí nghiệm kiểm tra

| Ca | Yêu cầu / Thí nghiệm | Kết quả mong đợi | Kết quả thực tế quan sát được | Bên trả lời & giải thích |
|---|---|---|---|---|
| `2` | `GET /` | `200 OK` + `{service, status}` | `HTTP/1.1 200 OK`, `{"service":"Submission.Api","status":"running"}` | Handler xác nhận API đang chạy ở cổng 5000. |
| `3` | `GET /api/assignments` | `200 OK`, 2 bài tập mẫu | `HTTP/1.1 200 OK`, mảng JSON 2 phần tử (`id: 1, 2`, `isOpen: true`) | Handler trả danh sách bài tập từ bộ nhớ. |
| `4a` | `GET /api/assignments/1` | `200 OK` + bài tập 1 | `HTTP/1.1 200 OK`, `{"id":1,"title":"Bai tap REST API",...}` | Handler tìm thấy phần tử theo route `assignmentId`. |
| `4b` | `GET /api/assignments/999` | `404 Not Found` + `ApiError` | `HTTP/1.1 404 Not Found`, `{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | Handler chạy, không tìm thấy ID 999 nên trả `ApiError`. |
| `4c` | `GET /api/assignments/abc` | `404 Not Found`, body rỗng | `HTTP/1.1 404 Not Found`, `Content-Length: 0` | Framework chặn trước handler vì ràng buộc tuyến `:int` không khớp `"abc"`. |
| `5a` | `POST /api/assignments/1/submissions` với `sv001.json` | `201 Created` + `Location` | `201 Created`, `Location: /api/submissions/5cc7f350-ae26-4c66-9007-7e903f47f2be` + JSON `SubmissionDto` | Handler kiểm qua 3 cổng, tự sinh `Guid`, gán `status = "received"`, `receivedAt` UTC. |
| `5b` | `POST /api/assignments/1/submissions` với `rong.json` | `400 INVALID_SUBMISSION` | `HTTP/1.1 400 Bad Request`, `{"code":"INVALID_SUBMISSION","message":"Thieu ma sinh vien hoac kho ma nguon."}` | Handler phát hiện `StudentCode`/`RepositoryUrl` rỗng ở cổng kiểm tra thứ 3. |
| `5c` | `POST /api/assignments/999/submissions` với `sv001.json` | `404 ASSIGNMENT_NOT_FOUND` | `HTTP/1.1 404 Not Found`, `{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | Handler kiểm cổng 1: tài nguyên cha `assignment 999` không tồn tại. |
| `5d` | `POST` hợp lệ nhưng thiếu `Content-Type` | `415 Unsupported Media Type` | `HTTP/1.1 415 Unsupported Media Type`, `Content-Length: 0` | Framework từ chối trước khi vào handler vì không biết body là JSON. |
| `5e` | `POST` gửi JSON hỏng `{"studentCode":"SV001",` | `400 Bad Request` (body text) | `400 Bad Request`, `BadHttpRequestException` (`JsonReaderException`) | Framework từ chối khi giải tuần tự JSON thất bại trước khi vào handler. |
| `5f` | `POST` thiếu trường `{"studentCode":"SV001"}` | `400 INVALID_SUBMISSION` | `400 Bad Request`, `{"code":"INVALID_SUBMISSION","message":"Thieu ma sinh vien hoac kho ma nguon."}` | Trường thiếu nhận giá trị `null`, handler bắt được qua `IsNullOrWhiteSpace`. |
| `5g` | `PUT /api/assignments/1` | `405 Method Not Allowed` | `HTTP/1.1 405 Method Not Allowed`, `Allow: GET` | Framework nhận diện URI tồn tại nhưng chỉ hỗ trợ `GET`. |
| `5h` | Thử tạm đặt bài tập 2 `IsOpen = false`, `POST /api/assignments/2/submissions` *(đã khôi phục `true`)* | `400 ASSIGNMENT_CLOSED` | `HTTP/1.1 400 Bad Request`, `{"code":"ASSIGNMENT_CLOSED","message":"Bai tap da dong nop bai."}` | Handler kiểm cổng 2: bài tập đã đóng không cho nộp. |
| `6a` | `GET` theo URL ở header `Location` của `5a` | `200 OK` + đúng lượt nộp | `HTTP/1.1 200 OK`, trả đúng JSON lượt nộp `5cc7f350-...` | Server tìm thấy lượt nộp trong `submissions`. |
| `6b` | `GET /api/submissions/00000000-0000-0000-0000-000000000000` | `404 SUBMISSION_NOT_FOUND` | `HTTP/1.1 404 Not Found`, `{"code":"SUBMISSION_NOT_FOUND","message":"Khong tim thay luot nop."}` | Handler chạy, không có GUID trong danh sách. |
| `6c` | Tắt API rồi bật lại, gọi `GET` với GUID `5cc7f350-...` | `404 SUBMISSION_NOT_FOUND` | `HTTP/1.1 404 Not Found`, `{"code":"SUBMISSION_NOT_FOUND","message":"Khong tim thay luot nop."}` | Dữ liệu chỉ lưu trong `List<SubmissionDto>` trên RAM tiến trình API nên mất khi API dừng. |
| `7a` | Tắt API rồi chạy `Submission.Client` | Lỗi kết nối mạng | `Unhandled exception. System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5000)` (`ExitCode: -532462766`) | Lỗi ở tầng kết nối TCP khi tiến trình máy chủ vắng mặt, chưa có phản hồi HTTP nào. |
| `7b` | Chạy `Submission.Client` với cổng sai `http://localhost:5999` | Lỗi kết nối tới cổng `5999` | `HttpRequestException: ... (localhost:5999)` | Hai tiến trình không thống nhất cùng URL/cổng dịch vụ. |
| `7c` | Thử tạm thêm bài tập 3 vào API rồi chạy lại Client *(đã khôi phục 2 bài tập)* | Client tự in 3 dòng không cần sửa code | Client in đủ 3 bài tập (`- 1: Bai tap REST API`, `- 2: Bai tap Client`, `- 3: Bai tap SignalR`) | Client chỉ phụ thuộc vào hợp đồng API (`GET /api/assignments` trả mảng `AssignmentDto`), không gắn cứng số lượng bài tập. |

---

## 4. Trả lời 3 câu hỏi giải thích (Slide 90)

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   - URI đóng vai trò định danh **tài nguyên** (các "danh từ" nghiệp vụ và quan hệ cha–con như `/api/assignments`, `/api/assignments/{assignmentId}/submissions`, `/api/submissions/{submissionId}`), giúp địa chỉ tài nguyên ổn định và đoán được.
   - Phương thức HTTP (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`) đã biểu đạt rõ **động từ/hành động** trên tài nguyên đó (cùng với ngữ nghĩa an toàn và lũy đẳng), tránh lặp động từ dư thừa như `/getAssignments` hay `/doSubmit`.

2. **“Stateless” thể hiện ở đâu trong code đã viết?**
   - Trong `Submission.Api`, server không dùng session hay ghi nhớ phiên hội thoại nào cho từng Client (ví dụ: không có biến ghi nhớ "client X đang chọn bài tập 1").
   - Mỗi yêu cầu tự mang đủ toàn bộ ngữ cảnh cần thiết để xử lý: `GET /api/assignments/{assignmentId}` mang `assignmentId` trong URL; `POST /api/assignments/{assignmentId}/submissions` mang `assignmentId` trên route và đủ `StudentCode`, `RepositoryUrl` trong JSON body; `GET /api/submissions/{submissionId}` mang trực tiếp GUID lượt nộp. *(Lưu ý: `List<SubmissionDto>` là dữ liệu nghiệp vụ của hệ thống, không phải trạng thái phiên của client.)*

3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   - Hai chương trình chạy ở hai tiến trình độc lập, giao tiếp bằng thông điệp HTTP chứa chuỗi JSON chứ không chia sẻ đối tượng C# trong bộ nhớ và không dùng Project Reference.
   - Hai bên thống nhất **hợp đồng dữ liệu JSON**: cùng tên trường (`Id`/`id`, `Title`/`title`, `StudentCode`/`studentCode`, `Code`/`code`, `Message`/`message`, ...), kiểu dữ liệu tương thích và quy tắc đặt tên camelCase mặc định của `System.Text.Json`. Nhờ vậy, miễn cấu trúc JSON khớp hợp đồng, Client giải tuần tự thành công sang bản sao `record` của riêng mình.

---

## 5. Ảnh minh chứng (người dùng tự chụp và lưu vào `docs/images/`)

- `![Build và cấu trúc dự án](docs/images/01-build-structure.png)`
- `![API chạy cổng 5000 và Client 4 bước](docs/images/02-api-client.png)`
- `![Kiểm thử requests.http trên VS Code](docs/images/03-requests-http.png)`
- `![Thí nghiệm phá thử và lỗi kết nối](docs/images/04-experiments.png)`

---

## 6. Đối chiếu phạm vi bắt buộc và mở rộng (`*`)

- **Đã hoàn thành (bắt buộc theo Slide 65–90 & Mã nguồn 2.3–2.4):**
  - Đủ 4 endpoint nghiệp vụ (`/api/assignments`, `/api/assignments/{assignmentId:int}`, `/api/assignments/{assignmentId:int}/submissions`, `/api/submissions/{submissionId:guid}`) cùng endpoint kiểm tra trạng thái `GET /`.
  - Đầy đủ DTO (`AssignmentDto`, `CreateSubmissionRequest`, `SubmissionDto`, `ApiError`) tách riêng ở API và Client, 3 cổng kiểm tra nghiệp vụ đúng thứ tự (`ASSIGNMENT_NOT_FOUND` -> `ASSIGNMENT_CLOSED` -> `INVALID_SUBMISSION`).
  - `Submission.Client` chạy chuỗi 4 bước, đọc `args[0]` (mặc định `http://localhost:5000`), kiểm tra `HttpStatusCode.Created`, có hàm dùng chung `PrintApiErrorAsync`.
  - `requests.http` (11 request, phủ đủ 7+ ca bắt buộc với `200`, `201`, `400`, `404`, cộng thêm `415`, `405`), `sv001.json`, `rong.json`, và `README.md`.
- **Phần mở rộng đánh dấu `*` / ngoài giáo trình (chưa gộp vào bản chính):**
  - *Bước 9 mở rộng (Slide 67 `*`, Slide 102–103):* `409 Conflict` (`DUPLICATE_SUBMISSION`) và `DELETE /api/submissions/{submissionId:guid}` (`204 No Content`), thêm bài tập 3 đóng. **Lý do chưa gộp vào code chính:** tài liệu đánh dấu rõ `* Mở rộng ở bước 9, ngoài giáo trình`, slide 90 yêu cầu bản nộp là `API 4 endpoint + client chuỗi 4 bước`, và nếu bật `409` ngay sẽ làm lần chạy thứ 2 của `Submission.Client` (gửi cùng `SV001`) dừng ở bước 2 thay vì hoàn tất 4 bước.
  - *Bài tập thực hành 3–4 (Giáo trình trang 43):* bổ sung gọi thêm `GET /api/assignments/{assignmentId}` vào Client trước bước POST. **Lý do chưa sửa Client chính:** giữ nguyên `Submission.Client` đúng 4 bước của Mã nguồn 2.4 và Slide 84–87.

