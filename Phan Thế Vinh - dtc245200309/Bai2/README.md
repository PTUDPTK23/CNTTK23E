# Nhật ký Bài 2 – RESTful API cho tương tác Client–Server

- **Họ và tên:** Phan Thế Vinh
- **MSSV:** DTC245200309
- **Lớp:** CNTT K23E
- **GitHub:** `phan-the-vinh`
- **Thư mục nộp bài (Slide 90):** `Phan Thế Vinh - dtc245200309/Bai2/`
- **Tiêu đề Pull Request dự kiến:** `Bai2 - DTC245200309 - Phan Thế Vinh`

---

## 1. Cấu trúc thư mục Bài 2

```text
Phan Thế Vinh - dtc245200309/Bai2/
├── README.md                          # Nhật ký tổng hợp Bài 2 (Catalog + EduSubmit)
├── docs/images/                       # Ảnh chụp minh chứng
├── CatalogApi/                        # Phần 2 – Catalog API & Client (cổng 5100)
│   ├── .gitignore
│   ├── CatalogApi.slnx
│   ├── requests.http
│   ├── sp-hop-le.json
│   ├── sp-sai.json
│   └── src/
│       ├── Catalog.Api/
│       └── Catalog.Client/
└── RestfulClientServer/               # Phần 3 – EduSubmit RESTful API & Client (cổng 5000)
    ├── .gitignore
    ├── RestfulClientServer.slnx
    ├── README.md
    ├── requests.http
    ├── sv001.json
    ├── rong.json
    ├── docs/images/
    └── src/
        ├── Submission.Api/
        └── Submission.Client/
```

---

## 2. Phần 2 – Catalog API (`CatalogApi/`, cổng `5100`)

### 2.1. Các lệnh đã chạy (tại `CatalogApi/`)

```powershell
dotnet build CatalogApi.slnx
# Terminal 1 – Máy chủ
dotnet run --project src/Catalog.Api --urls http://localhost:5100
# Terminal 2 – Máy khách
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

### 2.2. Kết quả chạy `Catalog.Client` (4 nấc)

```text
Danh sach san pham:
  [1] Giao trinh Lap trinh phan tan - 150,000 VND
  [2] Tai lieu RESTful API - 90,000 VND
  [3] Bai tap thuc hanh .NET - 120,000 VND

Da tao san pham moi: [4] Nhap mon REST API
Doc lai tai: /api/products/4

Chi tiet san pham 1: Giao trinh Lap trinh phan tan

Thu gui du lieu sai: 400 BadRequest
INVALID_PRODUCT: Ten san pham khong duoc de trong va gia phai lon hon 0.
```

### 2.3. Phiếu tự kiểm tra Catalog (Slide 63 – Bước 10)

| # | Yêu cầu | Mong đợi | Kết quả thực tế | Đạt |
|---|---|---|---|---|
| 1 | `GET /` | `200` | `200 OK` + `{"service":"Catalog.Api","status":"running"}` | [x] |
| 2 | `GET /api/products` | `200`, 3 phần tử | `200 OK`, mảng 3 sản phẩm ban đầu (`id: 1, 2, 3`) | [x] |
| 3 | `GET /api/products/1` | `200` | `200 OK`, `Giao trinh Lap trinh phan tan` | [x] |
| 4 | `GET /api/products/999` | `404` + `ApiError` | `404 Not Found`, `PRODUCT_NOT_FOUND` | [x] |
| 5 | `GET /api/products/abc` | `404` rỗng | `404 Not Found`, `Content-Length: 0` | [x] |
| 6 | `POST` hợp lệ (`sp-hop-le.json`) | `201` + `Location` | `201 Created`, `Location: /api/products/4` | [x] |
| 7 | `POST` giá âm (`sp-sai.json`) | `400` + `ApiError` | `400 Bad Request`, `INVALID_PRODUCT` | [x] |
| 8 | `POST` thiếu `Content-Type` | `415` | `415 Unsupported Media Type` | [x] |
| 9 | Máy khách chạy | đủ 4 nấc | In đủ 4 nấc (tạo mới, đọc lại chi tiết, xử lý lỗi `400`) | [x] |
| 10 | Tắt máy chủ, chạy máy khách | lỗi kết nối | `HttpRequestException: No connection could be made... (localhost:5100)` | [x] |

---

## 3. Phần 3 – EduSubmit (`RestfulClientServer/`, cổng `5000`)

### 3.1. Các lệnh đã chạy (tại `RestfulClientServer/`)

```powershell
dotnet build RestfulClientServer.slnx
# Terminal 1 – Máy chủ
dotnet run --project src/Submission.Api --urls http://localhost:5000
# Terminal 2 – Máy khách
dotnet run --project src/Submission.Client -- http://localhost:5000
```

### 3.2. Kết quả chạy `Submission.Client` (4 bước)

```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client

Da tao luot nop: bac90e27-31b7-44be-b8c2-fe1f89670a14
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

### 3.3. Bảng phá thử & kiểm tra hợp đồng HTTP

