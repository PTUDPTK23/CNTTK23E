# Nhật ký thực hành Bài 1 – Tổng quan hệ thống và MEP

## 1. Nhật ký trả lời câu hỏi MepDemo (Bước 7)
* **(1) Ở MEP 3, mỗi bài nộp sub-x được mấy worker xử lý?**
  * Mỗi bài nộp `sub-x` chỉ do **một worker duy nhất** xử lý (các worker hoạt động theo cơ chế hàng đợi, tự động phân chia công việc để không bị trùng lặp).
* **(2) Ở MEP 4, một sự kiện được gửi tới mấy dịch vụ?**
  * Một sự kiện được phát đi và gửi tới **tất cả các dịch vụ** đang đăng ký lắng nghe (mô hình phát-nhận / publish-subscribe).
* **(3) Viết 1 câu mô tả cho mỗi 5 mô hình trao đổi thông điệp (MEP):**
  * *Request–Response:* Máy khách gửi một yêu cầu và chờ đợi máy chủ phản hồi lại kết quả trực tiếp.
  * *One-Way:* Máy khách gửi thông điệp đi một chiều mà không cần chờ đợi máy chủ phản hồi hay xác nhận.
  * *Queue-based (Hàng đợi):* Thông điệp được đưa vào hàng đợi trung gian và được phân phối lần lượt cho các tiến trình xử lý (worker) độc lập.
  * *Publish–Subscribe:* Sự kiện được phát hành từ một nguồn và được truyền quảng bá tới toàn bộ các bên đăng ký nhận thông tin.
  * *Streaming:* Dữ liệu được truyền tải liên tục thành từng luồng (stream) theo thời gian thực thay vì đợi gom toàn bộ dữ liệu rồi mới gửi.

---

## 2. Nhật ký trả lời câu hỏi BasicWebApi (Bước 8.4)
* **Câu hỏi 1:** Hai dòng `builder.Services.AddSingleton…` và `var app = builder.Build();` – dòng nào phải đứng trước? Vì sao?
  * **Trả lời:** Dòng đăng ký dịch vụ (`builder.Services...`) **phải đứng trước** dòng `var app = builder.Build();`. **Vì** phương thức `Build()` sẽ khóa (finalize) danh sách các dịch vụ trong IoC Container để khởi tạo ứng dụng, sau thời điểm này chúng ta không thể đăng ký thêm dịch vụ mới vào container được nữa.
* **Câu hỏi 2:** Trong `/hello/{name}?language=en`, tham số `name` lấy từ đâu, `language` lấy từ đâu?
  * **Trả lời:** 
    * Tham số `name` được lấy từ **đường dẫn (Route / Path)** của URL.
    * Tham số `language` được lấy từ **chuỗi truy vấn (Query String)** trên URL (phần nằm sau dấu hỏi chấm `?`).
* **Câu hỏi 3:** Vì sao POST hợp lệ trả `Results.Created` (201) chứ không phải `Results.Ok` (200)?
  * **Trả lời:** Theo chuẩn RESTful API, mã trạng thái `201 Created` biểu thị rằng yêu cầu POST đã thành công và dẫn đến việc **tạo mới một tài nguyên trên hệ thống**, đồng thời thường đi kèm với header `Location` chỉ dẫn đường dẫn tới tài nguyên vừa tạo đó.

---

## 3. Nhật ký trả lời câu hỏi MiniDistributedSystem (Bước 9)
* **Câu hỏi 1:** Client gọi những endpoint nào của API? Mỗi lần gọi là một cặp request–response gồm method, URL, mã trạng thái gì?
  * **Lần 1 (Lấy danh sách bài tập):** Method `GET`, URL `/api/assignments`, mã trạng thái `200 OK`.
  * **Lần 2 (Gửi bài nộp):** Method `POST`, URL `/api/submissions`, mã trạng thái `201 Created` (kèm dữ liệu chi tiết lượt nộp).
* **Câu hỏi 2:** Client có tham chiếu (dùng chung) code của API không? Hai bên "thống nhất" với nhau bằng gì?
  * **Không dùng chung code:** Client và API là hai tiến trình hoàn toàn độc lập, client tự khai báo lại các cấu trúc bản ghi (`record`) của riêng mình.
  * **Điểm thống nhất:** Hai bên thống nhất với nhau thông qua **giao thức HTTP**, **cấu trúc đường dẫn (endpoints)** và **định dạng dữ liệu JSON** khi truyền tải qua mạng.
