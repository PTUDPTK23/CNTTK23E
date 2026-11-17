# Catalog API — Bài 2

Sinh viên: **Lê Hải Anh — DTC245200851 — CNTT K23E**.

Hoàn thiện phần Catalog (bước 1–9 trong PDF). Hai project dùng .NET 10, giao tiếp bằng HTTP và JSON. Giữ tên thư mục `CataLog` hiện có của dự án.

## Chạy bài

Mở terminal tại thư mục `CataLogApi` rồi build:

```powershell
dotnet build
```

Terminal 1 — chạy API:

```powershell
dotnet run --project .\src\CataLog.Api --no-launch-profile --urls http://localhost:5100
```

Terminal 2 — chạy client:

```powershell
dotnet run --project .\src\CataLog.Client -- http://localhost:5100
```

Client in đủ 4 bước: danh sách → tạo sản phẩm (201 và Location) → chi tiết sản phẩm 1 → gửi sai và đọc INVALID_PRODUCT (400). Client kiểm tra mã HTTP trước khi đọc dữ liệu và báo lỗi kết nối khi API tắt hoặc sai cổng.

Nhấn Ctrl+C tại terminal API trước khi sửa và chạy lại, tránh khóa file exe hoặc trùng cổng. Dữ liệu lưu trong bộ nhớ; khởi động lại API sẽ trở về 3 sản phẩm mẫu.

## Hợp đồng API

| Phương thức | URI | Kết quả |
|---|---|---|
| GET | / | 200, trạng thái dịch vụ |
| GET | /api/products | 200, danh sách sản phẩm |
| GET | /api/products/{id:int} | 200, hoặc 404 + PRODUCT_NOT_FOUND |
| POST | /api/products | 201 + Location, hoặc 400 + INVALID_PRODUCT |

DTO đầu vào: Name, Description (tùy chọn), Price. Server sinh Id; tên không được trống và giá phải lớn hơn 0. Tên và mô tả được Trim. DTO đầu ra thêm Id. Lỗi nghiệp vụ dùng ApiError(Code, Message).

## Gửi request

Cài extension REST Client trong VS Code, mở `requests.http` và bấm Send Request. Có 8 ca theo bảng kiểm Catalog, gồm 404 có body và 404 do route không khớp. Ca 415 dùng Content-Type không hỗ trợ; curl khi bỏ -H và dùng -d cũng mặc định gửi application/x-www-form-urlencoded.

Hoặc chạy trong terminal 2:

```powershell
curl.exe -i http://localhost:5100/api/products
curl.exe -i http://localhost:5100/api/products/1
curl.exe -i http://localhost:5100/api/products/999
curl.exe -i http://localhost:5100/api/products/abc
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-hop-le.json"
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-sai.json"
curl.exe -i -X POST http://localhost:5100/api/products -d "@sp-hop-le.json"
```

Sau khi đã thêm POST, gửi POST không còn trả 405 như ở bước 4 trước khi triển khai bước 5.

## Kết quả kiểm tra thực tế

| Kiểm tra | Kết quả |
|---|---|
| Build solution gồm API và Client | Thành công, 0 cảnh báo, 0 lỗi |
| GET / | 200, status running |
| GET danh sách ngay sau khởi động | 200, 3 sản phẩm |
| GET sản phẩm 1 | 200, id = 1 |
| GET sản phẩm 999 | 404, PRODUCT_NOT_FOUND |
| GET sản phẩm abc | 404, body rỗng |
| POST hợp lệ | 201, Location /api/products/4; GET Location trả đúng sản phẩm |
| POST giá âm hoặc tên trắng | 400, INVALID_PRODUCT; không thêm dữ liệu |
| POST thiếu header JSON | 415 |
| Client chạy đủ 4 bước | Thành công; đọc được ApiError |
| Client chạy lần 2, cùng API | Danh sách có 4 sản phẩm, tạo id 5 |
| Tắt API rồi chạy Client | Báo lỗi kết nối, mã thoát 1 |
| Khởi động lại API | Trở về 3 sản phẩm mẫu |
| Client dùng cổng 5999 | Báo lỗi kết nối, mã thoát 1 |

Client có xử lý lỗi kết nối nên in thông báo thay vì unhandled exception như ví dụ PDF.

## Giải thích

1. URI dùng danh từ vì xác định tài nguyên; GET và POST xác định thao tác đọc và tạo.
2. Stateless: mỗi request mang đủ URI, phương thức và body cần thiết; server không ghi nhớ bước trước của từng client. Danh sách sản phẩm là dữ liệu nghiệp vụ, không phải phiên hội thoại.
3. Client và server khai báo DTO riêng nhưng thống nhất tên thuộc tính và kiểu dữ liệu JSON, nên vẫn trao đổi được. Đổi Name thành Ten ở client sẽ không khớp trường name từ server.
4. /999 khớp route số nguyên nên handler trả ApiError; /abc không khớp ràng buộc int nên framework trả 404 rỗng.

## Bước 10 — lịch sử và nộp bài

Mã nguồn và kết quả kiểm tra đã sẵn sàng. Chưa tạo commit hoặc push. Không thể tạo lại lịch sử 7 bước đã làm trước đó một cách trung thực; hãy kiểm tra Git, bổ sung ảnh chạy thực tế và nộp theo yêu cầu giảng viên. Chưa triển khai các bài mở rộng ở phụ lục hoặc phần EduSubmit.