| Ca | Yêu cầu / Thí nghiệm | Kết quả mong đợi | Kết quả thực tế | Giải thích |
|---|---|---|---|---|
| `3` | `GET /api/assignments` | `200 OK` (2 bài tập) | `200 OK`, JSON 2 bài tập mở | Handler trả danh sách bài tập trong bộ nhớ. |
| `4a` | `GET /api/assignments/1` | `200 OK` | `200 OK`, bài tập `1` | Tìm thấy tài nguyên theo route param. |
| `4b` | `GET /api/assignments/999` | `404` + `ASSIGNMENT_NOT_FOUND` | `404 Not Found` + `ApiError` | Handler chạy nhưng không tìm thấy ID 999. |
| `4c` | `GET /api/assignments/abc` | `404` body rỗng | `404 Not Found`, `Content-Length: 0` | Framework chặn trước handler do `:int`. |
| `5a` | `POST /api/assignments/1/submissions` (`sv001.json`) | `201` + `Location` | `201 Created`, `Location: /api/submissions/...` | Qua 3 cổng kiểm tra, server tự sinh GUID & trạng thái `received`. |
| `5b` | `POST` với `rong.json` | `400` + `INVALID_SUBMISSION` | `400 Bad Request` + `ApiError` | Cổng 3 từ chối dữ liệu rỗng. |
| `5c` | `POST /api/assignments/999/submissions` | `404` + `ASSIGNMENT_NOT_FOUND` | `404 Not Found` + `ApiError` | Cổng 1: bài tập cha không tồn tại. |
| `5d` | `POST` thiếu `Content-Type` | `415 Unsupported Media Type` | `415 Unsupported Media Type` | Framework không biết body là JSON. |
| `5e` | `POST` JSON lỗi cú pháp | `400` body text | `400 Bad Request` (`BadHttpRequestException`) | Framework lỗi khi parse JSON. |
| `5f` | `POST` thiếu `repositoryUrl` | `400` + `INVALID_SUBMISSION` | `400 Bad Request` + `ApiError` | Trường thiếu thành `null`, handler bắt được. |
| `5g` | `PUT /api/assignments/1` | `405 Method Not Allowed` | `405 Method Not Allowed`, `Allow: GET` | URI có nhưng không hỗ trợ `PUT`. |
| `5h` | Thử tạm `IsOpen = false` cho bài tập 2 *(đã khôi phục)* | `400` + `ASSIGNMENT_CLOSED` | `400 Bad Request` + `ASSIGNMENT_CLOSED` | Cổng 2: bài tập đã đóng. |
| `6a` | `GET` theo URL ở header `Location` | `200 OK` | `200 OK`, trả đúng lượt nộp | Server tìm thấy bản ghi trong `List`. |
| `6b` | `GET /api/submissions/00000000-...` | `404` + `SUBMISSION_NOT_FOUND` | `404 Not Found` + `ApiError` | Không tìm thấy GUID. |
| `6c` | Tắt API rồi bật lại, `GET` GUID cũ | `404` + `SUBMISSION_NOT_FOUND` | `404 Not Found` + `ApiError` | Dữ liệu trong RAM mất khi API dừng. |
| `7a` | Tắt API rồi chạy `Submission.Client` | Lỗi kết nối | `HttpRequestException` (`localhost:5000`) | Tiến trình server vắng mặt. |
| `7b` | Chạy `Submission.Client` cổng sai `5999` | Lỗi kết nối | `HttpRequestException` (`localhost:5999`) | Sai địa chỉ dịch vụ. |
| `7c` | Thử tạm thêm bài tập 3 vào API *(đã khôi phục)* | Client tự in 3 dòng | In đủ 3 dòng không sửa code Client | Client đọc theo hợp đồng JSON động. |

---

## 4. Trả lời 3 câu hỏi giải thích (Slide 90)

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   URI định danh **tài nguyên** nghiệp vụ và quan hệ cha–con (ví dụ `/api/assignments`, `/api/assignments/{assignmentId}/submissions`, `/api/submissions/{submissionId}`), còn phương thức HTTP (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`) diễn đạt **hành động** trên tài nguyên đó. Cách tách này giúp URI ổn định, dễ đoán và tránh dư thừa động từ trong đường dẫn.
2. **“Stateless” thể hiện ở đâu trong code đã viết?**
   API không lưu phiên hội thoại hay lựa chọn riêng của từng client giữa các request. Mọi thông tin cần xử lý đều đi kèm trực tiếp trong từng yêu cầu HTTP (ID bài tập trên URI, dữ liệu nộp trong body JSON, hoặc GUID lượt nộp khi tra cứu). Danh sách `products`/`submissions` trong RAM là dữ liệu nghiệp vụ của hệ thống, không phải session state.
3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   Client và Server chạy ở hai tiến trình độc lập, không tham chiếu project của nhau và chỉ trao đổi thông điệp HTTP + JSON. Miễn hai bên giữ đúng tên trường, kiểu dữ liệu và quy ước camelCase của hợp đồng JSON (`AssignmentDto`, `CreateSubmissionRequest`, `SubmissionDto`, `ApiError`, `ProductDto`, `CreateProductRequest`), `System.Text.Json` sẽ tuần tự hóa và giải tuần tự hóa khớp nhau hoàn toàn.

---

## 5. Ảnh minh chứng (người dùng chụp và lưu vào `docs/images/`)

- `![CatalogApi build và Client](docs/images/01-catalog-api-client.png)`
- `![EduSubmit build và cấu trúc](docs/images/02-edusubmit-build.png)`
- `![EduSubmit API và Client 4 bước](docs/images/03-edusubmit-api-client.png)`
- `![Kiểm thử requests.http trên VS Code](docs/images/04-requests-http.png)`
- `![Thí nghiệm phá thử và lỗi kết nối](docs/images/05-experiments.png)`

---

## 6. Ghi chú phần mở rộng (`*` và Phụ lục)

- **Chưa gộp vào bản chính:** Bước 9 mở rộng đánh dấu `*` của EduSubmit (`409 DUPLICATE_SUBMISSION`, `DELETE /api/submissions/{submissionId:guid}` `204 No Content`, thêm bài tập đóng) và Phụ lục Vận dụng của Catalog (`?keyword`, `DELETE`, `NAME_TOO_LONG`, Đổi miền) để giữ đúng phạm vi bắt buộc (`Catalog` 4 endpoint + `EduSubmit` 4 endpoint & chuỗi 4 bước Client chạy lặp lại được).

