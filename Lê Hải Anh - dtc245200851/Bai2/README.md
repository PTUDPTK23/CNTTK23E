# Bài 2 — RESTful API

**Lê Hải Anh — dtc245200851 — CNTT K23E**

- [Catalog: mã nguồn, lệnh chạy và kết quả kiểm tra](CataLogApi/README.md) — API cổng 5100, client 4 bước.
- [EduSubmit: mã nguồn, lệnh chạy và kết quả kiểm tra](RestfulClientServer/README.md) — API cổng 5000, 4 endpoint nghiệp vụ và client 4 bước.

Hai solution dùng .NET 10; mở terminal trong từng thư mục rồi chạy `dotnet build`. Mỗi hệ thống chạy API và client ở hai terminal. Các tệp requests.http dùng extension REST Client.

Đã build và kiểm tra các ca HTTP, client và lỗi kết nối; chi tiết trong README từng phần. Dữ liệu lưu trong RAM nên mất khi khởi động lại. Chưa bổ sung ảnh minh chứng; chưa triển khai phần mở rộng tùy chọn 409/204. Code có hỗ trợ từ Codex.
