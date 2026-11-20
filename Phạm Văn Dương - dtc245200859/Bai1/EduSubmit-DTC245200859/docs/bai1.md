# Nhật ký Thực hành Bài 1 – Tổng quan hệ thống và MEP

**Họ tên:** [Phạm Văn Dương]
**MSSV:** DTC245200859
**Lớp:** [CNTT K23E]

---

## 1. Môi trường cài đặt (Bước 4)

- .NET SDK version: 10.x
- IDE / Editor: Visual Studio Code (C# Dev Kit)
- VCS: Git

![Ảnh môi trường cài đặt](./images/step4.png)

---

## 2. Mô hình trao đổi thông điệp - MepDemo (Bước 7)

### Trả lời câu hỏi thu hoạch:

1. **Ở MEP 3 (Queue-based), mỗi bài nộp sub-x được mấy worker xử lý?**
   - Trả lời: Mỗi bài nộp chỉ do **1 worker** xử lý (các worker chia nhau công việc từ hàng đợi, không xử lý trùng việc của nhau).

2. **Ở MEP 4 (Pub/Sub), một sự kiện được gửi tới mấy dịch vụ?**
   - Trả lời: Được gửi đồng thời tới cả **3 dịch vụ** (`ScoreService`, `NotificationService`, `StatsService`).

3. **Mô tả 1 câu ngắn gọn cho mỗi MEP:**
   - **Request–Response:** Bên gửi phát yêu cầu và tạm dừng để chờ bên nhận trả kết quả.
   - **One-way:** Bên gửi phát tin đi rồi tiếp tục công việc ngay mà không chờ phản hồi.
   - **Queue-based:** Đẩy công việc vào hàng đợi chung để các worker chia nhau lấy ra xử lý dần.
   - **Publish–Subscribe:** Phát 1 sự kiện ra hệ thống, tất cả các dịch vụ đăng ký sự kiện đó đều nhận được.
   - **Streaming / Push:** Máy chủ chủ động đẩy liên tục dữ liệu cập nhật về cho máy khách theo thời gian thực.

---

## 3. Web API cơ bản - BasicWebApi (Bước 8)

![Ảnh môi trường cài đặt](./images/step4.png)

### Trả lời câu hỏi thu hoạch:

1. **Dòng `builder.Services...` và `builder.Build()` - dòng nào phải đứng trước? Vì sao?**
   - Trả lời: `builder.Services...` phải đứng **trước**. Vì tất cả dịch vụ (Services) cần đăng ký vào thùng chứa Dependency Injection trước khi gọi `builder.Build()` để đóng đóng gói cấu hình ứng dụng.
2. **Trong `/hello/{name}?language=en`, tham số `name` và `language` lấy từ đâu?**
   - Trả lời: `name` lấy từ **Route parameter** (trực tiếp trên đường dẫn URL), `language` lấy từ **Query string** (tham số đính kèm sau dấu `?`).
3. **Vì sao POST hợp lệ trả `Results.Created` (201) chứ không phải `Results.Ok` (200)?**
   - Trả lời: Chuẩn RESTful quy định mã **201 Created** báo hiệu rõ ràng một tài nguyên mới đã được tạo thành công trên server (kèm vị trí ở header `Location`), trong khi 200 OK chỉ mang ý nghĩa xử lý thành công chung.

---

## 4. Hệ thống phân tán nhỏ - MiniDistributedSystem (Bước 9)

![Ảnh môi trường cài đặt](./images/step4.png)

### Trả lời câu hỏi thu hoạch:

1. **Client gọi những endpoint nào của API? Method, URL, mã trạng thái?**
   - `GET http://localhost:5000/api/assignments` -> Trả về `200 OK`
   - `POST http://localhost:5000/api/submissions` -> Trả về `201 Created`
2. **Client có tham chiếu code của API không? Hai bên thống nhất bằng gì?**
   - Trả lời: Client **không** tham chiếu code API. Hai bên thống nhất giao tiếp qua **giao thức HTTP** và định dạng dữ liệu **JSON** (API contract).
3. **Vì sao địa chỉ API truyền qua `-- http://localhost:5000` mà không hardcode?**
   - Trả lời: Giúp ứng dụng linh hoạt; khi thay đổi môi trường/IP/máy chủ khác chỉ cần truyền tham số mới mà không cần chỉnh sửa hay biên dịch lại code Client.

---

## 5. Bảng thí nghiệm lỗi (Bước 10)

| #     | Thí nghiệm                     | Dự đoán                            | Kết quả thực tế                                            | Đúng/Sai | Giải thích ngắn gọn                                                                      |
| ----- | ------------------------------ | ---------------------------------- | ---------------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------- |
| **a** | Thêm bài tập 3 trên Server     | Client tự hiện thêm bài 3          | Client in ra đủ 3 bài tập                                  | Đúng     | Client nhận dữ liệu cấu trúc JSON động từ API nên tự cập nhật.                           |
| **b** | Tắt API server rồi chạy Client | Kết nối thất bại                   | Lỗi `HttpRequestException: No connection could be made...` | Đúng     | Lời gọi qua mạng phụ thuộc trạng thái server, không như gọi hàm nội bộ chạy trên bộ nhớ. |
| **c** | Gửi StudentCode rỗng           | API trả về lỗi                     | API trả về mã `400 Bad Request`                            | Đúng     | Máy chủ tự kiểm tra tính hợp lệ dữ liệu. Client ném exception khi nhận mã lỗi.           |
| **d** | Đổi cổng API sang 5050         | Kết nối thành công nếu đổi tham số | Client kết nối và nộp bài thành công ở cổng 5050           | Đúng     | Nhờ truyền địa chỉ server linh hoạt qua tham số dòng lệnh `args[0]`.                     |

---

## 6. Dự án EduSubmit tự làm (Bước 11 & 12)

![Ảnh môi trường cài đặt](./images/step4.png)

### Luồng thông điệp trong hệ thống EduSubmit:

1. Client gửi `GET /api/assignments` -> API trả danh sách bài tập (JSON).
2. Client gửi `POST /api/submissions` chứa thông tin nộp bài -> API kiểm tra, lưu vào bộ nhớ và trả về mã `201 Created` cùng dữ liệu bài nộp (có ID).
3. Client lấy ID vừa nhận gửi `GET /api/submissions/{id}` -> API tra cứu từ `ConcurrentDictionary` và trả về thông tin chi tiết bài nộp.

---

## 7. Khó khăn gặp phải & Cách giải quyết

- **Lỗi đường dẫn do máy không có ổ D:** Đã chuyển toàn bộ môi trường làm việc sang ổ `C:\Code_demo`.
- **Lỗi ký tự cấm `<>` khi tạo thư mục PowerShell:** Đã loại bỏ ký tự ngoặc nhọn để đặt tên đúng chuẩn `EduSubmit-DTC245200859`.
- **Cú pháp dán nhầm vào Terminal:** Sử dụng lệnh `cls` để xóa màn hình và nhập lại lệnh chính xác.
