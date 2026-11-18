# BÁO CÁO ĐỒ ÁN DỰ ÁN: RESTful API CHO TƯƠNG TÁC CLIENT - SERVER

**Học phần:** Phát triển ứng dụng phân tán
**Đề tài 12:** Hệ thống phê duyệt tài liệu phân tán (Document Approval System)
**Tiêu đề nộp:** DuAn - dtc241200006 - Lý Thị Hà

---

## 1. Hợp đồng API (API Contract)

| Phương thức HTTP | URI Endpoint | Thành công | Lỗi có thể gặp | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/` | `200 OK` | - | Kiểm tra máy chủ đang hoạt động |
| **GET** | `/documents` | `200 OK` | - | Lấy danh sách toàn bộ tài liệu |
| **GET** | `/documents/{id:int}` | `200 OK` | `404 DOCUMENT_NOT_FOUND` <br> `404` rỗng (nếu id sai kiểu) | Xem chi tiết một tài liệu theo ID |
| **POST** | `/documents` | `201 Created` + `Location` | `400 INVALID_DOCUMENT` <br> `415 Unsupported Media Type` | Tạo tài liệu mới (mặc định trạng thái `Draft`) |
| **POST** | `/documents/{id:int}/submit` | `200 OK` | `404 DOCUMENT_NOT_FOUND` <br> `400 INVALID_STATUS` | Gửi tài liệu đi phê duyệt (`Draft` $\rightarrow$ `Submitted`) |
| **POST** | `/documents/{id:int}/approve` | `200 OK` | `404 DOCUMENT_NOT_FOUND` <br> `400 INVALID_STATUS` | Phê duyệt tài liệu (`Submitted` $\rightarrow$ `Approved`) |
| **POST** | `/documents/{id:int}/reject` | `200 OK` | `404 DOCUMENT_NOT_FOUND` <br> `400 INVALID_STATUS` | Từ chối tài liệu (`Submitted` $\rightarrow$ `Rejected`) |


---

## 2. Bảng phá thử (4 thí nghiệm phân tán)

| STT | Thí nghiệm | Thao tác thực hiện | Kết quả thực tế quan sát được | Bài học về hệ thống phân tán |
| :--- | :--- | :--- | :--- | :--- |
| **1** | **Chạy máy khách lần 2** *(máy chủ vẫn chạy)* | Giữ `Document.Api` cổng 5000 chạy, chạy lại `Document.Client` lần 2 | Client lần 2 thấy danh sách đã có tài liệu ID = 3 tạo ở lần trước, tiếp tục tạo mới nhận ID = 4 và chuyển sang `Submitted` | **Trạng thái nằm ở máy chủ, không ở máy khách.** Client chỉ gọi API để thao tác và nhận dữ liệu, không tự giữ dữ liệu gốc. |
| **2** | **Tắt máy chủ** | Dừng Server (`Ctrl+C`), sau đó chạy `Document.Client` | Chương trình bắt được ngoại lệ `HttpRequestException` và báo `[LOI MANG] Khong the ket noi toi may chu...` | **Lỗi chỉ có ở phân tán:** bên kia có thể vắng mặt. Lời gọi hàm nội bộ luôn chạy, nhưng qua mạng server có thể bị tắt, rớt mạng hoặc sai cổng. |
| **3** | **Chạy lại máy chủ** | Bật lại Server cổng 5000, gọi `GET /documents` | Server khởi động lại sạch sẽ, các tài liệu tạo thêm (ID 3, 4) trong RAM biến mất (chỉ còn 2 tài liệu mẫu ban đầu) | **Dữ liệu trong bộ nhớ (In-memory RAM) mất khi tiến trình kết thúc.** Cần kết nối CSDL lâu dài (SQLite, SQL Server) ở các giai đoạn phát triển tiếp theo. |
| **4** | **Sai cổng** | Chạy Client truyền tham số cổng sai: `dotnet run --project src/Document.Client http://localhost:5999` | Client báo lỗi mạng không kết nối được tới cổng `5999` | **Hai bên phải thống nhất địa chỉ (URL).** URL/Port cấu hình sai sẽ không thể bắt tay kết nối. |

---

## 3. Hướng dẫn chạy và kiểm thử

### Cách 1: Chạy bằng dòng lệnh thủ công (2 tiến trình đồng thời)
Mở 2 cửa sổ Terminal riêng biệt tại thư mục `DU_AN`:

- **Terminal 1 (Khởi động Máy chủ):**
  ```powershell
  dotnet run --project src/Document.Api --urls http://localhost:5000
- **Terminal 1 (Khởi động Máy khách):**
  ```powershell
dotnet run --project src/Document.Client http://localhost:5000
