# EduSubmit – Hệ thống nộp bài phân tán (Bài 1)

**Sinh viên:** Phan Quang Tùng  
**MSSV:** DTC245200250  
**Lớp:** CNTT K23E  
**Học phần:** Phát triển ứng dụng phân tán  

---

## Cấu trúc dự án

```text
EduSubmit-DTC245200250/
├── EduSubmit.slnx              # Solution chứa 2 dự án
├── requests.http               # File kịch bản kiểm thử REST Client
├── src/
│   ├── Submission.Api/         # Web API máy chủ (ASP.NET Core Minimal API)
│   │   ├── Program.cs
│   │   └── Submission.Api.csproj
│   └── Submission.Client/      # Client máy khách console
│       ├── Program.cs
│       └── Submission.Client.csproj
└── docs/
    ├── bai1.md                 # Báo cáo nhật ký thực hành theo mẫu Phụ lục 3
    └── images/                 # Ảnh chụp minh chứng các bước
```

---

## Hướng dẫn chạy

### 1. Build toàn bộ dự án
```bash
dotnet build EduSubmit.slnx
```

### 2. Chạy máy chủ (API)
Mở Terminal 1:
```bash
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

### 3. Chạy máy khách (Client)
Mở Terminal 2 song song:
```bash
dotnet run --project src/Submission.Client -- http://localhost:5000
```

### 4. Kiểm thử API qua `requests.http`
Mở file `requests.http` trong VS Code và bấm **Send Request** trên từng khối request (hỗ trợ cả các mã trạng thái `200`, `201`, `400`, `404`).
