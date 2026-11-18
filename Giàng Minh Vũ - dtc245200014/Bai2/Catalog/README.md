# Catalog

Phần này hướng dẫn chi tiết source Catalog API và Catalog Client của Bài 2 RESTful API.

## Thành phần

- `src/Catalog.Api`: ASP.NET Core Minimal API. Sản phẩm được lưu trong danh sách trong bộ nhớ, khởi tạo lại mỗi lần API chạy.
- `src/Catalog.Client`: ứng dụng console sử dụng `HttpClient` và JSON để gọi API, tạo sản phẩm hợp lệ và thử dữ liệu sai.
- `CatalogApi.slnx`: solution chứa cả hai project.
- `requests.http`: các request HTTP mẫu.
- `sp-hop-le.json`, `sp-sai.json`: payload tạo sản phẩm hợp lệ và không hợp lệ.

## Yêu cầu và chạy

Cài .NET 10 SDK. Từ thư mục gốc repository:

```powershell
dotnet restore Catalog/CatalogApi.slnx
dotnet build Catalog/CatalogApi.slnx
dotnet run --project Catalog/src/Catalog.Api/Catalog.Api.csproj --launch-profile http
```

API lắng nghe tại `http://localhost:5100`. Trong terminal khác:

```powershell
dotnet run --project Catalog/src/Catalog.Client/Catalog.Client.csproj
```

Có thể truyền URL API làm đối số cho client, ví dụ:

```powershell
dotnet run --project Catalog/src/Catalog.Client/Catalog.Client.csproj -- http://localhost:5100
```

## API

| Method | Route | Kết quả |
|---|---|---|
| GET | `/` | Thông tin trạng thái dịch vụ |
| GET | `/api/products` | Danh sách sản phẩm, `200 OK` |
| GET | `/api/products/{id}` | Chi tiết `200 OK`; ID không tồn tại trả `404 Not Found` |
| POST | `/api/products` | Tạo mới `201 Created`; dữ liệu sai trả `400 Bad Request` |

POST nhận JSON có `name`, `description`, `price`; tên phải có nội dung và giá phải lớn hơn 0. Request mẫu nằm trong `requests.http` và hai file JSON mẫu.

## Kiểm thử

Giữ API đang chạy, mở `Catalog/requests.http` trong REST Client (VS Code) hoặc HTTP Client tương thích, rồi gửi từng request. Có ví dụ cho 200, 201, 400, 404, 405 và 415. Dữ liệu nằm trong bộ nhớ và trở về bộ dữ liệu ban đầu khi khởi động lại API.
