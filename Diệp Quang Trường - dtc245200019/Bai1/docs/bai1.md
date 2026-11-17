# Bài 1 – Tổng quan hệ thống và MEP

**Họ tên:** Diệp Quang Trường  
**MSSV:** DTC245200019  
**Lớp:** CNTT

---

## 1. Cài đặt

- Kết quả kiểm tra phiên bản `.NET SDK` và `Git` trên máy:

![Kiểm tra phiên bản dotnet và git](Hình ảnh1.png)

- Cấu hình thông tin Git cá nhân và chạy thử chương trình mẫu `HelloPtudpt`:

![Cấu hình Git và chạy HelloPtudpt](Hình ảnh2.png)

- Cấu trúc thư mục mã nguồn tại `D:\code_demo`:

![Cấu trúc thư mục code_demo](Hình ảnh3.png)

---

## 2. MepDemo (Bước 7)

**Trả lời các câu hỏi:**

1. **Ở MEP 3 (Queue-based):** Mỗi bài nộp `sub-x` chỉ do **1 worker** xử lý (các worker chia nhau xử lý các bài nộp trong hàng đợi).
2. **Ở MEP 4 (Publish-subscribe):** Một sự kiện được gửi tới **cả 3 dịch vụ** (tất cả các dịch vụ đăng ký đều nhận được sự kiện).
3. **Mô tả ngắn gọn 5 mô hình MEP:**
   - **Request-Response:** Máy khách gửi yêu cầu và chờ phản hồi trực tiếp từ máy chủ.
   - **One-way (Fire-and-Forget):** Máy khách gửi dữ liệu đi mà không chờ phản hồi từ máy chủ.
   - **Queue-based:** Thông điệp được đẩy vào hàng đợi, các worker lấy ra xử lý độc lập từng thông điệp.
   - **Publish-Subscribe:** Nhà xuất bản phát sự kiện, tất cả các ứng dụng đăng ký (subscribers) đều nhận được.
   - **Streaming:** Dữ liệu được truyền/cập nhật liên tục thành một dòng thông điệp giữa các bên.

---

## 3. BasicWebApi (Bước 8)

- Biên dịch thành công dự án `BasicWebApi`:

![Build dự án BasicWebApi thành công](Hình ảnh4.png)

- Chạy API ở cổng 5081 (`http://localhost:5081`) và tiếp nhận request thành công:

![Chạy BasicWebApi ở cổng 5081](Hình ảnh5.png)

- Thử nghiệm gửi request GET `/hello/An?language=ja` trả về kết quả JSON với status `200 OK`:

![Thử nghiệm endpoint tiếng Nhật](Hình ảnh6.png)

**Trả lời các câu hỏi (Mục 8.4):**

1. Dòng `builder.Services.AddSingleton...` phải đứng **trước** `var app = builder.Build();` vì `builder.Build()` sẽ khởi tạo và đóng gói DI Container; mọi dịch vụ bắt buộc phải được đăng ký trước thời điểm này.
2. Trong `/hello/{name}?language=en`:
   - Tham số `name` được lấy từ **Route** (trên đường dẫn URL).
   - Tham số `language` được lấy từ **Query String** (phần sau dấu `?`).
3. POST hợp lệ trả về `Results.Created` (201) thay vì `Results.Ok` (200) vì yêu cầu POST đã tạo mới thành công một tài nguyên trên máy chủ. Mã 201 đúng chuẩn RESTful và đi kèm header `Location` chỉ tới vị trí tài nguyên vừa tạo.

---

## 4. MiniDistributedSystem (Bước 9)

**Trả lời các câu hỏi:**

1. **Các endpoint Client gọi:**
   - `GET /api/assignments`: Trả về danh sách bài tập (Mã `200 OK`).
   - `POST /api/submissions`: Trả về thông tin nộp bài kèm mã GUID (Mã `201 Created`).
