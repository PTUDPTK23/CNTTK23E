# Bài 2 – EduSubmit RESTful API

**Sinh viên:** Hà Đức Minh  
**Mã sinh viên:** DTC245200002  
**Học phần:** Phát triển ứng dụng phân tán

## 1. Mục tiêu

Xây dựng một lát cắt nhỏ của hệ thống EduSubmit theo mô hình máy khách–máy chủ:

- `Submission.Api`: ASP.NET Core Minimal API, cung cấp 4 endpoint REST.
- `Submission.Client`: ứng dụng Console sử dụng `HttpClient` để gọi API theo chuỗi 4 nấc.
- `requests.http`: 7 yêu cầu HTTP để kiểm thử các trường hợp thành công và lỗi.

Dữ liệu được lưu trong `List` ở bộ nhớ để tập trung vào giao tiếp HTTP. Vì vậy, dữ liệu lượt nộp sẽ mất khi tắt hoặc khởi động lại API. Đây là giới hạn có chủ ý của bài thực hành, chưa phải cơ sở dữ liệu dùng trong môi trường thật.

## 2. Cấu trúc thư mục

```text
Bai2/
├── Submission.Api/
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs
│   ├── appsettings.json
│   └── Submission.Api.csproj
├── Submission.Client/
│   ├── Program.cs
│   └── Submission.Client.csproj
├── requests.http
├── README.md
└── .gitignore
```

## 3. Yêu cầu môi trường

- .NET SDK 10.0.
- Visual Studio Code hoặc Visual Studio.
- VS Code extension **REST Client** (để gửi từng request trong `requests.http`).
- Hai cửa sổ Terminal: một chạy API, một chạy Client.

Kiểm tra SDK:

```powershell
dotnet --version
```

## 4. Cách biên dịch và chạy

Mở Terminal tại thư mục `Bai2`.

### 4.1. Biên dịch hai project

```powershell
dotnet build .\Submission.Api\Submission.Api.csproj
dotnet build .\Submission.Client\Submission.Client.csproj
```

Kết quả mong đợi: cả hai project build thành công, không có lỗi.

### 4.2. Chạy máy chủ API

Tại Terminal 1:

```powershell
dotnet run --project .\Submission.Api --urls http://localhost:5000
```

Giữ Terminal 1 hoạt động. Khi thấy dòng `Now listening on: http://localhost:5000`, API đã sẵn sàng.

Kiểm tra nhanh ở Terminal 2:

```powershell
curl.exe -i http://localhost:5000/
```

Kết quả mong đợi: `HTTP/1.1 200 OK` và JSON có `service` là `Submission.Api`, `status` là `running`.

### 4.3. Chạy máy khách

Tại Terminal 2, vẫn ở thư mục `Bai2`:

```powershell
dotnet run --project .\Submission.Client -- http://localhost:5000
```

Client thực hiện tuần tự 4 nấc:

1. GET `/api/assignments`, đọc và in danh sách bài tập.
2. POST tạo lượt nộp cho bài tập đang mở, kiểm tra `201 Created`, đọc `SubmissionDto` và header `Location`.
3. GET theo `Location` để đọc lại lượt nộp vừa tạo.
4. POST dữ liệu thiếu để kiểm tra `400 Bad Request` và đọc `ApiError`.

Client dùng `GetAsync`/`PostAsJsonAsync`, kiểm tra mã trạng thái trước khi đọc body. Lỗi kết nối được bắt riêng bằng `HttpRequestException`, không nhầm với lỗi HTTP 4xx.

## 5. Bốn endpoint của API

| STT | Phương thức | URI | Thành công | Trường hợp lỗi chính |
|---:|---|---|---|---|
| 1 | GET | `/api/assignments` | 200 – danh sách bài tập | — |
| 2 | GET | `/api/assignments/{assignmentId}` | 200 – chi tiết bài tập | 404 `ASSIGNMENT_NOT_FOUND` |
| 3 | POST | `/api/assignments/{assignmentId}/submissions` | 201 + `Location` + `SubmissionDto` | 404 bài không tồn tại; 400 bài đã đóng hoặc dữ liệu không hợp lệ |
| 4 | GET | `/api/submissions/{submissionId}` | 200 – lượt nộp | 404 `SUBMISSION_NOT_FOUND` |

