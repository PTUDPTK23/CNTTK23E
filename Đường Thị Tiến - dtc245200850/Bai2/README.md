# Báo cáo thực hành Bài 2: RESTful API

- Sinh viên: Đường Thị Tiến
- Mã sinh viên: dtc245200850
- Lớp: CNTT K23E

---

## 1. Hướng dẫn chạy chương trình

### 1.1. Dự án CatalogApi (Cổng 5100)
- **Chạy Server:**
  ```powershell
  dotnet run --project CatalogApi/src/Catalog.Api --urls http://localhost:5100
  ```
- **Chạy Client:**
  ```powershell
  dotnet run --project CatalogApi/src/Catalog.Client -- http://localhost:5100
  ```
- Thử nghiệm các ca kiểm thử bằng file `CatalogApi/requests.http`.

### 1.2. Dự án EduSubmit (Cổng 5000)
- **Chạy Server:**
  ```powershell
  dotnet run --project EduSubmit/src/Submission.Api --urls http://localhost:5000
  ```
- **Chạy Client:**
  ```powershell
  dotnet run --project EduSubmit/src/Submission.Client -- http://localhost:5000
  ```
- Thử nghiệm các ca kiểm thử bằng file `EduSubmit/requests.http`.

---

## 2. Trả lời câu hỏi lý thuyết (Slide 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời:**
  - Thiết kế RESTful coi mọi thực thể trong hệ thống là tài nguyên (Resource). Do đó URI chỉ dùng để đặt tên, định danh tài nguyên (danh từ, ví dụ `/api/products`, `/api/assignments/1/submissions`).
  - Các thao tác CRUD đã có sẵn các phương thức chuẩn của giao thức HTTP đảm nhận: `GET` (đọc), `POST` (tạo), `PUT` (thay thế), `PATCH` (sửa một phần), `DELETE` (xóa).
  - Cách làm này giúp URI ngắn gọn, nhất quán, tránh việc sinh ra hàng loạt URI tùy tiện chứa động từ (`/getProducts`, `/createProduct`, `/doSubmit`), đồng thời tận dụng được các cơ chế chuẩn của Web như caching và proxy.

### Câu 2: “Stateless” thể hiện ở đâu trong code đã viết?
- **Trả lời:**
  - Tính chất phi trạng thái (Stateless) thể hiện ở việc máy chủ không lưu bất kỳ thông tin phiên (session/conversation) nào của máy khách giữa hai lần gọi.
  - Trong code:
    - Ở máy chủ (`Catalog.Api` và `Submission.Api`), mỗi yêu cầu gửi đến đều tự chứa đầy đủ ngữ cảnh để xử lý: ID bài tập nằm trên route `{assignmentId}`, dữ liệu mã SV và URL nằm trong body DTO. Server xử lý xong là kết thúc, không lưu biến session hay cookie của client.
    - Ở máy khách (`Catalog.Client` và `Submission.Client`), mỗi lần gọi qua `HttpClient` đều đóng gói đầy đủ tham số và DTO cần thiết. Hai lần gọi liên tiếp không phụ thuộc vào trạng thái trung gian nào được lưu trên RAM của server.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời:**
  - Trong hệ phân tán, client và server chạy ở hai tiến trình hoàn toàn riêng biệt (thậm chí có thể khác máy, khác ngôn ngữ lập trình), không chung bộ nhớ và không dùng chung file mã nguồn.
  - Cầu nối duy nhất giữa hai bên là thông điệp định dạng **JSON** truyền qua giao thức HTTP.
  - Khi server trả kết quả, đối tượng DTO phía server được tuần tự hóa (serialize) thành chuỗi JSON. Phía client nhận chuỗi JSON và tự giải tuần tự hóa (deserialize) vào DTO của riêng mình. Chỉ cần hai bên thống nhất đúng "hợp đồng" (tên trường và kiểu dữ liệu JSON khớp nhau, ví dụ `name`, `price`, `studentCode`), thì code hai bên tự định nghĩa DTO độc lập vẫn ghép nối chính xác.
