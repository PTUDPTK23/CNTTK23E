# Bài 1 – Tổng quan hệ thống và MEP
Họ tên:                               MSSV: DTC245200209              Lớp: 

## 1. Cài đặt
*(Chèn ảnh chụp màn hình terminal chạy lệnh `dotnet --info` và `dotnet --version`)*

## 2. MepDemo (Bước 7)
- **Trả lời câu hỏi:**
  1. *Ở MEP 3, mỗi bài nộp sub-x được mấy worker xử lý?*
     -> Mỗi bài nộp chỉ được **1 worker** nhận và xử lý (cơ chế phân tải hàng đợi - Competing Consumers, các worker chia nhau công việc, không xử lý trùng).
  2. *Ở MEP 4, một sự kiện được gửi tới mấy dịch vụ?*
     -> Một sự kiện được gửi tới **cả 3 dịch vụ** (mô hình Publish-Subscribe gửi bản sao sự kiện tới mọi bên đăng ký nhận thông điệp).
  3. *Mô tả 1 câu cho mỗi MEP:*
     - **Request-Response**: Máy khách gửi yêu cầu và tạm dừng đợi máy chủ xử lý rồi nhận lại thông điệp phản hồi.
     - **One-Way (Fire-and-Forget)**: Bên gửi phát thông điệp đi và tiếp tục ngay mà không quan tâm bên nhận xử lý lúc nào hay có kết quả gì.
     - **Queue-based**: Thông điệp được đưa vào hàng đợi trung gian và phân phối lần lượt cho các tiến trình xử lý độc lập.
     - **Publish-Subscribe**: Bên phát hành thông điệp vào một chủ đề (Topic) và tất cả các bên đăng ký chủ đề đó đều nhận được bản sao.
     - **Streaming**: Dữ liệu được truyền tải liên tục theo luồng thông điệp nối tiếp nhau giữa hai chiều hoặc một chiều theo thời gian thực.

## 3. BasicWebApi (Bước 8)
*(Chèn ảnh chụp màn hình kết quả kiểm thử trên REST Client các mã: 200, 201, 400)*
- **Trả lời 3 câu hỏi mục 8.4:**
  1. *Hai dòng `builder.Services.AddSingleton...` và `var app = builder.Build();` – dòng nào phải đứng trước? Vì sao?*
     -> Dòng `builder.Services.AddSingleton...` bắt buộc phải đứng trước `builder.Build()`. Vì tất cả các dịch vụ phụ thuộc (Dependency Injection) phải được đăng ký vào DI Container trước khi ứng dụng được biên dịch (`Build`) và cố định container.
  2. *Trong `/hello/{name}?language=en`, tham số `name` lấy từ đâu, `language` lấy từ đâu?*
     -> Tham số `name` được lấy từ Route parameter (đường dẫn của URL), còn tham số `language` được lấy từ Query parameter (chuỗi truy vấn sau dấu `?`).
  3. *Vì sao POST hợp lệ trả `Results.Created` (201) chứ không phải `Results.Ok` (200)?*
     -> Theo chuẩn RESTful HTTP, mã `201 Created` biểu thị một tài nguyên mới vừa được tạo thành công trên hệ thống kèm header `Location` chỉ dẫn URL truy cập tài nguyên đó; trong khi `200 OK` chỉ là phản hồi thành công chung mà không khẳng định việc tạo mới tài nguyên.

## 4. MiniDistributedSystem (Bước 9)
*(Chèn ảnh chụp màn hình 2 terminal chạy song song: API và Client)*
- **Trả lời 3 câu hỏi:**
  1. *Client gọi những endpoint nào của API? Mỗi lần gọi là một cặp request–response gồm method, URL, mã trạng thái gì?*
     - Lần 1: `GET http://localhost:5000/api/assignments` -> Trả về mã trạng thái `200 OK`.
     - Lần 2: `POST http://localhost:5000/api/submissions` -> Trả về mã trạng thái `201 Created`.
  2. *Client có tham chiếu (dùng chung) code của API không? Hai bên "thống nhất" với nhau bằng gì?*
     -> Client hoàn toàn **không tham chiếu mã nguồn** của API. Hai bên độc lập tuyệt đối và thống nhất với nhau thông qua **Hợp đồng dữ liệu (Data Contract)** dưới dạng JSON qua giao thức mạng HTTP.
  3. *Vì sao địa chỉ API được truyền qua tham số `-- http://localhost:5000` mà không viết cứng trong code?*
     -> Để đảm bảo tính linh hoạt (loose coupling), cho phép client trỏ đến các môi trường khác nhau (như localhost, máy chủ lab, cloud production) qua cấu hình lúc khởi chạy mà không cần sửa mã nguồn hoặc biên dịch lại.

