# Frontend C#

Giao diện Blazor Web App viết bằng C# và Razor Components. CSS nằm trong `wwwroot/app.css`.

Giao diện Admin được mở sau khi đăng nhập thành công, tại `/admin`. Frontend gọi backend ở `http://localhost:5131` theo cấu hình `Backend:BaseUrl` trong `appsettings.json`.

Chạy bằng `dotnet run --launch-profile http`; địa chỉ mặc định là http://localhost:5251.