* **Câu hỏi 3:** Vì sao địa chỉ API được truyền qua tham số `-- http://localhost:5000` mà không viết cứng trong code?
  * **Trả lời:** Giúp ứng dụng đạt tính linh hoạt cao (configurable). Cho phép dễ dàng thay đổi địa chỉ máy chủ khi chuyển đổi môi trường (như từ máy cá nhân sang Docker Container hoặc Cloud Server thực tế) mà không cần phải biên dịch lại mã nguồn chương trình.

---

## 4. Bảng tổng hợp Thí nghiệm lỗi (Bước 10)
| # | Dự đoán | Kết quả thực tế (Mã lỗi / Thông báo) | Đúng/Sai | Giải thích |
|---|---|---|---|---|
| **a** | Client không cần sửa code vì API chỉ cung cấp thêm dữ liệu. | `200 OK`, client hiển thị thêm bài tập mới tự động. | Đúng | Client nhận mảng JSON động và duyệt qua danh sách, khônghardcode số lượng phần tử cố định. |
| **b** | Client sẽ báo lỗi kết nối vì không tìm thấy máy chủ. | `HttpRequestException: No connection could be made because the target machine actively refused it`. | Đúng | Gọi qua mạng phụ thuộc vào tiến trình server đang chạy; gọi hàm cục bộ nằm trong cùng không gian bộ nhớ của một tiến trình. |
| **c** | API trả về lỗi Bad Request do vi phạm ràng buộc dữ liệu. | `400 Bad Request`. `EnsureSuccessStatusCode()` ném ngoại lệ (`HttpRequestException`). | Đúng | Server phải tự kiểm tra dữ liệu để đảm bảo tính toàn vẹn nghiệp vụ vì dữ liệu từ client gửi qua mạng hoàn toàn có thể bị thay đổi hoặc giả mạo. |
| **d** | Không sửa code được nếu viết cứng, nhưng có thể truyền tham số cổng khác qua args. | Chạy thành công khi truyền tham số địa chỉ mới qua dòng lệnh mà không phải sửa code. | Đúng | Địa chỉ API được truyền linh hoạt qua tham số dòng lệnh (`args[0]`), giúp tái sử dụng mã nguồn cho mọi cổng mạng. |

---

## 5. Nhật ký trả lời Câu hỏi tự kiểm tra (Phụ lục 4)
1. **Ứng dụng phân tán khác ứng dụng nguyên khối (monolithic) ở điểm nào? Trong Bài 1, ranh giới giữa các thành phần nằm ở đâu?**
   * *Khác biệt:* Ứng dụng nguyên khối gộp chung mọi logic vào một chương trình chạy trong một tiến trình duy nhất; ứng dụng phân tán gồm nhiều tiến trình độc lập giao tiếp với nhau qua mạng.
   * *Ranh giới:* Nằm ở giao diện mạng thông qua giao thức HTTP giữa `Submission.Api` (tiến trình 1) và `Submission.Client` (tiến trình 2).
2. **Kể tên 5 mô hình trao đổi thông điệp (MEP). EduSubmit ở Bài 1 dùng mô hình nào?**
   * *5 MEP:* Request–Response, One-Way, Queue-based, Publish–Subscribe, Streaming.
   * *EduSubmit ở Bài 1:* Sử dụng mô hình **Request–Response**.
3. **Phân biệt mô hình hàng đợi (queue) và publish–subscribe. Cho mỗi loại một ví dụ thực tế.**
   * *Queue (Hàng đợi):* Mỗi thông điệp chỉ được xử lý bởi **đúng một** người nhận (ví dụ: hệ thống phân chia đơn hàng cho các nhân viên giao hàng).
   * *Publish–Subscribe:* Một thông điệp phát đi được gửi tới **tất cả** các bên đăng ký nhận (ví dụ: hệ thống thông báo đẩy, bảng tin tin tức thời gian thực).
4. **Mã 200, 201, 400, 404 có ý nghĩa gì? Khi nào API nên trả mỗi mã?**
   * `200 OK`: Thành công (dùng cho GET lấy dữ liệu).
   * `201 Created`: Tạo mới thành công (dùng cho POST tạo tài nguyên).
   * `400 Bad Request`: Lỗi dữ liệu do phía client gửi lên không hợp lệ.
   * `404 Not Found`: Không tìm thấy tài nguyên theo đường dẫn hoặc định danh yêu cầu.
5. **Vì sao máy khách luôn phải kiểm tra mã trạng thái trước khi đọc dữ liệu phản hồi?**
   * Vì khi yêu cầu gặp lỗi (mã 4xx, 5xx), máy chủ thường trả về nội dung lỗi hoặc thông báo dạng khác thay vì cấu trúc dữ liệu JSON chuẩn mà client mong đợi. Việc kiểm tra giúp tránh ứng dụng bị sập (crash) do không map được dữ liệu bất thường.