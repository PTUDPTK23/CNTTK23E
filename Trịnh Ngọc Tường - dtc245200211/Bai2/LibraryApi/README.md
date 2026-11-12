# LibraryApi — Bài tập Vận dụng & Đổi miền (Slide 104 #5 & Slide 105)

Ứng dụng quản lý sách thư viện xây dựng theo kiến trúc RESTful Client–Server gồm 2 tiến trình giao tiếp qua HTTP + JSON:
- `src/Library.Api`: ASP.NET Core Minimal API lắng nghe tại `http://localhost:5200`
- `src/Library.Client`: Console Client dùng `HttpClient` (chuỗi 4 nấc)
- `requests.http`: Tập hợp 11 ca kiểm thử (bao phủ `200`, `201`, `400`, `404`, `409`, `405`, `415`)
- `book-hop-le.json`, `book-sai.json`: Dữ liệu mẫu dùng kiểm thử `POST /api/books` bằng `curl.exe`

## Lệnh build và chạy

Build cả solution:
```powershell
cd LibraryApi
dotnet build
```

Terminal 1 — chạy máy chủ:
```powershell
dotnet run --project src/Library.Api --urls http://localhost:5200
```

Terminal 2 — chạy máy khách:
```powershell
dotnet run --project src/Library.Client -- http://localhost:5200
```

---

## 1. Bảng trả lời 5 câu hỏi thiết kế API (Slide 26 & Slide 105 #1)

| # | Câu hỏi | Trả lời cho LibraryApi | Thể hiện trong code C# |
|---|---|---|---|
| ① | Hệ thống có những danh từ nào? | Sách (`books`), trạng thái dịch vụ (`/`) | `/api/books`, `/api/books/{id}` |
| ② | Người dùng muốn làm gì với từng danh từ? | Xem danh sách sách, xem chi tiết 1 cuốn sách, thêm sách mới | `MapGet("/api/books", ...)`, `MapGet("/api/books/{id:int}", ...)`, `MapPost("/api/books", ...)` |
| ③ | Máy khách gửi gì, nhận gì? Trường nào máy chủ tự sinh? | Client gửi `CreateBookRequest(Title, Author, PublishYear)`; Server trả `BookDto(Id, Title, Author, PublishYear)`. Máy chủ tự sinh `Id`. | `record CreateBookRequest(...)` / `record BookDto(...)` |
| ④ | Có thể sai ở đâu? Sai thì nói gì? | Không tìm thấy sách (`404 BOOK_NOT_FOUND`), dữ liệu trống/năm <= 0 (`400 INVALID_BOOK`), trùng tiêu đề sách (`409 DUPLICATE_BOOK`), framework từ chối (`404` rỗng, `405`, `415`). | `Results.NotFound(new ApiError(...))`, `Results.BadRequest(new ApiError(...))`, `Results.Conflict(new ApiError(...))` |
| ⑤ | Ai gọi, theo thứ tự nào, lỗi thì làm gì? | `Library.Client` gọi tuần tự 4 nấc: lấy danh sách → thêm sách mới (`201` + `Location`) → đọc lại chi tiết sách vừa tạo → cố ý gửi sai để đọc `ApiError` (`400`). Luôn kiểm tra `StatusCode` trước khi đọc body. | `src/Library.Client/Program.cs` (`HttpClient` + `PrintApiErrorAsync`) |

---

## 2. Bảng hợp đồng API của LibraryApi (Slide 105 #2)

| Phương thức | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| `GET` | `/` | `200 OK` + `{ service, status }` | – |
| `GET` | `/api/books` | `200 OK` + danh sách `BookDto[]` | – |
| `GET` | `/api/books/{id}` | `200 OK` + một `BookDto` | `404 Not Found` (`BOOK_NOT_FOUND`)<br>`404` rỗng nếu `id` không phải số nguyên (`:int`) |
| `POST` | `/api/books` | `201 Created` + header `Location` + `BookDto` mới | `400 Bad Request` (`INVALID_BOOK`)<br>`409 Conflict` (`DUPLICATE_BOOK`)<br>`415 Unsupported Media Type` nếu thiếu `Content-Type` |

