# Đề tài 19: Hệ thống tra cứu dữ liệu học tập (GraphQL & Multi-Service)

Dự án thuộc học phần **Phát triển ứng dụng phân tán** (Nền tảng **.NET 10**).  
Hệ thống quản lý và tra cứu thông tin **Sinh viên (`HocSinh`)**, **Học phần (`HocPhan`)** và **Đăng ký học phần (`DKHocPhan`)**, hỗ trợ lọc dữ liệu theo học kỳ/trạng thái, truy vấn lồng nhau tổng hợp từ nhiều nguồn dữ liệu và kiểm tra tính hợp lệ tại biên hệ thống.

---

## 1. Kiến trúc & Cấu trúc thư mục dự án

### Sơ đồ kiến trúc tổng thể
```text
Client (Study.Client / REST Client / Browser)
   │
   ▼  (HTTP / GraphQL / REST)
GraphQL.Api (Cổng tổng hợp dữ liệu - Cổng 5000)
   ├──> Student.Service (Quản lý Sinh viên & Đăng ký học phần)
   └──> Course.Service  (Quản lý Danh mục Học phần)
```

### Cấu trúc thư mục Solution (`DuAn_UDPT.slnx`)
```text
D:\DuAn_UDPT\
├── DuAn_UDPT.slnx                # Tệp Solution quản lý toàn bộ các project
├── .gitignore                    # Cấu hình bỏ qua thư mục bin/, obj/ khi đẩy lên Git
├── README.md                     # Tài liệu hướng dẫn cài đặt, build và chạy hệ thống
├── requests.http                 # Tệp kịch bản kiểm thử các Endpoint (200, 201, 400, 404, 409)
└── src/
    ├── GraphQL.Api/              # Máy chủ API chính (Chạy ở cổng 5000)
    │   └── Program.cs
    ├── Study.Client/             # Máy khách Console App gọi API qua HttpClient
    │   └── Program.cs
    ├── Student.Service/          # Dịch vụ quản lý dữ liệu Sinh viên (gRPC Service)
    └── Course.Service/           # Dịch vụ quản lý dữ liệu Học phần (gRPC Service)
```

---

## 2. Yêu cầu môi trường cài đặt

1. **.NET SDK:** Phiên bản **.NET 10.0** trở lên.  
   Kiểm tra trong Terminal:
   ```powershell
   dotnet --version
   ```
   *(Kết quả hiển thị dạng `10.0.xxx`)*