### DTO và quy tắc xử lý

- `AssignmentDto`: dữ liệu bài tập trả về cho Client.
- `CreateSubmissionRequest`: DTO đầu vào, chỉ có `StudentCode` và `RepositoryUrl`.
- `SubmissionDto`: DTO đầu ra; `Id`, `AssignmentId`, `Status`, `ReceivedAt` do máy chủ tạo/kiểm soát.
- `ApiError`: định dạng lỗi thống nhất gồm `Code` và `Message` đối với lỗi do endpoint xử lý.

POST kiểm tra theo thứ tự: bài tập tồn tại → bài tập còn mở → dữ liệu đầu vào hợp lệ → tạo lượt nộp. Khi thành công, API trả `201 Created` và `Location: /api/submissions/{id}`.

Bài tập 1 đang mở để tạo lượt nộp thành công. Bài tập 2 được đặt trạng thái đóng để kiểm thử `400 ASSIGNMENT_CLOSED` (tình huống 5h trong slide).

## 6. Kiểm thử bằng requests.http (7 request)

Mở thư mục `Bai2` bằng VS Code, mở file `requests.http`, cài extension REST Client và chọn **Send Request** phía trên từng request. API phải đang chạy ở cổng 5000.

| Request | Nội dung | Kết quả mong đợi |
|---:|---|---|
| 1 | GET danh sách bài tập | 200 OK |
| 2 | GET chi tiết bài tập 1 | 200 OK |
| 3 | GET bài tập 999 không tồn tại | 404 `ASSIGNMENT_NOT_FOUND` |
| 4 | POST nộp bài tập 1 với dữ liệu hợp lệ | 201 Created, có `Location` |
| 5 | GET lượt nộp vừa tạo bằng ID từ response request 4 | 200 OK |
| 6 | POST thiếu mã sinh viên và URL kho mã nguồn | 400 `INVALID_SUBMISSION` |
| 7 | POST vào bài tập 2 đã đóng | 400 `ASSIGNMENT_CLOSED` |

**Lưu ý:** Gửi request 4 trước request 5 vì request 5 lấy `id` từ response của request 4. Mỗi lần khởi động lại API, danh sách lượt nộp trong bộ nhớ được tạo lại.

## 7. Bảng phá thử / kiểm tra lỗi

Các tình huống dưới đây dùng để quan sát sự khác nhau giữa lỗi do handler chủ động trả về và lỗi do framework xử lý.

| Tình huống | Cách thử | Kết quả dự kiến | Bên trả phản hồi |
|---|---|---|---|
| Bài tập không tồn tại | GET `/api/assignments/999` | 404 + `ASSIGNMENT_NOT_FOUND` | Handler |
| URL có chuỗi thay vì số | GET `/api/assignments/abc` | 404, thường không có `ApiError` | Framework (route constraint `:int`) |
| Thiếu Content-Type | Gửi POST JSON nhưng bỏ `Content-Type: application/json` | 415 hoặc phản hồi lỗi binding tùy cách gửi | Framework |
| JSON sai cú pháp | Gửi body JSON không hợp lệ | 400, body do framework tạo | Framework |
| Thiếu trường đầu vào | POST `{"studentCode":""}` hoặc body rỗng trường | 400 + `INVALID_SUBMISSION` | Handler |
| Bài tập đã đóng | POST vào bài tập 2 | 400 + `ASSIGNMENT_CLOSED` | Handler |
| Sai HTTP method | PUT `/api/assignments/1` | 405 Method Not Allowed | Framework |
| Tắt API rồi chạy Client | Dừng Terminal 1, chạy Client | Lỗi kết nối, không có mã HTTP | Mạng/Client |
| Dùng sai cổng | Chạy Client với `http://localhost:5999` | Lỗi kết nối | Mạng/Client |
| ID lượt nộp không tồn tại | GET `/api/submissions/00000000-0000-0000-0000-000000000000` | 404 + `SUBMISSION_NOT_FOUND` | Handler |

