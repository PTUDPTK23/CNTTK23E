# HỆ THỐNG GỬI THÔNG BÁO HÀNG LOẠT (MASS NOTIFICATION SYSTEM)
> **Môn học:** Phát triển ứng dụng phân tán  
> **Sinh viên:** Trần Thị Dung  
> **Mã sinh viên:** DTC245200209  
> **Giai đoạn:** Tuần 1 – Khởi tạo dự án và phân tích  

---

## 📌 Giới thiệu dự án
Hệ thống gửi thông báo hàng loạt được thiết kế và xây dựng theo **kiến trúc phân tán (Distributed Architecture)**, giải quyết triệt để bài toán nghẽn cổ chai khi gửi email số lượng lớn. Hệ thống tách bạch giữa tầng tiếp nhận yêu cầu (Web API - Producer), tầng hàng đợi tin nhắn (Message Broker) và tầng xử lý nghiệp vụ gửi thư thực tế (Multiple Independent Workers).

Toàn bộ hệ thống được cài đặt và vận hành trực tiếp trên hệ điều hành **Windows**, **không sử dụng Docker** theo đúng yêu cầu học phần.

---

## 🏗️ Kiến trúc hệ thống và Luồng gửi thông báo

```
+------------------+         +-------------------------------+
|   Frontend UI    | ------> |  Backend Express API Producer |
| (HTML/CSS/JS)    |         | (Lưu MySQL, sinh Tasks)       |
+------------------+         +-------------------------------+
                                             │
                                             ▼ Push tasks
                             +-------------------------------+
                             |    RabbitMQ Message Broker    |
                             |    (durable message queue)    |
                             +-------------------------------+
                                      │             │
                    Prefetch fair dispatch          Prefetch fair dispatch
                                      ▼             ▼
                             +----------------+  +----------------+
                             | Worker Node 1  |  | Worker Node 2  |
                             | (Independent)  |  | (Independent)  |
                             +----------------+  +----------------+
                                      │                  │
                                      ▼ Send Email       ▼ Send Email
                                 [SMTP Server]      [SMTP Server]
                                      │                  │
                                      ▼ Update status    ▼ Update status
                             +-------------------------------+
                             |        MySQL Database         |
                             | (Lịch sử, trạng thái, người)  |
                             +-------------------------------+
```

### Vai trò người dùng:
1. **Quản trị viên (Admin):** Cấu hình kết nối hệ thống (Database, RabbitMQ, SMTP), quản lý tài khoản, giám sát hàng đợi và xem báo cáo tổng thể.
2. **Nhân viên (Staff):** Quản lý danh bạ người nhận, nhóm người nhận, tạo và khởi chạy chiến dịch gửi thông báo, theo dõi tiến độ gửi.

---

## 📁 Cấu trúc thư mục dự án

```text
DuAn/
├── Backend/                    # Máy chủ API Node.js & Express
│   ├── src/
│   │   ├── config/             # Cấu hình biến môi trường
│   │   │   └── index.js
│   │   ├── controllers/        # Xử lý logic nghiệp vụ
│   │   │   └── health.controller.js
│   │   ├── routes/             # Định tuyến API endpoints
│   │   │   ├── health.route.js
│   │   │   └── index.js
│   │   ├── app.js              # Khởi tạo Express app & middlewares
│   │   └── server.js           # Khởi chạy HTTP Server
│   ├── .env                    # Biến môi trường cục bộ (không commit)
│   ├── .env.example            # Mẫu cấu hình môi trường
│   ├── package.json            # Khai báo dependencies
│   └── README.md
├── Frontend/                   # Giao diện quản trị & theo dõi
│   ├── css/
│   │   └── style.css           # Bảng định dạng giao diện Dashboard & Login
│   ├── js/
│   │   ├── auth.js             # Module xác thực giả lập & Auth Guards
│   │   └── app.js              # Xử lý giao diện & gọi API health check
│   ├── index.html              # Bảng điều khiển chính (Dashboard, Sidebar, Live Health Box)
│   ├── login.html              # Trang đăng nhập giả lập (Mock UI)
│   └── README.md
├── docs/                       # Tài liệu phân tích và thiết kế
│   └── phan-tich-yeu-cau.md   # Phân tích chi tiết chức năng, vai trò, luồng phân tán
├── .gitignore                  # Bỏ qua node_modules, .env, file rác
└── README.md                   # Tài liệu hướng dẫn tổng quan dự án
```

