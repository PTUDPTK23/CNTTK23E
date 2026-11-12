# Catalog API

Solution .NET 10 minh họa RESTful API va console client theo Bài 2.

## Chạy

```powershell
dotnet run --project src/Catalog.Api
```

API nghe tại `http://localhost:5100`. Mở terminal khác để chạy client:

```powershell
dotnet run --project src/Catalog.Client
```

## Hợp đồng API

| Phương thức | URI | Kết quả |
| --- | --- | --- |
| `GET` | `/` | `200` trạng thái máy chủ |
| `GET` | `/api/products` | `200` danh sách sản phẩm |
| `GET` | `/api/products?q=REST` | `200` lọc tên hoặc mô tả theo từ khóa |
| `GET` | `/api/products/{id:int}` | `200` sản phẩm hoặc `404 PRODUCT_NOT_FOUND` |
| `POST` | `/api/products` | `201 Created` + `Location`; dữ liệu không hợp lệ trả `400 INVALID_PRODUCT` |

`POST` nhận JSON có `name`, `description` và `price`; tên không được để trống và giá phải lớn hơn 0. Dữ liệu sản phẩm nằm trong bộ nhớ, vì vậy sẽ mất khi dừng API.

Ví dụ tạo sản phẩm:

```http
POST http://localhost:5100/api/products
Content-Type: application/json

{
  "name": "Sach REST",
  "description": "Huong dan REST API",
  "price": 80000
}
```
