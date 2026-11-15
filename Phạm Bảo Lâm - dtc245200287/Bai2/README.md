# EduSubmit-DTC245200287

Dự án thực hành môn **Phát triển ứng dụng phân tán** (ASP.NET Core Minimal API trên .NET 10) của sinh viên Phạm Bảo Lâm (MSSV: DTC245200287 - Lớp CNTTK23E).

👉 **[Xem báo cáo thực hành Bài 1 chi tiết (có ảnh minh chứng)](docs/bai1.md)**  
👉 **[Xem báo cáo thực hành Bài 2 chi tiết (có ảnh minh chứng)](docs/bai2.md)**

---

## Các bài thực hành trong Repository

* **Bài 1 (`src/Submission.Api` & `src/Submission.Client`):** Mô hình request-response phân tán đơn giản.
* **Bài 2 (`Catalog.Api` & `Catalog.Client`):** RESTful API Catalog API quản lý sản phẩm, xử lý DTO, validation, mã trạng thái HTTP (201, 400, 404) và lọc Query String.

---

## Hướng dẫn Khởi chạy Dự án

### 1. Chạy Bài 1 (Submission System)

**Terminal 1 (API Server):**
```bash
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

**Terminal 2 (Console Client):**
```bash
dotnet run --project src/Submission.Client -- http://localhost:5000
```

---

### 2. Chạy Bài 2 (Catalog RESTful API)

**Terminal 1 (API Server):**
```bash
dotnet run --project Catalog.Api --urls http://localhost:5100
```

**Terminal 2 (Console Client):**
```bash
dotnet run --project Catalog.Client -- http://localhost:5100
```

---

## Bảng tra cứu Endpoints Bài 2 (Catalog API)

| Phương thức | Đường dẫn (URI) | Mô tả | Mã phản hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/` | Kiểm tra trạng thái máy chủ | `200 OK` |
| **GET** | `/api/products` | Lấy danh sách sản phẩm (Hỗ trợ `?keyword=...`) | `200 OK` |
| **GET** | `/api/products/{id:int}` | Lấy chi tiết sản phẩm theo ID | `200 OK` / `404 Not Found` |
| **POST** | `/api/products` | Tạo sản phẩm mới (Yêu cầu Name & Price > 0) | `201 Created` / `400 Bad Request` |