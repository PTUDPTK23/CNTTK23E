\# ĐỀ TÀI 08: HỆ THỐNG THEO DÕI BƯU GỬI



\## 1. Giới thiệu

Xây dựng hệ thống theo dõi bưu gửi, cho phép người dùng quản lý và tra cứu thông tin bưu gửi thông qua ứng dụng Client giao tiếp với Web API.



\## 2. Công nghệ sử dụng

\- Ngôn ngữ: C#

\- Nền tảng: .NET 10

\- ASP.NET Core Minimal API

\- HttpClient

\- Giao thức HTTP

\- Dữ liệu lưu trữ tạm thời trong bộ nhớ (In-memory)



\## 3. Chức năng đã thực hiện

\### API

\- GET /api/parcels: Xem danh sách bưu gửi.

\- GET /api/parcels/{code}: Tra cứu bưu gửi theo mã.

\- POST /api/parcels: Tạo bưu gửi mới.

\- PUT /api/parcels/{code}/status: Cập nhật trạng thái bưu gửi.

\- GET /api/parcels/{code}/history: Xem lịch sử vận chuyển.



\### Client

\- Xem danh sách bưu gửi.

\- Tra cứu bưu gửi theo mã.

\- Xem lịch sử vận chuyển.

\- Tạo bưu gửi mới.

\- Cập nhật trạng thái bưu gửi.



\## 4. Hướng dẫn chạy chương trình



\### Bước 1: Chạy API

Mở terminal tại thư mục Tuan1 và chạy:



```powershell

dotnet run --project src/Parcel.Api --urls http://localhost:5100

```



\### Bước 2: Chạy Client

Mở một terminal khác tại thư mục Tuan1 và chạy:



```powershell

dotnet run --project src/Parcel.Client -- http://localhost:5100

```



\## 5. Kiến thức đã áp dụng

\- Mô hình Client – Server.

\- Xây dựng Web API bằng Minimal API.

\- Gửi và nhận yêu cầu HTTP.

\- Sử dụng HttpClient để Client giao tiếp với API.

\- Làm việc với các phương thức GET, POST và PUT.