Mã 404 có thể đến từ hai nơi: endpoint tìm không thấy tài nguyên sẽ trả `ApiError`; còn URL không khớp route (ví dụ `/abc` với ràng buộc `:int`) là phản hồi của framework và không nhất thiết có body `ApiError`.

## 8. Ảnh kết quả chạy (tự chụp và bổ sung)

Sau khi chạy thử trên máy, chụp ảnh màn hình thật và chèn vào các vị trí dưới đây. Không dùng ảnh minh họa thay cho kết quả chạy thực tế.

- **Hình 1 – Cấu trúc project:** chụp Explorer của VS Code, thấy `Submission.Api`, `Submission.Client`, `requests.http`, `README.md`.
- **Hình 2 – API hoạt động:** chụp Terminal 1 có dòng `Now listening on: http://localhost:5000` và kết quả gọi `/` trả 200.
- **Hình 3 – Client chạy đủ 4 nấc:** chụp Terminal 2 thể hiện danh sách bài tập, tạo lượt nộp 201, đọc lại lượt nộp và lỗi 400.
- **Hình 4 – requests.http:** chụp kết quả các request 1–7, thể hiện các mã 200, 201, 400, 404.
- **Hình 5 – Kiểm thử lỗi:** chụp một trường hợp lỗi do handler có `ApiError` và một trường hợp do framework trả về.

## 9. Ba câu hỏi giải thích theo yêu cầu

### Câu 1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?

URI định danh tài nguyên mà Client muốn thao tác, ví dụ `/api/assignments` là tập bài tập và `/api/submissions/{id}` là một lượt nộp cụ thể. Phương thức HTTP thể hiện hành động trên tài nguyên: GET để đọc, POST để tạo. Cách thiết kế này tạo giao diện thống nhất, dễ hiểu và tránh đưa động từ vào URI như `/getAssignments` hoặc `/createSubmission`.

### Câu 2. “Stateless” thể hiện ở đâu trong code đã viết?

Mỗi request chứa đủ thông tin để API xử lý, ví dụ `assignmentId` nằm trên URI và dữ liệu nộp nằm trong JSON body. API không lưu trạng thái phiên đăng nhập hay phụ thuộc vào request trước để hiểu request hiện tại. Tuy nhiên, Stateless không có nghĩa máy chủ tuyệt đối không lưu dữ liệu: danh sách bài tập và lượt nộp vẫn nằm trong bộ nhớ của API. Vì dùng bộ nhớ, dữ liệu lượt nộp mất khi tiến trình API dừng.

### Câu 3. Vì sao Client và Server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?

Client và Server là hai project độc lập, không cần dùng chung một lớp C# ở thời điểm biên dịch. Hai bên giao tiếp qua hợp đồng JSON: tên trường, kiểu dữ liệu và cấu trúc phải tương thích. Server dùng DTO riêng để kiểm soát dữ liệu đầu vào/đầu ra; Client khai báo DTO tương ứng để serialize và deserialize JSON. Nhờ đó hai bên ít phụ thuộc mã nguồn của nhau nhưng vẫn trao đổi dữ liệu đúng hợp đồng.

## 10. Giới hạn của phiên bản thực hành

- Dữ liệu lưu trong bộ nhớ, chưa dùng database.
- Chưa có xác thực, phân quyền hoặc chấm điểm bài nộp.
- Mỗi lần khởi động API, dữ liệu lượt nộp trở về trạng thái ban đầu.
- Đây là phạm vi nhỏ theo bài thực hành để tập trung vào REST, DTO, mã trạng thái HTTP và `HttpClient`.

