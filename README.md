# BÀI 2 – RESTful API Catalog

## 1. Thông tin sinh viên

- Họ và tên: Dương Văn Thịnh
- MSSV: DTC245200501
- Môn học: Lập trình phân tán

## 2. Giới thiệu

Dự án CatalogApi xây dựng RESTful API để quản lý danh sách sản phẩm theo mô hình Client - Server.

## 3. Công nghệ sử dụng

- C#
- ASP.NET Core Minimal API
- .NET 10
- HttpClient
- REST Client
- Git

## 4. Cấu trúc dự án

- Catalog.Api: Xử lý các yêu cầu API.
- Catalog.Client: Ứng dụng Client gọi API.
- requests.http: Kiểm thử các endpoint RESTful API.

## 5. Các API

| Method | Endpoint           | Mô tả                  |
| ------ | ------------------ | ---------------------- |
| GET    | /                  | Kiểm tra API           |
| GET    | /api/products      | Lấy danh sách sản phẩm |
| GET    | /api/products/{id} | Lấy chi tiết sản phẩm  |
| POST   | /api/products      | Thêm sản phẩm          |

## 6. Kết quả kiểm thử

| Trường hợp              | HTTP Status                |
| ----------------------- | -------------------------- |
| GET /                   | 200 OK                     |
| GET danh sách           | 200 OK                     |
| GET chi tiết            | 200 OK                     |
| ID không tồn tại        | 404 Not Found              |
| ID không hợp lệ         | 404 Not Found              |
| POST hợp lệ             | 201 Created                |
| POST giá âm             | 400 Bad Request            |
| POST thiếu Content-Type | 415 Unsupported Media Type |

## 7. Câu hỏi lý thuyết

### Câu 1: Vì sao URI nên dùng danh từ, còn hành động nằm trong HTTP method?

URI nên mô tả tài nguyên bằng danh từ. HTTP method thể hiện hành động như GET, POST, PUT và DELETE.

### Câu 2: Stateless được thể hiện ở đâu trong code?

Mỗi request từ Client gửi đến Server được xử lý độc lập. Server không cần lưu trạng thái của request trước để xử lý request tiếp theo.

### Câu 3: Vì sao Client và Server có thể khai báo DTO riêng?

Client và Server có thể khai báo DTO riêng miễn là cấu trúc dữ liệu JSON trao đổi giữa hai bên tương thích. Điều này giúp hai bên phát triển độc lập.

## 8. Ghi chú thực nghiệm Client

Dự án Catalog.Client đã build thành công. Tuy nhiên, khi chạy ứng dụng, Windows Application Control chặn thực thi file. Vì vậy, phần chạy Client chưa được xác nhận hoàn tất.

## 9. Kết luận

Dự án đã xây dựng và kiểm thử các chức năng RESTful API cơ bản, bao gồm GET danh sách, GET chi tiết và POST sản phẩm. Các trường hợp thành công và lỗi HTTP đã được kiểm tra bằng REST Client.
