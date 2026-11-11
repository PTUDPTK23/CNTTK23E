# BÁO CÁO BÀI TẬP THỰC HÀNH 2: RESTful API CHO TƯƠNG TÁC CLIENT - SERVER

**Học phần:** Phát triển ứng dụng phân tán  
**Hệ thống:** EduSubmit (Hệ thống nộp bài trực tuyến qua RESTful API)  
**Tiêu đề nộp:** `Bai2 - MSSV - Họ tên`

---

## 1. Hợp đồng API (API Contract)

| Phương thức HTTP | URI Endpoint | Thành công | Lỗi có thể gặp | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/` | `200 OK` | - | Kiểm tra máy chủ có đang hoạt động |
| **GET** | `/api/assignments` | `200 OK` | - | Lấy danh sách các bài tập |
| **GET** | `/api/assignments/{assignmentId:int}` | `200 OK` | `404 ASSIGNMENT_NOT_FOUND`<br>`404 rỗng (nếu id sai kiểu)` | Chi tiết bài tập theo ID |
| **POST** | `/api/assignments/{assignmentId:int}/submissions` | `201 Created` + `Location` | `404 ASSIGNMENT_NOT_FOUND`<br>`400 ASSIGNMENT_CLOSED`<br>`400 INVALID_SUBMISSION`<br>`409 DUPLICATE_SUBMISSION`<br>`415 Unsupported Media Type` | Nộp bài cho bài tập (tài nguyên con) |
| **GET** | `/api/submissions/{submissionId:guid}` | `200 OK` | `404 SUBMISSION_NOT_FOUND` | Đọc lại thông tin lượt nộp theo Guid |
| **DELETE** | `/api/submissions/{submissionId:guid}` | `204 No Content` | `404 SUBMISSION_NOT_FOUND` | Hủy / rút lượt nộp bản nháp |

---

## 2. Trả lời 3 câu hỏi giải thích bắt buộc (Slide 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Bản chất của REST (Representational State Transfer):** REST là phong cách kiến trúc lấy **tài nguyên (Resource)** làm trung tâm. URI sinh ra là để **định danh duy nhất một tài nguyên** (tài nguyên luôn là một thực thể/danh từ, ví dụ `/api/assignments`, `/api/submissions`).
- **Tách biệt định danh và thao tác:** Phương thức HTTP (HTTP Verbs: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`) đã được định nghĩa theo chuẩn giao thức mạng toàn cầu để chỉ rõ hành động muốn thực hiện lên tài nguyên đó (`GET` = đọc, `POST` = tạo, `PUT` = thay thế toàn bộ, `DELETE` = xóa).
- **Tránh trùng lặp và phân mảnh:** Nếu đưa động từ vào URI (như `/getAssignments`, `/doSubmit`, `/deleteSubmission`), API sẽ bị biến dạng thành kiểu RPC (gọi hàm từ xa), khiến số lượng URI bùng nổ, không chuẩn hóa được bộ nhớ cache, và client không thể suy diễn được hành vi chuẩn của HTTP.

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Khái niệm Stateless:** Mỗi yêu cầu HTTP từ máy khách gửi lên máy chủ phải **chứa đầy đủ toàn bộ thông tin cần thiết** để máy chủ hiểu và xử lý, máy chủ không lưu giữ bất kỳ trạng thái phiên làm việc (Session/Conversation state) nào giữa hai yêu cầu liên tiếp.
- **Thể hiện cụ thể trong code:**
  - Ở endpoint POST: `POST /api/assignments/{assignmentId}/submissions`. Client gửi kèm toàn bộ thông tin gồm: `assignmentId` trên URI, cùng `studentCode` và `repositoryUrl` trong Body JSON `CreateSubmissionRequest`.
  - Server không cần nhớ trước đó Client đã "chọn bài tập nào" hay "đăng nhập lúc nào". Máy chủ chỉ cần đọc request hiện tại là xử lý được trọn vẹn. Nhờ vậy, nếu hệ thống có 10 máy chủ chạy cân bằng tải (Load Balancer), request 1 vào server A và request 2 vào server B vẫn xử lý hoàn toàn chính xác.
  - *"Stateless không có nghĩa là server không lưu dữ liệu"*: Danh sách `assignments` và `submissions` vẫn được lưu trên máy chủ; server chỉ không lưu ngữ cảnh hội thoại của từng client.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Khái niệm DTO (Data Transfer Object):** DTO là hợp đồng trao đổi dữ liệu ở biên hệ thống, tách biệt hoàn toàn mô hình lưu trữ nội bộ của server khỏi những gì truyền qua mạng.