2. **Sự phụ thuộc code:** Client **không** tham chiếu hay dùng chung code của API. Hai bên thống nhất thông qua **cấu trúc dữ liệu JSON (Data Contract)** truyền qua môi trường mạng HTTP.
3. **Địa chỉ API truyền qua tham số `-- http://localhost:5000`:** Giúp ứng dụng linh hoạt, không bị viết cứng (hardcode) URL. Khi triển khai sang cổng hoặc môi trường khác, Client có thể dễ dàng thay đổi địa chỉ kết nối mà không cần biên dịch lại code.

---

## 5. Thí nghiệm lỗi (Bước 10)

| #     | Dự đoán (ghi TRƯỚC khi chạy)                              | Kết quả thực tế (mã lỗi / thông báo)                                        | Đúng/Sai | Giải thích                                                                                                                                                  |
| ----- | --------------------------------------------------------- | --------------------------------------------------------------------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **a** | Client hiển thị thêm bài tập thứ 3 mà không cần sửa code. | Client in ra đầy đủ 3 bài tập.                                              | Đúng     | Client lấy mảng JSON động từ API nên khi server cập nhật mảng dữ liệu, Client tự động đọc và hiển thị mảng mới.                                             |
| **b** | Client bị lỗi do không kết nối được tới server.           | Lỗi `HttpRequestException` (No connection could be made...).                | Đúng     | Giao tiếp mạng có thể bị gián đoạn nếu API chưa chạy hoặc sai cổng. Hàm gọi nội bộ (in-process) chạy trong cùng vùng nhớ RAM nên không gặp sự cố mạng.      |
| **c** | API trả về mã lỗi `400 Bad Request`.                      | API trả `400 Bad Request`. Client báo ngoại lệ `EnsureSuccessStatusCode()`. | Đúng     | Server không thể tin tưởng dữ liệu từ client nên phải tự kiểm tra dữ liệu vào. `EnsureSuccessStatusCode()` ném ngoại lệ để thông báo yêu cầu HTTP thất bại. |
| **d** | Client không kết nối được tới cổng 5000.                  | Chạy lại Client với tham số: `dotnet run -- http://localhost:5050`.         | Đúng     | Địa chỉ API được truyền qua dòng lệnh `args[0]`, cho phép thay đổi cổng kết nối linh hoạt từ bên ngoài.                                                     |

- Lỗi thực tế khi chạy thử thí nghiệm lỗi API chưa sẵn sàng hoặc sai cổng:

![Lỗi ném ngoại lệ từ Client](Hình ảnh9.png)

- Lỗi khi biên dịch API bị xung đột hoặc thiếu cấu hình:

![Lỗi Exception khi khởi chạy API](Hình ảnh8.png)

---

## 6. Dự án EduSubmit tự làm (Bước 11-12)

- Luồng thông điệp trong hệ thống:
  - `Client --(HTTP GET /api/assignments)--> Api` : Request-Response
  - `Client --(HTTP POST /api/submissions)--> Api` : Request-Response
  - `Client --(HTTP GET /api/submissions/{id})--> Api` : Request-Response (Bài mở rộng)

---

## 7. Khó khăn gặp phải và cách giải quyết

- **Gặp lỗi xung đột tiến trình / thiếu thông điệp phản hồi:** Đọc kỹ dòng thông báo lỗi đỏ đầu tiên trên Terminal để xác định lỗi phát sinh từ Server hay Client, sau đó kiểm tra lại cổng kết nối (`--urls`).
- **Gặp lỗi thiếu header JSON:** Thêm header `Content-Type: application/json` khi thực hiện các request POST.

---

## 8. Đẩy bài lên Git & GitHub

- Đã tạo Git Tag `bai1` và đẩy code lên GitHub repository thành công:

![Push git tag bai1 lên GitHub](Hình ảnh7.png)

- Giao diện Repository `EduSubmit-DTC245200019` trên GitHub chứa đầy đủ cấu trúc thư mục và Tag `bai1`:

![Giao diện GitHub Repository EduSubmit](github.png)
