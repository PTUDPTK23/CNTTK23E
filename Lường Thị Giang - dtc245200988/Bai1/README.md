# Bài 1 - Tổng quan hệ thống và MEP

## Thông tin sinh viên
- Họ tên: Lương Thị Giang
- MSSV: DTC245200988
- Lớp: CNTT K23E
- Học phần: Phát triển ứng dụng phân tán

## Giới thiệu
EduSubmit là hệ thống phân tán gồm hai thành phần:
- Submission.Api: cung cấp REST API.
- Submission.Client: gửi HTTP request và nhận response từ API.

## Hướng dẫn chạy

Yêu cầu: .NET 10 SDK.

### Chạy API
dotnet run --project src/Submission.Api --urls http://localhost:5000

### Chạy Client
dotnet run --project src/Submission.Client -- http://localhost:5000

## Kiểm thử
Xem file src/requests.http để kiểm thử các endpoint.

Các mã HTTP cần kiểm tra:
- 200 OK
- 201 Created
- 400 Bad Request
- 404 Not Found

## Nhật ký và minh chứng
Xem docs/bai1.md và thư mục docs/images.

## Thí nghiệm lỗi và câu hỏi
Xem phần giải thích và kết quả thực tế trong docs/bai1.md.
