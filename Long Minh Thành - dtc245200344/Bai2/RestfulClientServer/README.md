# Báo cáo Thực hành Bài 2 - Phần 3: Hệ thống Nộp bài EduSubmit (RestfulClientServer)

## 1. Giới thiệu dự án
Hệ thống **EduSubmit** là một ứng dụng phân tán thu nhỏ theo kiến trúc RESTful API gồm 2 tiến trình độc lập:
- **`Submission.Api`**: Máy chủ RESTful API (ASP.NET Core Minimal API) chạy tại cổng `http://localhost:5000`. Quản lý danh sách bài tập và lượt nộp bài trong bộ nhớ.
- **`Submission.Client`**: Máy khách Console App sử dụng `HttpClient` để tương tác với API qua mạng HTTP + JSON qua 4 nấc kịch bản: xem bài tập -> nộp bài -> đọc lại -> kiểm tra xử lý lỗi.

---

## 2. Bảng hợp đồng API (API Contract)

| Phương thức | URI | Mã thành công | Lỗi có thể gặp | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/assignments` | `200 OK` (danh sách) | - | Xem danh sách bài tập đang mở |
| **GET** | `/api/assignments/{assignmentId}` | `200 OK` (chi tiết) | `404 ASSIGNMENT_NOT_FOUND` | Xem chi tiết một bài tập theo Id |
| **POST** | `/api/assignments/{assignmentId}/submissions` | `201 Created` + header `Location` | `404 ASSIGNMENT_NOT_FOUND`<br>`400 ASSIGNMENT_CLOSED`<br>`400 INVALID_SUBMISSION`<br>`409 DUPLICATE_SUBMISSION`<br>`415 Unsupported Media Type` | Nộp bài cho bài tập tương ứng |
| **GET** | `/api/submissions/{submissionId}` | `200 OK` (chi tiết lượt nộp) | `404 SUBMISSION_NOT_FOUND` | Đọc lại lượt nộp (dùng URI trong header Location) |
| **DELETE** | `/api/submissions/{submissionId}` | `204 NoContent` (không body) | `404 SUBMISSION_NOT_FOUND` | Xóa/rút lại lượt nộp bài |

---

## 3. Hướng dẫn chạy và kiểm thử

### Bước 1: Khởi động máy chủ API (Terminal 1)
```bash
cd d:\Bai2\RestfulClientServer
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

### Bước 2: Chạy máy khách (Terminal 2)
```bash
cd d:\Bai2\RestfulClientServer
dotnet run --project src/Submission.Client -- http://localhost:5000
```

**Kết quả màn hình máy khách hiển thị đủ 4 nấc:**
```text
Danh sach bai tap dang mo:
 - 1: Bai tap REST API
 - 2: Bai tap Client

Da tao luot nop: <guid-vua-tao>
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

### Bước 3: Kiểm thử toàn diện qua tệp `requests.http`
Mở file `requests.http` trong VS Code (cài extension REST Client) hoặc Visual Studio, bấm **Send Request** trên từng ca kiểm thử.

---

## 4. Trả lời 3 câu hỏi giải thích (Slide 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời:**
  - Trong kiến trúc REST, **URI (Uniform Resource Identifier)** dùng để định danh **tài nguyên (Resource)** trong hệ thống, do đó phải là các danh từ (ví dụ: `/api/assignments`, `/api/submissions`).
  - Các thao tác hoặc hành động tác động lên tài nguyên đó đã được chuẩn hóa bởi các **phương thức HTTP (HTTP Methods)**:
    - `GET`: Đọc, lấy dữ liệu (an toàn, lũy đẳng).
    - `POST`: Tạo mới tài nguyên con (không an toàn, không lũy đẳng).
    - `PUT`/`PATCH`: Cập nhật toàn bộ hoặc một phần tài nguyên.
    - `DELETE`: Xóa tài nguyên (lũy đẳng).
  - Quy ước này giúp API trong sáng, dễ mở rộng, tính nhất quán cao, tránh việc tự chế ra các URI động từ rườm rà như `/doSubmit`, `/getAssignments`, `/createProduct`.

---

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Trả lời:**
  - Tính chất **Stateless (phi trạng thái)** thể hiện ở chỗ: Máy chủ không lưu bất kỳ thông tin phiên hội thoại (Session State) nào của máy khách giữa hai lần gọi liên tiếp.
  - Mỗi yêu cầu HTTP máy khách gửi lên đều phải **tự mang đầy đủ thông tin** để máy chủ hiểu và xử lý độc lập:
    - Trong endpoint `POST /api/assignments/{assignmentId}/submissions`: Mã bài tập `{assignmentId}` được đưa thẳng lên URI; thông tin sinh viên và repo nằm trong Body JSON (`CreateSubmissionRequest`).
    - Máy chủ không cần "nhớ" trước đó máy khách đã gọi hàm nào, hay đã chọn bài tập nào. Dù có thêm bao nhiêu bản sao máy chủ phía sau load balancer, bất kỳ máy chủ nào nhận được request cũng xử lý được ngay mà không cần chia sẻ bộ nhớ session.

---

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời:**
  - Máy khách (`Submission.Client`) và máy chủ (`Submission.Api`) là hai tiến trình độc lập, không dùng chung không gian bộ nhớ hay phụ thuộc mã nguồn của nhau.
  - **DTO (Data Transfer Object)** là khuôn mẫu hợp đồng dữ liệu ở biên hệ thống (System Boundary). Khi truyền dữ liệu qua mạng, đối tượng C# được chuyển đổi (Serialize) thành chuỗi JSON và ngược lại (Deserialize).
  - Trình phân giải JSON (System.Text.Json) ánh xạ dữ liệu dựa trên **tên thuộc tính trong JSON** (mặc định khớp theo chuẩn camelCase), vì vậy chỉ cần tên thuộc tính và kiểu dữ liệu trong hai khai báo DTO khớp với hợp đồng JSON thì dữ liệu sẽ được chuyển đổi chính xác mà không cần dùng chung class C#.