- **Giao tiếp dựa trên thông điệp (HTTP + JSON):** Hai tiến trình chạy độc lập, có thể ở hai máy tính khác nhau, không chung bộ nhớ RAM và không chia sẻ thư viện nhị phân C#. Giao tiếp giữa chúng chỉ là **chuỗi byte JSON** truyền qua giao thức HTTP.
- **Sự khớp nhau qua Serialization:** Cả hai bên chỉ cần thỏa thuận cấu trúc JSON:
  - Khi server trả về `SubmissionDto`, framework ASP.NET tuần tự hóa (serialize) thành JSON `{ "id": "...", "assignmentId": 1, "studentCode": "SV001", ... }`.
  - Khi client đọc bằng `ReadFromJsonAsync<SubmissionDto>()`, thư viện `System.Net.Http.Json` đọc các trường camelCase trong JSON và giải tuần tự hóa (deserialize) vào record `SubmissionDto` cục bộ của client.
  - Client và server tiến hóa độc lập: Server có thể đổi tên biến nội bộ, thêm thuộc tính riêng (như `RawScore`, `GraderNote`) mà không làm hỏng máy khách.

---

## 3. Bảng phá thử (4 thí nghiệm phân tán - Bước 9)

| STT | Thí nghiệm | Thao tác thực hiện | Kết quả thực tế quan sát được | Bài học về hệ phân tán |
| :---: | :--- | :--- | :--- | :--- |
| **1** | **Chạy máy khách lần 2** *(máy chủ vẫn chạy)* | Giữ Server cổng 5000 chạy, chạy lại Client lần 2 | Client lần 2 thấy danh sách đã có lượt nộp trước, nộp mới nhận ID Guid mới; nếu nộp trùng nhận `409 Conflict` | **Trạng thái nằm ở máy chủ, không ở máy khách.** Client chỉ hỏi và nhận dữ liệu, không tự giữ dữ liệu gốc. |
| **2** | **Tắt máy chủ** | Dừng Server (Ctrl+C), sau đó chạy Client | `Unhandled exception. HttpRequestException: Connection refused (localhost:5000)` | **Lỗi chỉ có ở phân tán: bên kia có thể vắng mặt.** Lời gọi hàm nội bộ luôn chạy, nhưng qua mạng server có thể bị tắt, lỗi mạng, sai cổng. |
| **3** | **Chạy lại máy chủ** | Bật lại Server cổng 5000, gọi `GET /api/assignments` và `GET /api/submissions` | Server khởi động lại sạch sẽ, các lượt nộp cũ trong RAM biến mất (chỉ còn dữ liệu mẫu ban đầu) | **Dữ liệu trong bộ nhớ (In-memory RAM) mất khi tiến trình kết thúc.** Cần CSDL lâu dài (như SQLite, SQL Server) ở các bài sau. |
| **4** | **Sai cổng** | Chạy Client truyền tham số cổng sai: `dotnet run -- http://localhost:5999` | `Connection refused (localhost:5999)` | **Hai bên phải thống nhất địa chỉ (URL).** URL cấu hình sai sẽ không thể bắt tay kết nối. |

---

## 4. Hướng dẫn chạy và kiểm thử

### Cách 1: Chạy tự động (Khuyên dùng)
Chạy tệp `Chay_EduSubmit_Tu_Dong.bat` có sẵn trong thư mục bài tập. Script sẽ tự động:
1. Khởi động `Submission.Api` ở cổng 5000 ngầm.
2. Chạy `Submission.Client` thực hiện chuỗi 4 nấc kiểm thử nộp bài tự động.
3. Dừng máy chủ an toàn sau khi hoàn tất.

### Cách 2: Chạy bằng dòng lệnh thủ công
- **Terminal 1 (Máy chủ):**
  ```powershell
  cd src/Submission.Api
  dotnet run --urls http://localhost:5000
  ```
- **Terminal 2 (Máy khách):**
  ```powershell
  cd src/Submission.Client
  dotnet run -- http://localhost:5000
  ```
- **Kiểm thử bằng REST Client (VS Code):**
  Mở tệp `requests.http` và nhấn nút `Send Request` trên từng mục để kiểm tra đầy đủ 12 ca test.

