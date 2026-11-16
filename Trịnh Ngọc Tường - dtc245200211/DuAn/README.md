# HỆ THỐNG NỘP VÀ CHẤM BÀI TỰ ĐỘNG PHÂN TÁN (DISTRIBUTED AUTOGRADER)

**Học phần:** Phát triển ứng dụng phân tán  
**Sinh viên thực hiện:** Trịnh Ngọc Tường  
**Mã sinh viên:** DTC245200211  
**Mục tiêu:** Xây dựng hệ thống nộp và chấm mã nguồn tự động theo kiến trúc phân tán phục vụ 9 tuần thực hành học phần.

---

## 🌟 ĐẶC TRƯNG HỆ THỐNG PHÂN TÁN
1. **Kiến trúc Dịch vụ Phân tán (Microservices Architecture):** Tách biệt API Gateway, Core Service, Message Broker, Worker Nodes, Database và Storage.
2. **Xử lý Bất đồng bộ với Message Queue:** Sử dụng RabbitMQ điều phối bài nộp, chống nghẽn nghẽn và đảm bảo không mất mát dữ liệu khi sinh viên nộp bài dồn dập.
3. **Mạng lưới Worker Chấm bài Độc lập (Distributed Grader Nodes):** Hỗ trợ mở rộng theo chiều ngang (Scale Out), nhiều Worker có thể chạy trên nhiều máy chủ khác nhau để cùng chia sẻ tải chấm bài.
4. **Cách ly An toàn (Docker Sandbox):** Mỗi bài nộp của sinh viên được biên dịch và chạy trong Docker Container với giới hạn CPU, RAM, thời gian thực thi (Timeout) và ngắt kết nối mạng ngoài.
5. **Cập nhật Tiến độ Realtime:** Sử dụng WebSocket để cập nhật trực tiếp trạng thái chấm (Pending -> Compiling -> Running Testcases -> Result) lên màn hình sinh viên.
6. **Khung Đề bài Thực hành 9 Tuần:** Hỗ trợ kiểm thử tự động các bài toán mạng Socket, RPC/gRPC, REST API, Message Queue, Caching & Concurrency.

---

## 📂 CẤU TRÚC THƯ MỤC DỰ ÁN

```text
DuAn/
├── README.md                      # Giới thiệu & Hướng dẫn cài đặt
├── docker-compose.yml             # Khởi chạy toàn bộ hạ tầng phân tán
├── services/
│   ├── api-gateway/               # Nginx Reverse Proxy & Load Balancer
│   ├── core-service/              # Backend API (Quản lý User, Bài tập, Nộp bài)
│   ├── grader-worker/             # Worker phân tán nhận job từ RabbitMQ, chạy Docker sandbox
│   └── web-client/                # Giao diện Web (Portal Sinh viên & Giảng viên)
├── sample-labs/                   # Bộ đề bài & test case mẫu cho 9 tuần học
│   ├── week-01-socket/
│   ├── week-02-multithreading/
│   ├── week-03-rmi-grpc/
│   ├── week-04-rest-api/
│   ├── week-05-message-broker/
│   ├── week-06-distributed-cache/
│   ├── week-07-jwt-gateway/
│   ├── week-08-load-balancer/
│   └── week-09-final-project/
└── docs/                          # Tài liệu kỹ thuật & Báo cáo đồ án
```

---

## 🚀 HƯỚNG DẪN KHỞI CHẠY HỆ THỐNG

### Yêu cầu tiên quyết:
- [Docker](https://www.docker.com/) & Docker Compose
- Node.js (>= 18) hoặc Python (>= 3.10) nếu muốn chạy dev môi trường máy local

### 1. Khởi động toàn bộ cụm phân tán bằng Docker Compose:
```bash
docker compose up -d
```

### 2. Truy cập các dịch vụ:
- **Web Portal (Sinh viên & Giảng viên):** `http://localhost:3000`
- **Backend API Gateway:** `http://localhost:8080/api`
- **RabbitMQ Management Dashboard:** `http://localhost:15672` (User: `guest`, Pass: `guest`)
- **PostgreSQL Database:** `localhost:5432` (DB: `autograder_db`)

---

## 📅 LỘ TRÌNH 9 TUẦN THỰC HIỆN
- **Tuần 1:** Phân tích yêu cầu, đặc tả kiến trúc phân tán & sơ đồ khối.
- **Tuần 2:** Thiết kế CSDL PostgreSQL, chuẩn hóa REST API & setup môi trường Docker.
- **Tuần 3:** Xây dựng Core API Service (Auth JWT, CRUD môn học, bài tập, upload code).
- **Tuần 4:** Tích hợp RabbitMQ Task Queue cho hàng đợi bài nộp.
- **Tuần 5:** Xây dựng Grader Worker Node với Docker Sandbox cô lập tài nguyên.
- **Tuần 6:** Tích hợp bộ đánh giá bài tập phân tán (Socket/Client-Server) & WebSocket Realtime.
- **Tuần 7:** Phát triển Web Frontend UI cho Sinh viên và Giảng viên.
- **Tuần 8:** Kiểm thử chịu tải (Stress testing) khi nhiều bài nộp đồng thời & tối ưu scale worker.
- **Tuần 9:** Đóng gói hệ thống, hoàn thiện báo cáo và slide thuyết trình nghiệm thu.
