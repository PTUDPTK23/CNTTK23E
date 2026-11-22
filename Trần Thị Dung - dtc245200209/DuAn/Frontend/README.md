# Frontend - Giao diện Hệ thống gửi thông báo hàng loạt

Giao diện quản trị và theo dõi hệ thống gửi thông báo hàng loạt theo kiến trúc phân tán.

## Công nghệ sử dụng
- HTML5, CSS3 hiện đại, Vanilla JavaScript (không phụ thuộc framework nặng nề, tốc độ tải tức thì).
- Responsive thiết kế linh hoạt cho cả máy tính và thiết bị di động.

## Cấu trúc thư mục
- `index.html`: Bảng điều khiển chính (Dashboard), hiển thị Sidebar, Thống kê KPI, Bảng chiến dịch mẫu và Widget kiểm tra trực tiếp trạng thái Backend Express API (`/api/health`).
- `login.html`: Giao diện đăng nhập giả lập (Mock UI), lưu session người dùng vào `localStorage`.
- `css/style.css`: Toàn bộ định dạng giao diện, hiệu ứng và layout.
- `js/auth.js`: Module xác thực mô phỏng, auth guards và session manager.
- `js/app.js`: Xử lý giao diện Dashboard, tương tác và gọi API health check.

## Cách mở và chạy
Bạn có thể mở giao diện bằng bất kỳ cách nào sau đây:
1. **Cách đơn giản nhất:** Nhấp đúp chuột trực tiếp vào file `Frontend/login.html` hoặc `Frontend/index.html` để mở trong trình duyệt Chrome/Edge/Firefox.
2. **Sử dụng VS Code Live Server:** Mở thư mục dự án trong VS Code, nhấp chuột phải vào `Frontend/login.html` và chọn **Open with Live Server**.
3. **Sử dụng lệnh npm / npx:**
   ```bash
   npx serve Frontend
   ```
