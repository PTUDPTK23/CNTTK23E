# Bài 2 - RESTful API cho tương tác client-server

- Họ tên: Đặng Quốc Trung
- MSSV: dtc245200016
- GitHub: `trungdtt123-cmd`
- Công nghệ: .NET 10, ASP.NET Core Minimal API, C#, HttpClient
- Ngày kiểm thử gần nhất: 11/11/2026

## 1. Mục tiêu

Bài thực hành xây dựng hai hệ thống client-server nhỏ, chạy thành các tiến trình riêng và chỉ trao đổi qua HTTP/JSON:

- Catalog API: quản lý danh sách sản phẩm, đọc chi tiết và tạo sản phẩm.
- EduSubmit: đọc bài tập, tạo lượt nộp, đọc lại lượt nộp, chống nộp trùng và rút lượt nộp.
- Máy khách kiểm tra mã trạng thái trước khi đọc body và dùng `ApiError` cho các lỗi nghiệp vụ.
- Thí nghiệm các đặc điểm của hệ phân tán: sai địa chỉ, server ngừng hoạt động và dữ liệu RAM mất sau khi khởi động lại.

## 2. Cấu trúc dự án

```text
Bai2/
├── .gitignore
├── README.md
├── CataLogApi/
│   └── CatalogApi/
│       ├── CatalogApi.slnx
│       ├── requests.http
│       ├── sp-hop-le.json
│       ├── sp-sai.json
│       └── src/
│           ├── Catalog.Api/
│           └── Catalog.Client/
└── RestfulClientServer/
    ├── RestfulClientServer.slnx
    ├── requests.http
    └── src/
        ├── Submission.Api/
        └── Submission.Client/
```

Hai solution đều chứa đủ project API và Client. `bin/`, `obj/`, `.vs/`, tệp log và thư mục tạm được loại khỏi Git bằng `.gitignore`.

## 3. Yêu cầu môi trường

```powershell
dotnet --version
```

Kết quả đã kiểm tra: `.NET SDK 10.0.102`.

Trong VS Code nên cài:

- C# Dev Kit
- REST Client

## 4. Build và chạy Catalog

```powershell
cd CataLogApi\CatalogApi
dotnet restore CatalogApi.slnx
dotnet build CatalogApi.slnx
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```

Mở terminal thứ hai:

```powershell
cd CataLogApi\CatalogApi
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

### Hợp đồng Catalog API

| Phương thức | URI | Thành công | Lỗi |
|---|---|---|---|
| GET | `/` | 200 + trạng thái dịch vụ | - |
| GET | `/api/products` | 200 + danh sách | - |
| GET | `/api/products/{id:int}` | 200 + sản phẩm | 404 `PRODUCT_NOT_FOUND`; 404 rỗng nếu ID không phải số |
| POST | `/api/products` | 201 + `Location` + sản phẩm | 400 `INVALID_PRODUCT`; 415 nếu sai/thiếu `Content-Type` |

## 5. Build và chạy EduSubmit

```powershell
cd RestfulClientServer
dotnet restore RestfulClientServer.slnx
dotnet build RestfulClientServer.slnx
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Mở terminal thứ hai:

```powershell
cd RestfulClientServer
dotnet run --project src/Submission.Client -- http://localhost:5000
```

### Hợp đồng Submission API

| Phương thức | URI | Thành công | Lỗi |
|---|---|---|---|
| GET | `/` | 200 + trạng thái dịch vụ | - |
| GET | `/api/assignments` | 200 + danh sách bài tập | - |
| GET | `/api/assignments/{assignmentId:int}` | 200 + bài tập | 404 `ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{assignmentId:int}/submissions` | 201 + `Location` + lượt nộp | 400 `ASSIGNMENT_CLOSED`; 400 `INVALID_SUBMISSION`; 404 `ASSIGNMENT_NOT_FOUND`; 409 `DUPLICATE_SUBMISSION`; 415 nếu sai/thiếu `Content-Type` |
| GET | `/api/submissions/{submissionId:guid}` | 200 + lượt nộp | 404 `SUBMISSION_NOT_FOUND` |
| DELETE | `/api/submissions/{submissionId:guid}` | 204, không body | 404 `SUBMISSION_NOT_FOUND` |

