# IoT Monitoring System

## Backend PostgreSQL

Backend ASP.NET Core dùng Npgsql để kết nối PostgreSQL. Khi khởi động, ứng dụng tạo bảng `app_users` và chỉ mục cần thiết nếu chưa có, sau đó tạo hoặc cập nhật tài khoản quản trị viên duy nhất `Cha123` / `Cha123456`.

1. Mở `backend/IotMonitoring.Api/appsettings.json` và thay `YOUR_PASSWORD` bằng mật khẩu của PostgreSQL:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=iot_monitoring;Username=postgres;Password=YOUR_PASSWORD"
     }
   }
   ```

2. Tạo database `iot_monitoring` trong pgAdmin hoặc bằng lệnh `createdb -U postgres -h localhost iot_monitoring` nếu database chưa tồn tại.

3. Trong PowerShell tại thư mục dự án, chạy:

   ```powershell
   .\run-backend.bat
   ```

   Backend sẽ kết nối PostgreSQL, tạo bảng tài khoản và chỉ mục nếu chưa có, rồi phục vụ giao diện.

   Mở `http://localhost:5000`. Backend sẽ phục vụ luôn frontend. Để chạy frontend riêng trong VS Code bằng lệnh `py -m http.server 5500 --directory frontend`, backend đã bật CORS cho cổng này.

Không đưa mật khẩu thật lên kho mã nguồn. Backend tự tạo bảng tài khoản khi khởi động.
