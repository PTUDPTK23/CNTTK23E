# Đề tài 16 - Hệ thống chat hỗ trợ trực tuyến

- `frontend`: giao diện Blazor Web App viết bằng C# và Razor.
- `backend`: ASP.NET Core Web API dùng PostgreSQL.

## Khởi động

PostgreSQL phải đang chạy và backend cần được cấu hình trong `backend/appsettings.Development.local.json`. Mở hai terminal:

1. Trong `backend`, chạy `dotnet run --launch-profile http` (mặc định `http://localhost:5131`).
2. Trong `frontend`, chạy `dotnet run --launch-profile http` (mặc định `http://localhost:5251`).

Đăng nhập bằng tài khoản Admin đã cấp. Sau khi đăng nhập, vào `/admin` để quản lý khách hàng, nhân viên, hội thoại, lịch sử tin nhắn, hiệu suất và cấu hình hệ thống.
