# Báo cáo Bài tập Thực hành 2: RESTful API cho tương tác Client - Server

- **Họ và tên:** Vàng Thị Sinh
- **Mã sinh viên:** dtc241200007

---

## 1. Nhật ký lệnh đã thực hiện
- **Tạo Solution và các project:**
  ```powershell
  dotnet new sln -n RestfulClientServer
  dotnet new web -n Submission.Api -o src/Submission.Api
  dotnet new console -n Submission.Client -o src/Submission.Client
  ```
- **Thêm project vào solution, tạo .gitignore và build:**
  ```powershell
  dotnet sln add src/Submission.Api/Submission.Api.csproj
  dotnet sln add src/Submission.Client/Submission.Client.csproj
  dotnet new gitignore --force
  dotnet build
  ```
- **Chạy máy chủ (Server) tại cổng 5000:**
  ```powershell
  dotnet run --project src/Submission.Api --urls http://localhost:5000
  ```
- **Chạy máy khách (Client) kiểm tra chuỗi 4 bước:**
  ```powershell
  dotnet run --project src/Submission.Client http://localhost:5000
  ```

---

---

## 3. Bảng phá thử (7 ca kiểm thử trong requests.http)
| STT | Phương thức & URI | Tình huống kiểm thử | Mã trạng thái | Kết quả / Mã lỗi |
| :--- | :--- | :--- | :--- | :--- |
| 1 | `GET /api/assignments` | Lấy danh sách bài tập | **200 OK** | Trả về mảng bài tập |
| 2 | `GET /api/assignments/1` | Xem chi tiết bài tập hợp lệ | **200 OK** | Trả về thông tin bài tập 1 |
| 3 | `GET /api/assignments/999` | Bài tập không tồn tại | **404 Not Found** | `ASSIGNMENT_NOT_FOUND` |
| 4 | `GET /api/assignments/abc` | Sai kiểu dữ liệu `:int` trên route | **404 Not Found** | Lỗi do framework chặn |
| 5 | `POST /api/assignments/1/submissions` | Nộp bài hợp lệ | **201 Created** | Kèm Header `Location` và DTO |
| 6 | `GET /api/submissions/{id}` | Đọc lại lượt nộp vừa tạo | **200 OK** | Trả về đúng lượt nộp theo ID |
| 7 | `POST /api/assignments/1/submissions` | Nộp thiếu mã SV hoặc link repo | **400 Bad Request** | `INVALID_SUBMISSION` |

---

## 4. Trả lời ba câu hỏi ôn tập
1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   - **Trả lời:** URI dùng danh từ (ví dụ: `/api/assignments`, `/api/submissions`) để định danh rõ ràng tài nguyên mà hệ thống quản lý. Việc tách hành động sang phương thức HTTP (`GET`, `POST`, `PUT`, `DELETE`) giúp chuẩn hóa hợp đồng API theo kiến trúc REST, dễ đọc và dễ đoán cách sử dụng.
2. **“Stateless” thể hiện ở đâu trong code đã viết?**
   - **Trả lời:** Thể hiện ở việc máy chủ (`Submission.Api`) không lưu giữ trạng thái phiên làm việc (session) của client giữa các lần gọi. Mọi yêu cầu HTTP gửi lên đều phải tự mang đầy đủ thông tin cần thiết (tham số trên URI hoặc JSON trong body) để server xử lý độc lập.
3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   - **Trả lời:** Client và server là hai tiến trình độc lập, không dùng chung mã nguồn hay vùng nhớ mà chỉ giao tiếp qua chuẩn thông điệp JSON trên mạng. Miễn là tên các thuộc tính tương thích (chuẩn *camelCase* khi tuần tự hóa JSON), dữ liệu truyền qua lại sẽ được ánh xạ chính xác.