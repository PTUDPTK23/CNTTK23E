# Hệ thống quản lý hóa đơn

Project gồm frontend HTML/CSS/JavaScript, backend ASP.NET Core API (.NET 10) và PostgreSQL.

## Cổng chạy thống nhất

| Thành phần | Địa chỉ |
| --- | --- |
| Frontend | `http://localhost:5500` |
| Backend API | `http://localhost:5000` |
| API readiness (kiểm tra cả PostgreSQL) | `http://localhost:5000/health` |
| PostgreSQL | `localhost:5432` |

Mở frontend bằng `localhost` hoặc `127.0.0.1`; frontend tự gọi API trên cùng hostname để cookie đăng nhập khớp. Không dùng `0.0.0.0` làm địa chỉ trên trình duyệt; đó chỉ là địa chỉ bind của server.

## Chạy từ PowerShell

Đảm bảo dịch vụ PostgreSQL cài trên máy đang chạy và thông tin kết nối trong `backend/InvoiceManagementSystem.Api/appsettings.json` đúng với PostgreSQL local. Cấu hình mặc định dùng `localhost:5432`, database `invoicemanagementsystem`, user `postgres`.

Mở ba cửa sổ PowerShell riêng. Nếu chưa biết tên dịch vụ PostgreSQL:

```powershell
Get-Service *postgres*
```

Nếu dịch vụ đang dừng, khởi động bằng tên dịch vụ tìm được (cài đặt PostgreSQL 16 thường dùng `postgresql-x64-16`):

```powershell
Start-Service postgresql-x64-16
```

Sau đó chạy backend và frontend:

```powershell
cd E:\InvoiceManagementSystem\backend\InvoiceManagementSystem.Api
dotnet run
```

```powershell
cd E:\InvoiceManagementSystem\frontend
python -m http.server 5500 --bind 127.0.0.1
```

Truy cập `http://localhost:5500/` hoặc `http://127.0.0.1:5500/` (trang quản trị thêm `/admin/`). Giữ hai cửa sổ terminal mở trong khi sử dụng.

## Chạy từ VS Code

Mở thư mục gốc `E:\InvoiceManagementSystem`, chọn **Terminal → Run Task…**, rồi chạy lần lượt:

1. `Backend API (localhost:5000)`
2. `Frontend (localhost:5500)`

Hai task chạy trong terminal riêng, dùng cùng cổng và hostname như lệnh PowerShell ở trên.

Xem [backend/README.md](backend/README.md) để biết API, tài khoản quản trị và cấu hình PostgreSQL.
