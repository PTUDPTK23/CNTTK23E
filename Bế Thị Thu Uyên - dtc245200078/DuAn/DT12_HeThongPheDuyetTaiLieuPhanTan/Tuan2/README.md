# ĐỀ TÀI 12: HỆ THỐNG PHÊ DUYỆT TÀI LIỆU PHÂN TÁN (DOCUMENT APPROVAL SYSTEM)

## 1. Thông tin sinh viên thực hiện
- **Họ và tên:** Bế Thị Thu Uyên
- **Mã sinh viên:** Dtc245200078
- **Lớp:** CNTTK23E

---

## 2. Tổng quan & Kiến trúc hệ thống
Hệ thống được xây dựng theo kiến trúc hướng sự kiện (Event-Driven Architecture) trên nền tảng **.NET 10**, bao gồm 4 dịch vụ độc lập giao tiếp qua mạng nội bộ Docker:
1. **DocumentApi (.NET 10 Minimal API):** Cung cấp bộ RESTful API quản lý vòng đời tài liệu (`Draft` -> `Submitted` -> `Approved` / `Rejected`), lưu trữ dữ liệu & lịch sử xử lý vào PostgreSQL và phát sự kiện trạng thái lên RabbitMQ.
2. **DocumentWorker (.NET 10 Background Service):** Dịch vụ chạy nền lắng nghe bất đồng bộ các sự kiện từ hàng đợi `document_events` trên RabbitMQ để xử lý thông báo theo thời gian thực.
3. **PostgreSQL 15:** Hệ quản trị cơ sở dữ liệu lưu trữ thông tin tài liệu (`Documents`) và lịch sử thay đổi trạng thái (`DocumentHistories`).
4. **RabbitMQ 3:** Message Broker trung gian đảm bảo truyền tải thông điệp bất đồng bộ, tách biệt giữa dịch vụ xử lý nghiệp vụ và dịch vụ thông báo.

---

## 3. Hướng dẫn cài đặt và khởi chạy

### Cách 1: Chạy toàn bộ hệ thống bằng Docker Compose (Khuyên dùng)
**Yêu cầu:** Máy đã cài đặt Docker Desktop.

1. Mở Terminal tại thư mục chứa file `docker-compose.yml` và chạy lệnh:
   ```bash
   docker-compose up -d --build