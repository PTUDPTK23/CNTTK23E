# EduSubmit – Hệ Thống Tiếp Nhận Bài Nộp Phân Tán

Dự án bài tập thực hành môn **Phát triển ứng dụng phân tán** – Bài 1.

## 1. Thông tin sinh viên

- **Họ và tên:** Phan Thế Vinh
- **MSSV:** DTC245200309
- **Lớp:** CNTT K23E
- **Học phần:** Phát triển ứng dụng phân tán
- **Mã bài tập / Tag nộp:** `bai1`

## 2. Cấu trúc dự án

```
EduSubmit-DTC245200309/
├── .gitignore                      # File cấu hình bỏ qua file build (.NET)
├── EduSubmit.slnx                  # Solution chứa các project
├── requests.http                   # Bộ kiểm thử các API endpoint qua REST Client
├── README.md                       # Giới thiệu dự án và hướng dẫn chạy
├── docs/                           # Tài liệu báo cáo thực hành
│   ├── bai1.md                     # Báo cáo chi tiết Bài 1 theo mẫu giảng viên
│   └── images/                     # Thư mục lưu trữ ảnh chụp màn hình minh chứng
│       ├── 01-environment.png
│       ├── 02-mepdemo.png
│       └── ...
└── src/
    ├── Submission.Api/             # Web API (Máy chủ ASP.NET Core Minimal API)
    │   ├── Program.cs
    │   ├── Submission.Api.csproj
    │   └── Properties/
    │       └── launchSettings.json
    └── Submission.Client/          # Console Client (Máy khách C# sử dụng HttpClient)
        ├── Program.cs
        └── Submission.Client.csproj
```

## 3. Kiến trúc và Giao tiếp

Hệ thống được thiết kế theo mô hình phân tán gồm 2 tiến trình độc lập trao đổi thông điệp qua mạng:
- **`Submission.Api` (Máy chủ):** Lắng nghe tại `http://localhost:5000`, cung cấp các endpoint:
  - `GET /` : Kiểm tra trạng thái máy chủ.
  - `GET /api/assignments` : Cung cấp danh sách các bài tập mẫu.
  - `POST /api/submissions` : Tiếp nhận bài nộp, kiểm tra dữ liệu, sinh mã GUID và lưu trữ trong bộ nhớ (Thread-safe).
  - `GET /api/submissions/{id:guid}` : Tra cứu thông tin bài nộp theo mã GUID (Bài mở rộng Bước 12).
- **`Submission.Client` (Máy khách):** Nhận URL máy chủ từ tham số dòng lệnh (mặc định `http://localhost:5000`), gọi lấy danh sách bài tập, gửi yêu cầu nộp bài, và tra cứu lại chi tiết bài nộp vừa tạo.

Hai project không chia sẻ thư viện nhị phân (không sử dụng Project Reference), chỉ thống nhất thông qua hợp đồng dữ liệu JSON qua giao thức HTTP.

## 4. Hướng dẫn biên dịch và chạy ứng dụng

### 4.1. Biên dịch toàn bộ solution
```powershell
dotnet build EduSubmit.slnx
```

### 4.2. Khởi chạy máy chủ API (Terminal 1)
```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```
*(Chờ dòng thông báo: `Now listening on: http://localhost:5000`)*

### 4.3. Khởi chạy máy khách Client (Terminal 2)
```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

## 5. Kiểm thử các endpoint qua file `requests.http`

Mở file `requests.http` trong VS Code (đã cài extension REST Client) và bấm **Send Request** trên từng khối yêu cầu để kiểm tra các kịch bản:
1. `GET /` -> 200 OK
2. `GET /api/assignments` -> 200 OK
3. `POST /api/submissions` (hợp lệ) -> 201 Created
4. `POST /api/submissions` (không hợp lệ) -> 400 Bad Request
5. `GET /api/submissions/{id}` (tồn tại) -> 200 OK
6. `GET /api/submissions/{id}` (không tồn tại) -> 404 Not Found

## 6. Báo cáo chi tiết

Báo cáo đầy đủ về 5 mô hình MEP, 4 thí nghiệm lỗi và các câu trả lời kỹ thuật nằm tại:
`docs/bai1.md`