Thứ tự kiểm tra POST là: bài tập tồn tại → bài tập còn mở → body hợp lệ → không trùng sinh viên/bài tập → tạo lượt nộp.

## 6. Chạy bộ request

Mở một trong hai tệp `requests.http`, bảo đảm API tương ứng đang chạy, rồi bấm **Send Request** theo thứ tự từ trên xuống.

- Catalog có 8 ca chính, bao phủ 200, 201, 400, 404, 405 và 415.
- EduSubmit có 16 ca, bao phủ 200, 201, 204, 400, 404, 405, 409 và 415.
- Ca EduSubmit số 9 phải chạy trước các ca 10-13 vì những ca sau lấy `id` từ phản hồi `createSubmission`.

## 7. Kết quả kiểm thử thực tế

### Build

| Solution | Kết quả |
|---|---|
| `CatalogApi.slnx` | PASS - 0 warning, 0 error |
| `RestfulClientServer.slnx` | PASS - 0 warning, 0 error |

### Catalog API và Client

| Ca kiểm thử | Kết quả thực tế | Trạng thái |
|---|---|---|
| GET `/` | 200, JSON `Catalog.Api/running` | PASS |
| GET `/api/products` | 200, 3 sản phẩm sau khi server khởi động | PASS |
| GET `/api/products/1` | 200, đúng sản phẩm | PASS |
| GET `/api/products/999` | 404 + `PRODUCT_NOT_FOUND` | PASS |
| GET `/api/products/abc` | 404, body rỗng | PASS |
| PUT `/api/products` | 405 | PASS |
| POST hợp lệ | 201, `Location: /api/products/4` | PASS |
| POST tên rỗng/giá âm | 400 + `INVALID_PRODUCT` | PASS |
| POST thiếu `Content-Type` | 415 | PASS |
| POST JSON hỏng | 400 | PASS |
| Catalog.Client | In đủ danh sách → tạo → đọc chi tiết → lỗi 400 | PASS |
| Client gọi cổng 5999 | In lỗi mạng có kiểm soát, không còn unhandled exception | PASS |
| Khởi động lại API | Danh sách trở về 3 sản phẩm | PASS |

### Submission API và Client

| Ca kiểm thử | Kết quả thực tế | Trạng thái |
|---|---|---|
| GET `/api/assignments` | 200, 3 bài tập | PASS |
| GET bài tập 1 | 200 | PASS |
| GET bài tập 999 | 404 + `ASSIGNMENT_NOT_FOUND` | PASS |
| GET ID `abc` | 404, body rỗng | PASS |
| PUT sai phương thức | 405 | PASS |
| POST thiếu `Content-Type` | 415 | PASS |
| POST JSON hỏng | 400 | PASS |
| POST bài tập 3 đã đóng | 400 + `ASSIGNMENT_CLOSED` | PASS |
| POST body rỗng/thiếu trường | 400 + `INVALID_SUBMISSION` | PASS |
| POST vào bài tập 999 | 404 + `ASSIGNMENT_NOT_FOUND` | PASS |
| POST hợp lệ | 201 + `Location` + `status: received` | PASS |
| GET theo `Location` | 200, đúng lượt nộp | PASS |
| POST trùng, không phân biệt hoa/thường mã SV | 409 + `DUPLICATE_SUBMISSION` | PASS |
| DELETE lần đầu | 204, không body | PASS |
| DELETE lần hai | 404 + `SUBMISSION_NOT_FOUND` | PASS |
| POST lại sau khi rút | 201, ID mới | PASS |
| Submission.Client lần đầu | In đủ xem → nộp → đọc lại → lỗi 400 | PASS |
| Submission.Client lần hai | In `DUPLICATE_SUBMISSION`, không sập | PASS |
| Client gọi cổng 5999 | In lỗi mạng có kiểm soát | PASS |
| Khởi động lại API, GET ID cũ | 404 + `SUBMISSION_NOT_FOUND` | PASS |

## 8. Bảng đối chiếu yêu cầu

