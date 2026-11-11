# Bài 2: RESTful API cho tương tác client–server
**Môn học:** Phát triển ứng dụng phân tán  
**Sinh viên:** Long Minh Thành  
**Mã sinh viên:** dtc245200344  
**Lớp:** CNTTK23E  

---

## 1. Cấu trúc bài nộp

Thư mục `Bai2` bao gồm hai phần thực hành theo yêu cầu bài học:

1. **`CatalogApi/` (Phần 2: Catalog API)**:
   - `src/Catalog.Api`: Máy chủ RESTful API quản lý danh mục sản phẩm (cổng `5100`).
   - `src/Catalog.Client`: Máy khách Console gọi API qua `HttpClient` (4 nấc).
   - `requests.http`: Tập hợp các ca kiểm thử bằng REST Client.
   - `sp-hop-le.json`, `sp-sai.json`: Tệp body JSON phục vụ kiểm thử `curl.exe`.

2. **`RestfulClientServer/` (Phần 3: EduSubmit)**:
   - `src/Submission.Api`: Máy chủ RESTful API nộp bài tập (cổng `5000`) với tài nguyên con `/api/assignments/{id}/submissions`.
   - `src/Submission.Client`: Máy khách nộp bài tập (kịch bản 4 nấc: xem -> nộp -> đọc lại -> thử lỗi).
   - `requests.http`: 10 ca kiểm thử bao phủ toàn bộ các mã trạng thái (`200`, `201`, `400`, `404`, `409`, `204`, `415`).

---

## 2. Bảng hợp đồng API (API Contract)

### 2.1. Hợp đồng Catalog API (Cổng 5100)
| Phương thức | URI | Thành công | Lỗi có thể gặp | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/` | `200 OK` | - | Kiểm tra trạng thái máy chủ |
| **GET** | `/api/products` | `200 OK` | - | Lấy danh sách sản phẩm |
| **GET** | `/api/products/{id:int}` | `200 OK` | `404 PRODUCT_NOT_FOUND` | Lấy thông tin chi tiết một sản phẩm |
| **POST** | `/api/products` | `201 Created` + `Location` | `400 INVALID_PRODUCT`<br>`415 Unsupported Media Type` | Thêm mới một sản phẩm |

### 2.2. Hợp đồng EduSubmit API (Cổng 5000)
| Phương thức | URI | Thành công | Lỗi có thể gặp | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/` | `200 OK` | - | Kiểm tra trạng thái máy chủ |
| **GET** | `/api/assignments` | `200 OK` | - | Danh sách bài tập đang mở |
| **GET** | `/api/assignments/{assignmentId:int}` | `200 OK` | `404 ASSIGNMENT_NOT_FOUND` | Chi tiết một bài tập |
| **POST** | `/api/assignments/{assignmentId:int}/submissions` | `201 Created` + `Location` | `404 ASSIGNMENT_NOT_FOUND`<br>`400 ASSIGNMENT_CLOSED`<br>`400 INVALID_SUBMISSION`<br>`409 DUPLICATE_SUBMISSION` | Nộp bài cho bài tập |
| **GET** | `/api/submissions/{submissionId:guid}` | `200 OK` | `404 SUBMISSION_NOT_FOUND` | Đọc lại lượt nộp bài |
| **DELETE** | `/api/submissions/{submissionId:guid}` | `204 NoContent` | `404 SUBMISSION_NOT_FOUND` | Xóa/rút lại bài đã nộp |

---

## 3. Trả lời 3 câu hỏi giải thích (Slide 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời:**
  - Theo nguyên lý thiết kế REST (Representational State Transfer), **URI** đóng vai trò định danh **tài nguyên (Resource)** trong hệ thống. Tài nguyên là đối tượng dữ liệu nên phải được biểu diễn bằng **danh từ** (ví dụ: `/api/products`, `/api/assignments`, `/api/submissions`).
  - Các hành động tác động lên tài nguyên đã được chuẩn hóa bởi các **phương thức HTTP (HTTP verbs)**:
    - `GET`: Lấy biểu diễn của tài nguyên (an toàn, lũy đẳng).
    - `POST`: Tạo mới tài nguyên con hoặc gửi dữ liệu xử lý (không an toàn, không lũy đẳng).
    - `PUT`/`PATCH`: Cập nhật trạng thái tài nguyên.
    - `DELETE`: Xóa tài nguyên (lũy đẳng).
  - Quy ước này giúp API có tính đồng nhất cao, rõ ràng, tránh sinh ra các URI động từ rườm rà như `/getProducts`, `/createProduct`, `/doSubmit`.

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Trả lời:**
  - **Stateless (phi trạng thái)** nghĩa là máy chủ không lưu giữ bất kỳ ngữ cảnh hội thoại (session) nào của máy khách giữa hai yêu cầu liên tiếp.
  - Mỗi yêu cầu HTTP gửi đến máy chủ phải **tự chứa đầy đủ thông tin** cần thiết để máy chủ hiểu và xử lý độc lập:
    - Ví dụ trong endpoint `POST /api/assignments/{assignmentId}/submissions`: `assignmentId` nằm trực tiếp trên đường dẫn URI, mã sinh viên và repository URL nằm trong body JSON (`CreateSubmissionRequest`).
    - Máy chủ không cần nhớ trước đó máy khách đã gọi hàm nào, hay đang chọn bài tập nào ở màn hình trước. Bất kỳ máy chủ nào trong cụm phân tán (load balancing) nhận được request này đều xử lý được ngay.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời:**
  - Máy khách và máy chủ là hai chương trình độc lập chạy trên hai tiến trình riêng biệt, không dùng chung bộ nhớ và không dùng chung mã nguồn.
  - **DTO (Data Transfer Object)** là bản hợp đồng dữ liệu ở biên hệ thống. Khi truyền qua mạng, dữ liệu được chuyển đổi thành chuỗi văn bản JSON.
  - Cơ chế tuần tự hóa / giải tuần tự hóa (`System.Text.Json`) ánh xạ dữ liệu dựa trên **tên và kiểu thuộc tính trong JSON** (khớp theo quy ước camelCase mặc định). Do đó, dù mỗi bên định nghĩa một record C# riêng, miễn là tên trường tương thích với JSON thì dữ liệu truyền nhận vẫn khớp nhau hoàn toàn.

---

## 4. Kết quả thực nghiệm 4 kịch bản hệ phân tán (Bước 9)

| Thí nghiệm | Thao tác | Kết quả quan sát | Bài học rút ra |
| :--- | :--- | :--- | :--- |
| **1. Chạy Client lần 2** | Giữ máy chủ, chạy lại Client | Thấy 4 sản phẩm, tạo mới ID = 5 | **Trạng thái nằm ở máy chủ**, không ở máy khách. Mọi client đều nhìn thấy dữ liệu chung. |
| **2. Tắt máy chủ** | Dừng máy chủ, chạy Client | Báo lỗi `HttpRequestException: Connection refused` | **Lỗi đặc thù trong phân tán**: Bên kia có thể vắng mặt hoặc mất mạng bất cứ lúc nào. |
| **3. Khởi động lại máy chủ** | Bật lại máy chủ, gọi GET | Dữ liệu quay về 3 sản phẩm ban đầu | **Dữ liệu trong RAM sẽ mất** khi tiến trình dừng. Cần database để lưu trữ bền vững. |
| **4. Sai cổng / URL** | Chạy Client với cổng `5999` | Báo lỗi `Connection refused` | **Thống nhất cấu hình địa chỉ**: Hai bên phải cấu hình chính xác địa chỉ URL/cổng giao tiếp. |
