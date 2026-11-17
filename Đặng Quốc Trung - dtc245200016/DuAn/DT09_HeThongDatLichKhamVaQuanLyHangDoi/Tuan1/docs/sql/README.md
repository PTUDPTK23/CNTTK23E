# SQL script để review

Thư mục này chứa SQL do EF Core sinh và truy vấn xác minh read-only.

- `InitialCreate.sql`: script idempotent sinh từ Migration `InitialCreate`.
- `VerifyDatabase.sql`: chỉ đọc system catalog/lịch sử Migration và dừng nếu query window không ở `MediQueue`.
- `InitialCreate` đã áp dụng bằng EF Core CLI ngày 18/11/2026; không chạy lại `InitialCreate.sql` trên database này.

Trong SSMS, kết nối `LAPTOP-AQFGUM9Q\SQLEXPRESS`, chọn database `MediQueue`, mở `VerifyDatabase.sql` và nhấn Execute. Kết quả đầu phải là `ValidationStatus = OK`; kiểm tra summary 22/21 bảng và dòng filtered index cuối script. Chụp ảnh Object Explorer, Migration History và result grid thật nếu cần minh chứng; không tạo ảnh giả.
