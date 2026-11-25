# Backend API - Hệ thống gửi thông báo hàng loạt

Phần Backend phục vụ REST API cho hệ thống phân tán gửi thông báo hàng loạt.

## Yêu cầu môi trường
- Node.js >= 18.x (khuyến nghị >= 20.x, đang chạy Node 24)
- npm >= 9.x

## Hướng dẫn cài đặt và chạy
1. Di chuyển vào thư mục Backend:
   ```bash
   cd Backend
   ```
2. Cài đặt các gói phụ thuộc:
   ```bash
   npm install
   ```
3. Tạo file cấu hình môi trường `.env`:
   ```bash
   copy .env.example .env
   ```
4. Khởi chạy máy chủ:
   - Chế độ phát triển (auto reload):
     ```bash
     npm run dev
     ```
   - Chế độ chuẩn:
     ```bash
     npm start
     ```

## Endpoint kiểm tra (Week 1)
- **URL:** `http://localhost:5000/api/health`
- **Phương thức:** `GET`
- **Mẫu dữ liệu trả về:**
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