| # | Yêu cầu | Kết quả | Tệp/chứng cứ |
|---:|---|---|---|
| 1 | Đọc và đối chiếu PDF 105 trang | Đạt | Đã đối chiếu Phần 2, Phần 3, trang 90, Bước 9 |
| 2 | Catalog solution có API và Client | Đạt | `CataLogApi/CatalogApi/CatalogApi.slnx` |
| 3 | Catalog đủ 4 endpoint | Đạt | `CataLogApi/CatalogApi/src/Catalog.Api/Program.cs` |
| 4 | Catalog.Client đủ chuỗi 4 bước | Đạt | `CataLogApi/CatalogApi/src/Catalog.Client/Program.cs` |
| 5 | Catalog `requests.http` đủ 8 ca | Đạt | `CataLogApi/CatalogApi/requests.http` |
| 6 | EduSubmit có API và Client | Đạt | `RestfulClientServer/RestfulClientServer.slnx` |
| 7 | EduSubmit đủ 4 endpoint cơ bản | Đạt | `RestfulClientServer/src/Submission.Api/Program.cs` |
| 8 | DTO đầu vào/đầu ra và `ApiError` riêng | Đạt | Hai tệp `Program.cs` của EduSubmit |
| 9 | Kiểm tra tồn tại → mở → hợp lệ | Đạt | Handler POST của Submission.Api |
| 10 | Trùng lượt nộp trả 409 | Đạt | `DUPLICATE_SUBMISSION` |
| 11 | DELETE trả 204; xóa lại trả 404 | Đạt | Endpoint DELETE |
| 12 | `Location` đọc lại được | Đạt | POST + GET submission |
| 13 | EduSubmit `requests.http` đủ ca bắt buộc/mở rộng | Đạt | `RestfulClientServer/requests.http` |
| 14 | Client xử lý lỗi HTTP | Đạt | `PrintApiErrorAsync` |
| 15 | Cả hai Client phân biệt lỗi mạng | Đạt | `HttpRequestException`/`TaskCanceledException` |
| 16 | Build cả hai solution | Đạt | 0 warning, 0 error |
| 17 | Test HTTP thực tế | Đạt | Bảng kết quả mục 7 |
| 18 | Kiểm tra dữ liệu RAM sau restart | Đạt | Catalog về 3 sản phẩm; ID submission cũ trả 404 |
| 19 | README có lệnh, endpoint, kiểm thử, câu hỏi | Đạt | Tệp này |
| 20 | Không đưa `bin/`, `obj/`, tệp tạm vào Git | Đạt | `.gitignore` |
| 21 | Kiểm tra `git status` và `git log` | Đạt | Nhánh `bai2-dtc245200016`, working tree sạch trước khi rà soát |
| 22 | Ảnh minh chứng | Chưa thực hiện | Cần chụp thủ công theo mục 11 |
| 23 | Fork/push/PR repo lớp | Chưa hoàn tất | Commit ban đầu đã push; các commit rà soát còn local và PR chưa mở |

Tổng hợp: **21 đạt, 0 chưa đạt, 2 chưa hoàn tất** (ảnh minh chứng và push/PR cuối cùng).

## 9. Trả lời ba câu hỏi trang 90

### 1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?

URI định danh tài nguyên, ví dụ `/api/assignments/1` là bài tập số 1 và `/api/products` là tập sản phẩm. Hành động đã được chuẩn hóa bởi phương thức HTTP: GET để đọc, POST để tạo, DELETE để xóa. Cách tách này giúp URI ổn định, dễ đoán, tránh các đường dẫn kiểu gọi hàm như `/getProducts` hoặc `/doSubmit`, đồng thời cho phép framework, proxy và client hiểu đúng ngữ nghĩa của yêu cầu.

### 2. “Stateless” thể hiện ở đâu trong code đã viết?

Mỗi request mang đủ thông tin để xử lý độc lập: ID bài tập nằm trong URI, mã sinh viên và URL kho mã nguồn nằm trong body, ID lượt nộp nằm trong URI khi đọc/xóa. Server không cần nhớ client trước đó đã “chọn” bài tập nào. Danh sách `assignments` và `submissions` là dữ liệu nghiệp vụ trong RAM, không phải trạng thái hội thoại; vì vậy việc server lưu dữ liệu không mâu thuẫn với stateless.

