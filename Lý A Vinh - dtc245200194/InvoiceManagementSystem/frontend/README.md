# Frontend

Frontend dùng HTML, CSS và JavaScript thuần. Chạy từ thư mục `frontend` bằng:

```powershell
python -m http.server 5500 --bind 127.0.0.1
```

Mở frontend bằng `http://localhost:5500/` hoặc `http://127.0.0.1:5500/` (trang quản trị: thêm `/admin/`). Đừng nhập `0.0.0.0` lên thanh địa chỉ trình duyệt; đây là địa chỉ server lắng nghe.

API gốc được tính từ hostname của trang tại `shared/js/config.js` (`localhost:5000` hoặc `127.0.0.1:5000`), khớp launch profile backend và CORS. Giữ nguyên hostname khi mở frontend để cookie quản trị được gửi đúng.