## 5. Thí nghiệm lỗi (Bước 10)
| # | Thao tác | Dự đoán | Thực tế | Đúng/Sai | Giải thích |
|---|----------|---------|---------|----------|------------|
| a | Thêm bài tập thứ 3 vào API | Client in thêm bài 3 mà không cần sửa code | Client in đủ 3 bài tập | Đúng | Client phân tích cú pháp mảng JSON động và duyệt bằng vòng lặp, không cố định số lượng phần tử. |
| b | Tắt API rồi chạy client | Báo lỗi kết nối không thể thiết lập | `HttpRequestException: No connection could be made because the target machine actively refused it` | Đúng | Giao tiếp mạng phụ thuộc tính sẵn sàng của máy chủ đích; khi máy chủ tắt, cổng kết nối TCP bị từ chối. Gọi hàm trong cùng tiến trình thì không gặp lỗi này. |
| c | Đổi `StudentCode: ""` | Máy chủ từ chối và báo lỗi 400 | `HttpRequestException: Response status code does not indicate success: 400 (Bad Request)` | Đúng | API tự xác thực dữ liệu đầu vào theo nguyên tắc phòng vệ (zero-trust), `EnsureSuccessStatusCode()` trên client ném lỗi khi gặp mã 400. |
| d | Chạy API cổng 5050, Client gọi 5000 | Lỗi từ chối kết nối nếu để nguyên; chạy được khi truyền URL 5050 vào tham số | Lỗi nếu không truyền tham số; chạy thành công khi gõ lệnh kèm `http://localhost:5050` | Đúng | Client đọc URL từ `args[0]`, cho phép thay đổi địa chỉ kết nối tại thời điểm chạy mà không cần sửa code. |

## 6. Dự án EduSubmit tự làm (Bước 11-12)
- **Kiến trúc hệ thống**:
  - `Submission.Api`: Xây dựng bằng ASP.NET Core Minimal API, chạy độc lập tại cổng 5000.
  - `Submission.Client`: Ứng dụng Console .NET, giao tiếp với API qua HttpClient.
- **Luồng thông điệp trao đổi**:
  - `Client --(HTTP GET /api/assignments)--> Api` : Request-Response (Lấy danh sách bài tập)
  - `Client --(HTTP POST /api/submissions)--> Api` : Request-Response (Gửi bài nộp, nhận mã GUID và trạng thái `received`)
  - `Client --(HTTP GET /api/submissions/{id})--> Api` : Request-Response (Bước 12 - Tra cứu lại lượt nộp vừa tạo)
- **Bài mở rộng (Bước 12)**:
  - API tạo `submissions = new List<SubmissionResult>()` lưu trữ trong bộ nhớ và thêm endpoint `GET /api/submissions/{id:guid}` trả `200 OK` (kèm dữ liệu) hoặc `404 Not Found`.
  - Client sau khi POST nộp bài lập tức gọi `GET /api/submissions/{result.Id}` để xác minh dữ liệu đã ghi nhận trên hệ thống.
  - File `requests.http` có đủ 6 trường hợp kiểm thử (200, 201, 400, 404).

## 7. Khó khăn gặp phải và cách giải quyết
- Đảm bảo tính độc lập giữa Client và API: Tự khai báo các `record` riêng biệt ở phía Client theo đúng định dạng JSON mà không tham chiếu project API.
- Cấu hình địa chỉ động: Xử lý đọc URL từ `args[0]` và gán giá trị mặc định `http://localhost:5000` an toàn.
- Kiểm thử toàn diện: Sử dụng REST Client với file `requests.http` để kiểm tra cả kịch bản hợp lệ và các mã lỗi biên (400, 404).
