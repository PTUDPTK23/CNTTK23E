# Báo cáo Thực hành Bài 2: RESTful API Catalog & EduSubmit

**Học phần:** Phát triển ứng dụng phân tán  
**Lớp:** K23.K1.D2.2627.CNTT K23E  
**Họ và tên:** Diệp Quang Trường  
**Mã sinh viên:** DTC245200019  
**Kho mã nguồn cá nhân:** [https://github.com/Qtruong26/EduSubmit-DTC245200019](https://github.com/Qtruong26/EduSubmit-DTC245200019)  

---

## 1. Giới thiệu tổng quan
Bài thực hành Bài 2 tập trung vào việc thiết kế và hiện thực hóa giao tiếp liên tiến trình qua mạng theo chuẩn kiến trúc **RESTful API** sử dụng **ASP.NET Core Minimal API** và **HttpClient**:
1. **Catalog.Api & Catalog.Client:** Dịch vụ quản lý danh mục sản phẩm (chạy mặc định tại cổng `5100`), hỗ trợ các thao tác xem danh sách, xem chi tiết theo ID, tạo mới có kiểm tra dữ liệu và phản hồi mã lỗi chuẩn.
2. **EduSubmit RESTful:** Nâng cấp hệ thống nộp bài tập phân tán với các ràng buộc RESTful, quan hệ cha–con giữa bài tập và lượt nộp, quản lý trạng thái đồng thời bằng `ConcurrentDictionary`.

---

## 2. Bảng thí nghiệm lỗi và đặc trưng phân tán (Bước 9)

| STT | Tình huống thí nghiệm | Dự đoán trước khi chạy | Kết quả thực tế quan sát được | Giải thích nguyên nhân & Bài học |
|:---:|---|---|---|---|
| **1** | Chạy máy khách `Catalog.Client` lần 2 trong khi máy chủ vẫn đang chạy | Danh sách ban đầu thấy 4 sản phẩm; lượt tạo mới sinh ID = 5 | Danh sách hiển thị 4 sản phẩm, tạo mới sản phẩm ID = 5, đọc lại tại `/api/products/5` | **Trạng thái (State) tập trung ở máy chủ**: Client không lưu trạng thái dữ liệu. Mọi thay đổi do Client gửi lên được Server ghi nhận và ảnh hưởng trực tiếp đến kết quả của các lần gọi tiếp theo từ bất kỳ Client nào. |
| **2** | Tắt máy chủ (Ctrl+C), sau đó chạy máy khách `Catalog.Client` | Máy khách không kết nối được và báo lỗi | Báo ngoại lệ: `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5100)` | **Lỗi phân tán đặc thù**: Trong kiến trúc phân tán, đối tác mạng có thể ngừng hoạt động (crash, mất mạng, tắt). Máy khách bắt buộc phải có cơ chế bắt lỗi và xử lý ngoại lệ giao tiếp mạng. |
| **3** | Khởi động lại máy chủ và chạy lại máy khách | Danh sách sản phẩm quay về trạng thái ban đầu (3 sản phẩm) | Danh sách chỉ còn 3 sản phẩm mẫu mặc định, sản phẩm 4 và 5 đã mất | **Bộ nhớ tạm thời (In-Memory)**: Dữ liệu hiện đang lưu trong RAM (`List<T>`) của tiến trình máy chủ nên sẽ bị xóa khi tiến trình kết thúc. Hệ thống thực tế cần gắn với CSDL bền vững. |
| **4** | Chạy máy khách trỏ vào sai cổng (ví dụ cổng `5999`) | Máy khách không tìm thấy cổng dịch vụ và báo lỗi từ chối kết nối | Báo lỗi `HttpRequestException` (Connection refused) tại cổng 5999 | **Tính độc lập về cấu hình**: Máy khách và máy chủ phải thống nhất địa chỉ endpoint (URL/Port). Tham số hóa URL giúp Client linh hoạt chuyển đổi môi trường mà không cần sửa code. |

---

## 3. Phiếu 10 ô tự kiểm tra (Bước 10)

| STT | Yêu cầu kiểm thử | Kết quả mong đợi | Tự đánh giá |
|:---:|---|---|:---:|
| 1 | `GET /` | `200 OK` kèm JSON: `service` và `status` | Đạt [x] |
| 2 | `GET /api/products` | `200 OK` kèm danh sách 3 sản phẩm mẫu | Đạt [x] |
| 3 | `GET /api/products/1` | `200 OK` kèm thông tin sản phẩm ID = 1 | Đạt [x] |
| 4 | `GET /api/products/999` | `404 Not Found` kèm mã lỗi `PRODUCT_NOT_FOUND` | Đạt [x] |
| 5 | `GET /api/products/abc` | `404 Not Found` (Route constraint `{id:int}` tự chặn tham số sai kiểu) | Đạt [x] |
| 6 | `POST /api/products` (hợp lệ) | `201 Created`, header `Location` trỏ đến sản phẩm mới, body trả về `ProductDto` | Đạt [x] |
| 7 | `POST /api/products` (lỗi giá âm / tên rỗng) | `400 Bad Request` kèm body `ApiError` (`INVALID_PRODUCT`) | Đạt [x] |
| 8 | `POST /api/products` (thiếu `Content-Type`) | `415 Unsupported Media Type` | Đạt [x] |
| 9 | Máy khách `Catalog.Client` chạy tự động | Thực hiện đủ 4 bước: xem danh sách, tạo mới, đọc lại qua Location, xử lý lỗi 400 | Đạt [x] |
| 10 | Tắt máy chủ, chạy máy khách | Bắt ngoại lệ `HttpRequestException`, thông báo không thể kết nối tới server | Đạt [x] |

---

## 4. Hợp đồng dữ liệu (API Contract) và các Endpoint

### 4.1 Danh mục sản phẩm (Catalog API - Cổng 5100)
- `GET /`: Kiểm tra tình trạng dịch vụ.
- `GET /api/products`: Lấy toàn bộ danh sách sản phẩm.
- `GET /api/products/{id:int}`: Lấy chi tiết một sản phẩm theo mã số (ID).
- `POST /api/products`: Thêm mới sản phẩm.
  - Body mẫu hợp lệ (`sp-hop-le.json`):
    ```json
    {
      "name": "Lap trinh ASP.NET",
      "description": "Tai lieu bai so 5",
      "price": 89999
    }
    ```
  - Body mẫu không hợp lệ (`sp-sai.json`):
    ```json
    {
      "name": "San pham loi",
      "description": "",
      "price": -1
    }
    ```

### 4.2 Cấu trúc DTO (Data Transfer Object)
```csharp
// DTO trao đổi qua mạng
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
```

---

## 5. Trả lời các câu hỏi lý thuyết cốt lõi

### 1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- Trong phong cách kiến trúc REST (Representational State Transfer), URI đóng vai trò là **Bộ định danh tài nguyên (Uniform Resource Identifier)**. Nó đại diện cho "thực thể" hoặc "danh từ" (ví dụ: `/api/products`, `/api/assignments`), chứ không đại diện cho hành động hay hàm gọi như RPC.
- Các hành động được chuẩn hóa thông qua các phương thức HTTP (HTTP Verbs):
  - `GET`: Lấy biểu diễn của tài nguyên (an toàn và idempotent).
  - `POST`: Tạo tài nguyên con mới.
  - `PUT`/`PATCH`: Cập nhật/thay thế tài nguyên.
  - `DELETE`: Xóa tài nguyên.
- Lợi ích: Tạo ra một giao diện đồng nhất (**Uniform Interface**), giúp API trực quan, dễ dự đoán và cho phép các tầng mạng trung gian (như proxy, gateway, browser caching) hiểu và tối ưu hóa xử lý.

### 2. Tính chất "Stateless" thể hiện ở đâu trong mã nguồn?
- **Stateless (Phi trạng thái)** nghĩa là máy chủ không lưu giữ bất kỳ ngữ cảnh hội thoại hay trạng thái phiên (session) nào của máy khách giữa các yêu cầu. Mỗi yêu cầu HTTP gửi từ Client lên Server phải tự chứa đầy đủ mọi thông tin cần thiết để Server hiểu và xử lý độc lập.
- Thể hiện trong code:
  - Khi gọi `POST /api/products`, Client gửi toàn bộ dữ liệu `{ name, description, price }` trong body JSON. Server chỉ đọc dữ liệu từ request này để xử lý mà không cần kiểm tra Client trước đó đã gửi gì.
  - Mỗi request là độc lập; nếu hệ thống được triển khai trên nhiều máy chủ (Cluster/Load Balancer), bất kỳ máy chủ nào cũng có thể xử lý request mà không cần đồng bộ phiên người dùng.

### 3. Vì sao Client và Server mỗi bên khai báo DTO riêng mà vẫn hoạt động khớp nhau?
- Client và Server là hai tiến trình phần mềm hoàn toàn độc lập, có thể chạy trên hai máy khác nhau, thậm chí viết bằng hai ngôn ngữ lập trình khác nhau.
- Hai bên không hề chia sẻ mã nguồn hay bộ nhớ in-process. Cầu nối duy nhất là **Hợp đồng dữ liệu (Data Contract)** được mã hóa dưới dạng định dạng chuẩn mở **JSON**.
- Trình tuần tự hóa (Serializer: `System.Text.Json`) ở mỗi bên tự ánh xạ giữa các trường JSON và các thuộc tính tương ứng của C# theo tên thuộc tính (case-insensitive / camelCase). Nhờ vậy, Client và Server có thể phát triển và nâng cấp độc lập mà không bị ràng buộc chặt chẽ về mã nguồn.
