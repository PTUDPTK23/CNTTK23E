# BÀI THỰC HÀNH 2: RESTful API CHO TƯƠNG TÁC CLIENT–SERVER

\+ Sinh viên: Phạm Long Vũ  
\+ Mã sinh viên: dtc241200004  
\+ Lớp: CNTT K23E  
\+ Môn học: Phát triển ứng dụng phân tán  

---

## 1. Cấu trúc bài nộp

Thư mục bài làm bao gồm 2 dự án phân tán:

1. CatalogAPI:
   \+ `src/Catalog.Api`: Máy chủ Web API quản lý sản phẩm (chạy ở cổng `http://localhost:5100`).  
   \+ `src/Catalog.Client`: Máy khách Console gọi API qua 4 nấc bằng `HttpClient`.  
   \+ `requests.http`: Bộ kiểm thử 8 ca với REST Client.  

2. EduSubmit:
   \+ `src/Submission.Api`: Máy chủ Web API quản lý bài tập và lượt nộp bài (chạy ở cổng `http://localhost:5000`).  
   \+ `src/Submission.Client`: Máy khách Console thực hiện chuỗi 4 bước (xem bài mở, nộp bài, đọc lại lượt nộp, bắt lỗi nghiệp vụ).  
   \+ `requests.http`: Bộ kiểm thử các ca nộp bài với REST Client.  

---

## 2. Bảng phá thử nghiệm hệ phân tán (Bước 9 - Slide 60)

| Thí nghiệm | Thao tác thực hiện | Hiện tượng quan sát | Bài học rút ra |
| :---: | :--- | :--- | :--- |
| TN 1 | Chạy máy khách lần 2 (khi máy chủ vẫn đang mở) | Danh sách ban đầu hiển thị sản phẩm vừa tạo ở lần 1; sản phẩm mới tạo ra có `Id = [5]` | Trạng thái nằm ở máy chủ, không nằm ở máy khách. |
| TN 2 | Bấm `Ctrl + C` tắt máy chủ rồi chạy lại máy khách | Máy khách sập với ngoại lệ `HttpRequestException: Connection refused (localhost:5100)` | Lỗi đặc thù của hệ phân tán: Tiến trình phía bên kia có thể vắng mặt / ngừng hoạt động. |
| TN 3 | Khởi động lại máy chủ và lấy danh sách sản phẩm | Danh sách quay trở lại đúng 3 sản phẩm mẫu ban đầu, các sản phẩm mới tạo bị mất | Dữ liệu trong RAM tiến trình sẽ mất khi tiến trình dừng. |
| TN 4 | Chạy máy khách trỏ vào cổng sai (`http://localhost:5999`) | Báo lỗi `Connection refused (localhost:5999)` | Hai bên bắt buộc phải thống nhất chính xác địa chỉ mạng (URL/cổng). |

---

## 3. Trả lời 3 câu hỏi giải thích (Slide 90)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?

\+ Trả lời:  
Trong kiến trúc REST (Representational State Transfer), URI được thiết kế để định danh tài nguyên (Resource) – là các "thực thể/danh từ" trong bài toán nghiệp vụ (ví dụ: `/api/products`, `/api/assignments`).

Các hành động thao tác với tài nguyên đó đã được chuẩn hóa bởi giao thức HTTP thông qua các phương thức chuẩn (HTTP Verbs):  
\+ `GET`: Lấy biểu diễn tài nguyên (chỉ đọc, an toàn, lũy đẳng).  
\+ `POST`: Tạo mới tài nguyên con.  
\+ `PUT`: Thay thế toàn bộ trạng thái tài nguyên (lũy đẳng).  
\+ `DELETE`: Xóa tài nguyên (lũy đẳng).  

Việc tách biệt này giúp API có giao diện thống nhất (Uniform Interface), client chỉ cần nhìn URI và Method là đoán đúng cách dùng, tránh việc tự chế ra các URI chứa động từ tùy tiện và lộn xộn như `/getProducts`, `/doSubmit`, `/createProduct`.

---

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?

\+ Trả lời:  
Tính chất vô trạng thái (Stateless) thể hiện ở chỗ: Máy chủ không lưu giữ bất kỳ phiên làm việc (Session) hay ngữ cảnh hội thoại nào của máy khách giữa hai yêu cầu liên tiếp.

Mỗi yêu cầu HTTP gửi từ Client lên Server đều phải mang đầy đủ toàn bộ thông tin cần thiết để Server tự hiểu và xử lý độc lập:  
\+ Khi nộp bài: `POST /api/assignments/{assignmentId}/submissions`, Client truyền đủ mã bài tập `{assignmentId}` trên URI và body JSON chứa `studentCode`, `repositoryUrl`. Server không cần biết trước đó Client đã xem trang nào.  
\+ Server kiểm tra, xử lý và trả về phản hồi (`201 Created` kèm Location hoặc mã lỗi `400/404`) là kết thúc một chu trình độc lập. Nhờ đó, nếu có nhiều máy chủ đặt sau bộ cân bằng tải (Load Balancer), bất kỳ máy chủ nào nhận yêu cầu cũng đều xử lý được như nhau.

---

### Câu 3: Vì sao Client và Server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?

\+ Trả lời:  
Client và Server là hai chương trình chạy ở hai tiến trình riêng biệt, thậm chí ở hai máy tính khác nhau, không dùng chung bộ nhớ và không chia sẻ mã nguồn trực tiếp (không tham chiếu DLL/Project của nhau). Phương tiện duy nhất để hai bên hiểu nhau là thông điệp gửi qua mạng (chuỗi byte JSON theo chuẩn HTTP).

DTO (Data Transfer Object) ở mỗi bên đóng vai trò là một bản hợp đồng dữ liệu:  
\+ Phía Server: DTO được tuần tự hóa (Serialize) thành chuỗi JSON dạng camelCase (ví dụ: `id`, `name`, `studentCode`).  
\+ Phía Client: Nhận chuỗi JSON và giải tuần tự hóa (Deserialize) ngược lại vào DTO của Client.  

Miễn là tên và kiểu dữ liệu của các trường trong DTO ở Client khớp với cấu trúc JSON của Server, thì hai bên hoàn toàn khớp nhau về mặt dữ liệu mà không cần phải dùng chung code C#.

Client và Server là hai chương trình chạy ở hai tiến trình riêng biệt, thậm chí ở hai máy tính khác nhau, không dùng chung bộ nhớ và không chia sẻ mã nguồn trực tiếp (không tham chiếu DLL/Project của nhau). Phương tiện duy nhất để hai bên hiểu nhau là thông điệp gửi qua mạng (chuỗi byte JSON theo chuẩn HTTP).

DTO (Data Transfer Object) ở mỗi bên đóng vai trò là một bản hợp đồng dữ liệu:  
\+ Phía Server: DTO được tuần tự hóa (Serialize) thành chuỗi JSON dạng camelCase (ví dụ: `id`, `name`, `studentCode`).  
\+ Phía Client: Nhận chuỗi JSON và giải tuần tự hóa (Deserialize) ngược lại vào DTO của Client.  

Miễn là tên và kiểu dữ liệu của các trường trong DTO ở Client khớp với cấu trúc JSON của Server, thì hai bên hoàn toàn khớp nhau về mặt dữ liệu mà không cần phải dùng chung code C#.
