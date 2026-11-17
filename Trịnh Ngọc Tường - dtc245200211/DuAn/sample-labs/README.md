# TẬP HỢP ĐỀ BÀI VÀ TESTCASES MẪU 9 TUẦN
## HỌC PHẦN: PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN
**Sinh viên:** Trịnh Ngọc Tường - DTC245200211

---

### Tuần 1: Lập trình Mạng Socket TCP/UDP cơ bản
- **Mục tiêu:** Xây dựng Client - Server tính toán biểu thức số học qua Socket TCP.
- **Dữ liệu đầu vào (Input):** Lệnh tính toán `ADD 15 25`, `SUB 50 12`, `MUL 7 8`, `DIV 100 4`.
- **Dữ liệu mong muốn (Output):** `RESULT 40`, `RESULT 38`, `RESULT 56`, `RESULT 25`.

### Tuần 2: Concurrency & Đa luồng (Multi-threading)
- **Mục tiêu:** Xây dựng Multi-threaded Server hỗ trợ tối thiểu 50 client kết nối và truyền nhận dữ liệu đồng thời.
- **Đánh giá:** Chạy đồng thời 50 worker connection gửi request kiểm tra không bị race condition hay crash server.

### Tuần 3: Gọi hàm từ xa (Java RMI / gRPC)
- **Mục tiêu:** Định nghĩa Remote Interface (RMI) hoặc Protobuf (gRPC) cho hệ thống tra cứu điểm thi và sinh viên.
- **Đánh giá:** Stub gọi Skeleton từ xa, kiểm tra cơ chế Serialization và RemoteException.

### Tuần 4: Kiến trúc RESTful Web Services
- **Mục tiêu:** Xây dựng REST API chuẩn CRUD cho quản lý tài nguyên phân tán, xử lý chuẩn HTTP Codes (200, 201, 400, 404, 500).
- **Đánh giá:** Kiểm thử bằng HTTP request tự động, kiểm tra format JSON trả về.

### Tuần 5: Message-Oriented Middleware (RabbitMQ)
- **Mục tiêu:** Xây dựng mô hình Publish/Subscribe xử lý đơn hàng bất đồng bộ với Topic Exchange.
- **Đánh giá:** Gửi 100 sự kiện vào Broker, kiểm tra consumer nhận đủ và đúng thứ tự.

### Tuần 6: Caching & Khóa phân tán (Redis Distributed Lock)
- **Mục tiêu:** Ứng dụng Redis Cache và Distributed Lock (Redlock) giải quyết bài toán chống Overselling khi mua vé trực tuyến.
- **Đánh giá:** 10 luồng mua đồng thời chiếc vé cuối cùng, chỉ 1 luồng thành công, 9 luồng nhận thông báo hết vé.

### Tuần 7: Xác thực & Bảo mật Phân tán (JWT & API Gateway)
- **Mục tiêu:** Triển khai Single Sign-On (SSO) và xác thực tập trung bằng JSON Web Token (JWT) qua Nginx/Kong Gateway.
- **Đánh giá:** Kiểm tra tính toàn vẹn của Token (Secret signature, Expired time, Role access).

### Tuần 8: Tính chịu lỗi & Cân bằng tải (Load Balancing & Failover)
- **Mục tiêu:** Cấu hình cân bằng tải Round Robin / Least Connections cho 2 máy chủ backend, tích hợp Healthcheck.
- **Đánh giá:** Kill 1 server trong lúc đang chạy tải, hệ thống vẫn phản hồi 100% request qua server còn lại.

### Tuần 9: Đồ án Cuối kỳ - Tích hợp Hệ sinh thái Phân tán
- **Mục tiêu:** Đóng gói toàn bộ hệ thống bằng Docker Compose, chạy trơn tru trên môi trường production.
