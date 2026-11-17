# Đề tài 18: Hệ thống Đăng ký Sự kiện và Check-in (EventFlow)

**Sinh viên thực hiện:** Phan Quang Tùng  
**Mã sinh viên:** DTC245200250  
**Lớp:** K23.K1.D2.2627.CNTT K23E  
**Học phần:** Phát triển ứng dụng phân tán  

---

## 1. Giới thiệu Đề tài
Dự án **EventFlow** quản lý đăng ký và check-in sự kiện phân tán, giải quyết các bài toán:
- Quản lý danh mục sự kiện và tra cứu số chỗ còn lại theo thời gian thực.
- Đăng ký tham dự, kiểm soát sức chứa (tránh tình trạng overbooking) và sinh mã vé (Registration Code) duy nhất.
- Check-in người tham dự tại cổng sự kiện và ngăn chặn gian lận check-in nhiều lần.
- Cho phép người tham dự tra cứu và hủy đăng ký (tự động hoàn lại chỗ trống).

---

## 2. Danh sách RESTful API tối thiểu (Theo đặc tả đề tài)

| Phương thức | Endpoint URI | Mô tả chức năng | Mã trạng thái HTTP |
|:---:|---|---|:---:|
| `GET` | `/events` | Lấy danh sách sự kiện và số chỗ còn lại | `200 OK` |
| `GET` | `/events/{id}` | Lấy chi tiết thông tin sự kiện | `200 OK`, `404 Not Found` |
| `POST` | `/events/{id}/registrations` | Đăng ký tham dự, sinh mã vé duy nhất | `201 Created`, `400 Bad Request` |
| `DELETE` | `/registrations/{id}` | Hủy đăng ký, hoàn trả lại chỗ trống | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `POST` | `/registrations/{id}/checkin` | Xác nhận check-in, chống check-in 2 lần | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `GET` | `/events/{id}/registrations` | Xem danh sách người tham dự của sự kiện | `200 OK`, `404 Not Found` |
| `POST` | `/events` | Thêm sự kiện mới vào hệ thống | `201 Created`, `400 Bad Request` |
| `GET` | `/health` | Kiểm tra tình trạng hoạt động dịch vụ | `200 OK` |

---

## 3. Hướng dẫn khởi chạy hệ thống (Bằng .NET 10 CLI)

### Bước 1: Di chuyển vào thư mục dự án
```powershell
cd "Phan Quang Tùng - dtc245200250/DuAn/DeTai18_EventFlow"
```

### Bước 2: Build giải pháp
```powershell
dotnet build EventFlow.slnx
```

### Bước 3: Khởi chạy Event API Service
```powershell
dotnet run --project src/EventFlow.EventApi
```
*API sẽ tự động khởi tạo cơ sở dữ liệu SQLite (`eventflow_events.db`) và nạp sẵn 5 sự kiện mẫu.*

### Bước 4: Kiểm thử API
- **Giao diện trực quan Swagger UI:** Truy cập trình duyệt tại **`http://localhost:5000/swagger`**
- **File kiểm thử HTTP Client:** Mở file [`requests.http`](requests.http) trong VS Code (sử dụng extension REST Client) hoặc Visual Studio để gửi trực tiếp các yêu cầu HTTP kiểm thử.
