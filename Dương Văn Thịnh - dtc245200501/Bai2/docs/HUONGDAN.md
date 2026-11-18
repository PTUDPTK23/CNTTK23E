# HƯỚNG DẪN CHẠY VÀ KIỂM THỬ CATALOG API

## 1. Yêu cầu

- Đã cài đặt .NET SDK.
- Đã cài đặt Visual Studio Code.
- Đã mở thư mục CatalogApi.

## 2. Build dự án

Mở Terminal tại thư mục gốc và chạy:

```powershell
dotnet build
```

## 3. Chạy API

```powershell
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```

## 4. Kiểm thử API

Sử dụng REST Client trong Visual Studio Code và file requests.http.

Các chức năng kiểm thử:

- GET danh sách sản phẩm.
- GET chi tiết sản phẩm.
- POST thêm sản phẩm.
- Kiểm tra ID không tồn tại.
- Kiểm tra giá không hợp lệ.
- Kiểm tra tên sản phẩm.
- Kiểm tra Content-Type.

## 5. Kết quả mong đợi

- GET thành công: HTTP 200.
- POST thành công: HTTP 201.
- Dữ liệu không hợp lệ: HTTP 400.
- Không tìm thấy sản phẩm: HTTP 404.
- Thiếu Content-Type: HTTP 415.