### 3. Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?

Hai tiến trình không chia sẻ lớp C# hay bộ nhớ. Chúng khớp nhau nhờ hợp đồng JSON: tên thuộc tính, kiểu dữ liệu và ý nghĩa trường phải tương thích. Bộ tuần tự của ASP.NET Core và `System.Net.Http.Json` đổi DTO của mỗi bên sang/cùng từ JSON theo cùng hợp đồng. Trình biên dịch không kiểm tra hợp đồng giữa hai project, nên nếu đổi tên hoặc kiểu trường ở một bên mà không cập nhật bên kia thì vẫn có thể build nhưng dữ liệu đọc ra sai; các test HTTP giúp phát hiện sai lệch đó.

## 10. Ghi chú thí nghiệm phân tán

- Client và server là hai tiến trình; client không đọc trực tiếp được `List` của server.
- Sai cổng hoặc server tắt tạo lỗi mạng và không có phản hồi HTTP.
- Lỗi 4xx nghĩa là server đã nhận yêu cầu và gửi phản hồi; lỗi mạng nghĩa là không có phản hồi.
- Dữ liệu hiện lưu trong `List`, nên khởi động lại API làm mất sản phẩm/lượt nộp tạo trong phiên trước.
- Nếu chạy hai bản API cùng lúc ở hai cổng, mỗi tiến trình có một vùng RAM riêng và có thể trả dữ liệu khác nhau.
- POST không lũy đẳng; Bước 9 dùng quy tắc 409 để ngăn cùng sinh viên nộp trùng một bài tập.

## 11. Ảnh minh chứng cần chụp thủ công

Không tạo ảnh giả. Nên chụp các ảnh sau sau khi chạy đúng lệnh:

1. `dotnet --version` và build hai solution đều `Build succeeded`, 0 warning, 0 error.
2. Hai terminal Catalog: API lắng nghe cổng 5100 và Client in đủ 4 nấc.
3. `requests.http` Catalog: phản hồi 201 có `Location`, 400 có `INVALID_PRODUCT`, 404 có `PRODUCT_NOT_FOUND`, 415 khi thiếu `Content-Type`.
4. Hai terminal EduSubmit: API lắng nghe cổng 5000 và Client lần đầu in đủ 4 bước.
5. Submission.Client chạy lần hai hiển thị `DUPLICATE_SUBMISSION` 409 nhưng không sập.
6. Chuỗi Bước 9 trong `requests.http`: 201 → 409 → 204 → 404 → 201.
7. Ca bài tập đóng trả 400 `ASSIGNMENT_CLOSED` và bài tập 999 trả 404 `ASSIGNMENT_NOT_FOUND`.
8. Client gọi cổng 5999 và hiển thị lỗi kết nối.
9. GET một submission cũ sau khi khởi động lại API trả 404 `SUBMISSION_NOT_FOUND`.
10. Cây thư mục nộp bài và `git status` không chứa `bin/`, `obj/`, `tmp/`.

## 12. Trạng thái Git và các bước GitHub còn lại

Bài nằm đúng cấu trúc trong fork:

```text
Đặng Quốc Trung - dtc245200016/
└── Bai2/
    ├── README.md
    ├── .gitignore
    ├── CataLogApi/
    └── RestfulClientServer/
```

Trước đợt rà soát cuối, nhánh `bai2-dtc245200016` có một commit Bài 2 đã push. Các commit sửa lỗi và cập nhật tài liệu được giữ ở local để kiểm tra trước khi nộp. Chạy:

```powershell
git status
git log --oneline origin/main..HEAD
git diff origin/bai2-dtc245200016..HEAD
git push origin bai2-dtc245200016
```

Kiểm tra lần cuối rằng Git không theo dõi `bin/`, `obj/`, `tmp/`, khóa API hoặc mật khẩu. Mở Pull Request vào repo lớp với tiêu đề:

```text
Bai2 - dtc245200016 - Đặng Quốc Trung
```