Mọi lỗi nghiệp vụ từ handler đều dùng chung một khuôn:
`ApiError { "code": "...", "message": "..." }`

---

## 3. Kết quả kiểm thử thực tế (`curl.exe`, `requests.http` & `Library.Client`)

| Ca | Yêu cầu | Kết quả quan sát được | Bên trả lời |
|---|---|---|---|
| 1 | `GET /` | `200 OK` (`{"service":"Library.Api","status":"running"}`) | Handler |
| 2 | `GET /api/books` | `200 OK` (mảng 3 sách ban đầu) | Handler |
| 3 | `GET /api/books/1` | `200 OK` (`Clean Code`) | Handler |
| 4 | `GET /api/books/999` | `404 Not Found` + `BOOK_NOT_FOUND` | Handler |
| 5 | `GET /api/books/abc` | `404 Not Found`, `Content-Length: 0` (body rỗng) | Framework (`:int`) |
| 6 | `POST /api/books` (`book-hop-le.json`) | `201 Created`, `Location: /api/books/4`, `BookDto` mới | Handler |
| 7 | `POST /api/books` (`book-sai.json`) | `400 Bad Request` + `INVALID_BOOK` | Handler |
| 8 | `POST /api/books` (gửi lại tiêu đề đã tồn tại) | `409 Conflict` + `DUPLICATE_BOOK` | Handler |
| 9 | `PUT /api/books/1` | `405 Method Not Allowed`, `Allow: GET` | Framework |
| 10 | `POST /api/books` (thiếu `Content-Type`) | `415 Unsupported Media Type` | Framework |
| 11 | Chạy `Library.Client` lần 1 | Đủ 4 nấc: liệt kê sách → tạo `[5] Domain-Driven Design` (`Location: /api/books/5`) → đọc lại chi tiết → gửi sai nhận `400 BadRequest` + `INVALID_BOOK` | Client + Handler |
| 12 | Chạy `Library.Client` lần 2 (máy chủ đang chạy) | Liệt kê 5 sách, sau đó dừng ở nấc 2 với `409 Conflict` (`DUPLICATE_BOOK`) nhờ kiểm tra `StatusCode != Created` | Client + Handler |

---

## 4. Thí nghiệm phân tán (Bước 9) & Phiếu tự kiểm tra (Bước 10)

| # | Thí nghiệm / Kiểm tra | Kết quả thực tế | Trạng thái |
|---|---|---|---|
| 1 | `GET /` | `200 OK` | ✓ Đạt |
| 2 | `GET /api/books` | `200 OK`, 3 phần tử ban đầu | ✓ Đạt |
| 3 | `GET /api/books/1` | `200 OK` | ✓ Đạt |
| 4 | `GET /api/books/999` | `404` + `ApiError` (`BOOK_NOT_FOUND`) | ✓ Đạt |
| 5 | `GET /api/books/abc` | `404` rỗng (`Content-Length: 0`) | ✓ Đạt |
| 6 | `POST` hợp lệ | `201` + `Location: /api/books/4` | ✓ Đạt |
| 7 | `POST` dữ liệu sai / trùng tiêu đề | `400` (`INVALID_BOOK`) / `409` (`DUPLICATE_BOOK`) | ✓ Đạt |
| 8 | `PUT /api/books/1` / `POST` thiếu `Content-Type` | `405` / `415` | ✓ Đạt |
| 9 | Chạy `Library.Client` đủ 4 nấc | Lần 1 đủ 4 nấc; lần 2 xử lý `409` sạch sẽ | ✓ Đạt |
| 10 | Tắt máy chủ rồi chạy `Library.Client` | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5200)` | ✓ Đạt |