2. **Công cụ soạn thảo:** Visual Studio Code (cài đặt extension **C# Dev Kit** và **REST Client**) hoặc Visual Studio 2022/2026.
3. **Quản lý mã nguồn:** Git.

---

## 3. Hướng dẫn Build và Chạy chương trình

### Bước 1: Mở Terminal tại thư mục gốc của dự án
```powershell
cd D:\DuAn_UDPT
```

### Bước 2: Biên dịch (Build) toàn bộ Solution
```powershell
dotnet build
```
*Kết quả mong đợi:* `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Bước 3: Khởi chạy máy chủ `GraphQL.Api` (Terminal 1)
```powershell
dotnet run --project src/GraphQL.Api --urls http://localhost:5000
```
*Kết quả mong đợi:* Terminal hiển thị `Now listening on: http://localhost:5000`. Giữ nguyên cửa sổ Terminal này để máy chủ lắng nghe yêu cầu.

### Bước 4: Khởi chạy máy khách `Study.Client` (Terminal 2)
Mở thêm một cửa sổ Terminal thứ hai (bấm nút **Split Terminal** hoặc dấu `+` trong VS Code) và chạy:
```powershell
cd D:\DuAn_UDPT
dotnet run --project src/Study.Client -- http://localhost:5000
```

---

## 4. Bảng Hợp đồng API (API Contract)

| Phương thức | URI | Tham số / Body đầu vào | Thành công | Mã lỗi & `ApiError.Code` |
| :---: | :--- | :--- | :--- | :--- |
| `GET` | `/` | — | `200 OK` + `{ service, status }` | — |
| `GET` | `/api/students` | — | `200 OK` + Danh sách `HocSinhDto` | — |
| `GET` | `/api/courses` | Query: `?hocky=2026-2` *(tùy chọn)* | `200 OK` + Danh sách `HocPhanDto` | — |
| `GET` | `/api/HocSinhs/{id}` | Route: `id` (VD: `SV001`) | `200 OK` + `ChiTietHocSinhDto` *(gồm tổng tín chỉ & danh sách môn học lồng bên trong)* | `404 Not Found` (`STUDENT_NOT_FOUND`) |
| `GET` | `/api/DKHocPhans` | Query *(tùy chọn)*: `?hocky=...&mahocphan=...&trangthai=...` | `200 OK` + Danh sách `DKHocPhanDto` thỏa điều kiện lọc | — |
| `POST` | `/api/DKHocPhans` | Body JSON: `TaoDKHocPhanRequest` `{ maSinhVien, maHocPhan, hocKy }` | `201 Created` + Header `Location` + `DKHocPhanDto` vừa tạo | `400` (`INVALID_ENROLLMENT`)<br>`404` (`STUDENT_NOT_FOUND` / `COURSE_NOT_FOUND`)<br>`409` (`DUPLICATE_ENROLLMENT`) |

---

## 5. Hướng dẫn Kiểm thử nhanh (Bằng Trình duyệt hoặc `curl.exe`)

Khi máy chủ đang chạy ở **Terminal 1** (`http://localhost:5000`), bạn có thể mở **Terminal 2** và chạy các lệnh kiểm thử sau:

### 5.1. Kiểm tra trạng thái máy chủ (`200 OK`)
```powershell
curl.exe -i http://localhost:5000/
```

### 5.2. Xem danh sách sinh viên (`200 OK`)
```powershell
curl.exe -i http://localhost:5000/api/students
```

### 5.3. Lọc danh sách học phần theo học kỳ `2026-2` (`200 OK`)
```powershell
curl.exe -i "http://localhost:5000/api/courses?hocky=2026-2"
```

### 5.4. Truy vấn lồng nhau: Xem chi tiết sinh viên `SV001` kèm các môn đã đăng ký (`200 OK`)
```powershell
curl.exe -i http://localhost:5000/api/HocSinhs/SV001
```

### 5.5. Tra cứu sinh viên không tồn tại (`404 Not Found` + `ApiError`)
```powershell
curl.exe -i http://localhost:5000/api/HocSinhs/SV999
```

### 5.6. Lọc danh sách đăng ký học phần theo học kỳ (`200 OK`)
```powershell
curl.exe -i "http://localhost:5000/api/DKHocPhans?hocky=2026-2"
```

---

## 6. Giải thích các nguyên lý thiết kế hệ thống phân tán (Bài 1 & Bài 2)

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   * Trong kiến trúc REST, URI dùng để định danh **tài nguyên (Resource)** như `/api/students`, `/api/courses`, `/api/DKHocPhans`. Hành động thao tác lên tài nguyên đã được chuẩn hóa qua phương thức HTTP (`GET` để đọc/tra cứu, `POST` để tạo mới). Nhờ vậy, hợp đồng API rõ ràng, thống nhất và không bị trùng lặp kiểu gọi hàm (`/getStudents`, `/createEnrollment`).

2. **Tính chất “Stateless” (Phi trạng thái) thể hiện ở đâu trong code đã viết?**
   * Máy chủ `GraphQL.Api` không lưu phiên hội thoại (session) của máy khách giữa hai lần gọi. Mỗi yêu cầu HTTP từ máy khách (như `GET /api/HocSinhs/SV001` hay `POST /api/DKHocPhans` kèm body JSON) đều tự mang đầy đủ thông tin cần thiết để máy chủ hiểu và xử lý độc lập.

3. **Vì sao Client và Server mỗi bên khai báo DTO (`record`) riêng mà vẫn khớp nhau?**
   * Hai chương trình chạy ở hai tiến trình độc lập, không dùng chung bộ nhớ RAM và không tham chiếu code của nhau. Khi truyền qua mạng, các đối tượng C# được tuần tự hóa (serialize) thành chuỗi văn bản **JSON**. Chỉ cần tên thuộc tính và kiểu dữ liệu trong `record` của hai bên tuân thủ đúng **Hợp đồng API (API Contract)** thì dữ liệu JSON gửi đi và nhận về sẽ tự động ánh xạ khớp nhau.