---

## 🚀 Hướng dẫn cài đặt và chạy trên Windows

### 1. Khởi chạy Backend Express API

Yêu cầu máy tính đã cài đặt **Node.js** (>= 18.x).

1. Mở cửa sổ dòng lệnh (Terminal / PowerShell / CMD):
   ```bash
   cd Backend
   ```
2. Cài đặt các gói phụ thuộc (Dependencies):
   ```bash
   npm install
   ```
3. Chuẩn bị file `.env`:
   ```bash
   copy .env.example .env
   ```
4. Khởi động máy chủ:
   - Cách 1: Chế độ phát triển (tự reload khi sửa code):
     ```bash
     npm run dev
     ```
   - Cách 2: Chế độ thông thường:
     ```bash
     npm start
     ```
5. Khi khởi động thành công, màn hình sẽ thông báo:
   ```text
   ====================================================
   🚀 Mass Notification System - Backend API
   📡 Server đang chạy tại: http://localhost:5000
   🩺 Health check endpoint: http://localhost:5000/api/health
   🌍 Môi trường: development
   ====================================================
   ```

### 2. Kiểm tra Endpoint Health Check
- Mở trình duyệt web hoặc dùng cURL/Postman:
  ```text
  GET http://localhost:5000/api/health
  ```
- Dữ liệu JSON phản hồi chuẩn HTTP 200 OK:
  ```json
  {
    "status": "healthy",
    "message": "Mass Notification System API is up and running",
    "timestamp": "2026-11-17T13:40:00.000Z",
    "uptime": "12s",
    "environment": "development",
    "version": "1.0.0",
    "phase": "Week 1 - Project Initialization & Architecture Design",
    "services": {
      "api": "online",
      "database": "pending_week2",
      "rabbitmq": "pending_week4",
      "workers": "pending_week5"
    }
  }
  ```

### 3. Mở và sử dụng Giao diện Frontend
1. **Cách 1 (Nhanh nhất):** Nhấp đúp chuột trực tiếp vào file `Frontend/login.html` để mở trong trình duyệt Chrome/Edge.
2. **Cách 2 (VS Code Live Server):** Chuột phải vào `Frontend/login.html` chọn **Open with Live Server**.
3. **Cách 3 (Node static server):**
   ```bash
   npx serve Frontend
   ```
- **Tài khoản đăng nhập mô phỏng (Mock UI):**
  - **Admin:** `admin@system.local` / Mật khẩu: `123456`
  - **Staff:** `staff@system.local` / Mật khẩu: `123456`
- Khi đăng nhập vào Dashboard, hệ thống Frontend sẽ **tự động gọi đến `http://localhost:5000/api/health`** để hiển thị trạng thái kết nối máy chủ theo thời gian thực (Trực tuyến / Ngoại tuyến).

---

## 🎯 Nghiệm thu kết quả Tuần 1

- [x] **Phân tích bài toán:** Chốt đầy đủ yêu cầu, chức năng, vai trò người dùng và luồng gửi thông báo phân tán trong `docs/phan-tich-yeu-cau.md`.
- [x] **Cấu trúc thư mục:** Chuẩn chỉ, chuyên nghiệp, phân tách `Backend/`, `Frontend/`, `docs/`, có `.gitignore` và `README.md`.
- [x] **Backend API:** Khởi tạo thành công với Express.js, lắng nghe cổng 5000, cấu hình CORS, có route `/api/health` trả về JSON trạng thái.
- [x] **Frontend:** Hoàn thiện giao diện đăng nhập giả lập (`login.html`), Dashboard tổng quan có Sidebar, Thống kê, Bảng chiến dịch mẫu và Widget kiểm tra trực tiếp trạng thái Backend.
- [x] **Kiểm thử tích hợp:** Frontend tự động kết nối Backend `/api/health` và cập nhật giao diện trực tuyến.
- [x] **Git commit:** Đã tạo commit `week-01-init-project`.
