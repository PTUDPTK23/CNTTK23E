# Bài 2 - RESTful API - EduSubmit

**Sinh viên:** Bế Thị Thu Uyên

**Mã sinh viên:** dtc245200078

## 1. Giới thiệu

EduSubmit là chương trình RESTful API hỗ trợ quản lý bài tập và tiếp nhận bài nộp của sinh viên.

## 2. Công nghệ sử dụng

- C#
- ASP.NET Core Minimal API
- .NET
- HttpClient
- JSON

## 3. Chức năng chính

- Lấy danh sách bài tập.
- Xem thông tin chi tiết bài tập.
- Kiểm tra trạng thái mở hoặc đóng của bài tập.
- Tiếp nhận bài nộp của sinh viên.
- Tra cứu thông tin bài nộp.
- Kiểm tra dữ liệu đầu vào và xử lý lỗi.

## 4. Các API

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/api/assignments` | Lấy danh sách bài tập |
| GET | `/api/assignments/{assignmentId}` | Xem chi tiết bài tập |
| POST | `/api/assignments/{assignmentId}/submissions` | Nộp bài |
| GET | `/api/submissions/{submissionId}` | Xem thông tin bài nộp |

## 5. Cách chạy chương trình

### Chạy API

```bash
dotnet run --project ./src/Catalog.Api --urls http://localhost:5100
```

### Chạy Client

Mở Terminal thứ hai:

```bash
dotnet run --project ./src/Catalog.Client -- http://localhost:5100
```

## 6. Kiểm thử API

Sử dụng file `requests.http` để kiểm thử các trường hợp:

1. Lấy danh sách bài tập.
2. Xem chi tiết bài tập.
3. Kiểm tra bài tập không tồn tại.
4. Nộp bài thành công.
5. Đọc lại thông tin bài nộp.
6. Kiểm tra dữ liệu không hợp lệ.
7. Kiểm tra nộp bài tập đã đóng.

## 7. Kết quả

Chương trình đã được kiểm tra bằng REST Client và chương trình Catalog.Client.

Các trường hợp thành công và xử lý lỗi được kiểm tra với các mã HTTP tương ứng như 200, 201, 400 và 404.