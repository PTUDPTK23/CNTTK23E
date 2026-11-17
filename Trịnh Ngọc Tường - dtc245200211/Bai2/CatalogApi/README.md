# Bài 2: RESTful API cho tương tác Client–Server (Catalog API)

## Cấu trúc dự án

- `CatalogApi/CatalogApi.slnx`: Solution liên kết 2 project:
  - `src/Catalog.Api`: ASP.NET Core Minimal API lắng nghe tại `http://localhost:5100`
  - `src/Catalog.Client`: Console Client dùng `HttpClient`
- `requests.http`: Tập hợp 8 request kiểm thử cho REST Client (`humao.rest-client`)
- `sp-hop-le.json`, `sp-sai.json`: Dữ liệu mẫu dùng kiểm thử `POST /api/products` bằng `curl.exe`
- `RestfulClientServer/`: Phần 3 — Áp dụng sang hệ thống **EduSubmit** (`Submission.Api` cổng `5000` & `Submission.Client`)

## Lệnh chạy nhanh

Terminal 1 (máy chủ):
```powershell
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```

Terminal 2 (máy khách):
```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

## Kết quả Bước 9: Bốn thí nghiệm phân tán

| # | Thí nghiệm | Kết quả quan sát được | Bài học |
|---|---|---|---|
| 1 | Chạy `Catalog.Client` lần 2 (máy chủ vẫn chạy) | Danh sách in ra 4 sản phẩm (có `[4] Nhap mon REST API`), tạo mới ra `[5]`, `Location: /api/products/5` | Trạng thái (`products`) nằm ở máy chủ, không ở máy khách |
| 2 | Tắt máy chủ, chạy `Catalog.Client` | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5100)` | Lỗi chỉ có ở hệ phân tán: tiến trình bên kia có thể vắng mặt |
| 3 | Chạy lại máy chủ rồi `GET /api/products` | Danh sách quay về 3 sản phẩm mặc định ban đầu | Dữ liệu lưu trong RAM bị mất khi tiến trình máy chủ dừng |
| 4 | Chạy `Catalog.Client` với cổng sai (`http://localhost:5999`) | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5999)` | Hai bên phải thống nhất đúng địa chỉ (URL/cổng) |

## Bước 10: Phiếu tự kiểm tra Catalog API

| # | Yêu cầu | Mong đợi | Tự đánh giá |
|---|---|---|---|
| 1 | `GET /` | `200` (`{"service":"Catalog API","status":"Running"}`) | ✓ Đạt |
| 2 | `GET /api/products` | `200`, 3 phần tử | ✓ Đạt |
| 3 | `GET /api/products/1` | `200` | ✓ Đạt |
| 4 | `GET /api/products/999` | `404` + `ApiError` (`PRODUCT_NOT_FOUND`) | ✓ Đạt |
| 5 | `GET /api/products/abc` | `404` rỗng (`Content-Length: 0`) | ✓ Đạt |
| 6 | `POST` hợp lệ | `201` + `Location: /api/products/4` | ✓ Đạt |
| 7 | `POST` giá âm | `400` + `ApiError` (`INVALID_PRODUCT`) | ✓ Đạt |
| 8 | `POST` thiếu `Content-Type` | `415 Unsupported Media Type` | ✓ Đạt |
| 9 | Máy khách chạy | Đủ 4 nấc (liệt kê → tạo mới → chi tiết → gửi lỗi 400) | ✓ Đạt |
| 10 | Tắt máy chủ, chạy máy khách | Lỗi kết nối (`HttpRequestException`) | ✓ Đạt |

## Phần mở rộng — Vận dụng (Slide 104)

1. **Lọc theo từ khóa (`GET /api/products?keyword=REST`)**:
   - Đã thêm tham số query `string? keyword` vào `GET /api/products`, lọc bằng `Where` + `Contains(..., StringComparison.OrdinalIgnoreCase)`.
   - Kết quả chạy thực tế: `200 OK`, trả về mảng JSON chứa 1 sản phẩm khớp (`[{"id":2,"name":"Tai lieu RESTful API",...}]`). Khi không truyền `keyword`, vẫn trả về đầy đủ danh sách mặc định.

2. **Xóa sản phẩm (`DELETE /api/products/{id:int}`)**:
   - Đã thêm `app.MapDelete("/api/products/{id:int}", ...)`.
   - Khi tồn tại (ví dụ `DELETE /api/products/4` sau khi tạo): trả `204 No Content` (body rỗng).
   - Khi không tồn tại (`DELETE /api/products/999`): trả `404 Not Found` kèm `ApiError` (`{"code":"PRODUCT_NOT_FOUND","message":"Khong tim thay san pham."}`).

3. **Giới hạn độ dài tên (`NAME_TOO_LONG`)**:
   - Trong `POST /api/products`, kiểm tra `request.Name.Trim().Length > 100` trả về `400 Bad Request` kèm `ApiError` (`NAME_TOO_LONG`).
   - **Đặt sau `INVALID_PRODUCT`**: Vì `INVALID_PRODUCT` kiểm tra `string.IsNullOrWhiteSpace(request.Name)` trước; nếu đặt kiểm tra độ dài lên trước, trường hợp `Name` là `null` sẽ gây `NullReferenceException` (hoặc chuỗi chỉ gồm > 100 khoảng trắng bị báo nhầm là tên quá dài thay vì tên rỗng).
   - Kết quả chạy thực tế với tên 101 ký tự: `400 Bad Request`, `{"code":"NAME_TOO_LONG","message":"Ten san pham khong duoc vuot qua 100 ky tu."}`.

4. **So sánh hai kiểu 404 (`/api/products/abc` vs `/api/products/999`)**:

   - **Gọi `/api/products/999`** (do **Handler** trả):
     ```http
     HTTP/1.1 404 Not Found
     Content-Type: application/json; charset=utf-8
     Date: Wed, 11 Nov 2026 13:11:03 GMT
     Server: Kestrel
     Transfer-Encoding: chunked

     {"code":"PRODUCT_NOT_FOUND","message":"Khong tim thay san pham."}
     ```
   - **Gọi `/api/products/abc`** (do **Framework** trả):
     ```http
     HTTP/1.1 404 Not Found
     Content-Length: 0
     Date: Wed, 11 Nov 2026 13:11:03 GMT
     Server: Kestrel
     ```
   - **Giải thích vì sao khác**: Tuyến đường khai báo ràng buộc kiểu `{id:int}` nên `"abc"` không phải số nguyên -> router của ASP.NET Core loại bỏ tuyến ngay từ tầng framework, hàm xử lý không chạy, trả `404` với `Content-Length: 0` (body rỗng). Với `999`, tuyến khớp nên handler chạy, tìm trong `products` trả về `null`, nên code tự gọi `Results.NotFound(new ApiError(...))` kèm `Content-Type: application/json; charset=utf-8` và body JSON có `code` + `message`.

5. **Đổi miền (Slide 104 #5 & Slide 105)**: Đã dựng tại thư mục [`LibraryApi/`](LibraryApi/).
