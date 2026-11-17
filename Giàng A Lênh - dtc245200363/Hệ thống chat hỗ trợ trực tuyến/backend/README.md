# Backend C# và PostgreSQL

ASP.NET Core Web API dùng Entity Framework Core với PostgreSQL.

## Kết nối database

Thông tin phát triển cục bộ được đặt trong `appsettings.Development.local.json` (đã thêm vào `.gitignore` để không đưa mật khẩu vào Git). Sao chép `appsettings.Development.local.json.example`, điền chuỗi kết nối của bạn rồi chạy backend.

Khi khởi động, backend tạo các bảng còn thiếu trong database đã cấu hình và thêm tài khoản Admin ban đầu nếu bảng `admin_users` chưa có tài khoản.

## API quản trị

- `POST /api/auth/login` và `POST /api/auth/logout`
- `GET /api/admin/summary`
- CRUD khách hàng: `/api/admin/customers`
- CRUD nhân viên: `/api/admin/staff`
- Đọc hội thoại, tin nhắn và hiệu suất: `/api/admin/conversations`, `/api/admin/messages`, `/api/admin/performance`
- Đọc/cập nhật cấu hình: `/api/admin/settings`

Các endpoint `/api/admin/*` cần bearer token do API đăng nhập cấp.
