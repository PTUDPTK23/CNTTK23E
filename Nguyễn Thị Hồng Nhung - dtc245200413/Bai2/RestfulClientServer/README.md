# Nhật ký Bài 2 - RESTful Client & Server

## Đáp án 3 câu hỏi lý thuyết

### 1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời**: 
  - URI biểu diễn **tài nguyên** (Resource), nên việc dùng danh từ (như `/api/assignments`, `/api/submissions`) giúp định danh rõ ràng đối tượng đang thao tác mà không bị rườm rà hay trùng lặp.
  - Các hành động tác động lên tài nguyên đã được chuẩn hóa bởi các phương thức HTTP (HTTP Verbs): `GET` (đọc/lấy), `POST` (tạo mới), `PUT/PATCH` (cập nhật), `DELETE` (xóa). 
  - Cách thiết kế này giúp API đạt tính RESTful, nhất quán và dễ mở rộng.

### 2. "Stateless" (Phi trạng thái) thể hiện ở đâu trong code đã viết?
- **Trả lời**: 
  - Thể hiện ở việc mỗi yêu cầu HTTP (Request) từ máy khách gửi lên máy chủ đều độc lập và phải chứa **đầy đủ thông tin cần thiết** để máy chủ hiểu và xử lý.
  - Máy chủ không lưu trữ bất kỳ trạng thái phiên (Session/Context) nào của máy khách giữa các lần gọi khác nhau. 
  - *Ví dụ trong code*: Khi gọi `POST /api/assignments/1/submissions`, máy khách phải truyền đầy đủ `StudentCode` và `RepositoryUrl` trong Body request; máy chủ chỉ đọc dữ liệu từ request đó để xử lý rồi trả về kết quả mà không cần nhớ lịch sử trước đó của client.

### 3. Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời**: 
  - Vì hai bên giao tiếp với nhau dựa trên **Hợp đồng dữ liệu JSON (JSON Contract)** chứ không phụ thuộc vào mã nguồn C# hay kiểu lớp (Class/Record) cụ thể của nhau.
  - Máy chủ biến đổi đối tượng C# thành chuỗi JSON (Serialize) trả về cho máy khách, và máy khách đọc chuỗi JSON đó chuyển ngược thành đối tượng C# của máy khách (Deserialize).
  - Miễn là tên trường (`studentCode`, `repositoryUrl`,...) và kiểu dữ liệu trong JSON tương thích (mặc định ASP.NET Core tự chuyển đổi thành camelCase), hai bên hoàn toàn độc lập về mặt khai báo DTO.