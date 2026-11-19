# SQL script để review

Các script được kế thừa nguyên trạng từ Tuần 1. `MediQueue` là database duy nhất xuyên suốt 9 tuần. Prompt 03.2 không thực thi script và không thay đổi schema.

Thư mục này chứa SQL do EF Core sinh và truy vấn xác minh read-only.

- `InitialCreate.sql`: script idempotent sinh từ Migration `InitialCreate`.
- `VerifyDatabase.sql`: chỉ đọc system catalog/lịch sử Migration và dừng nếu query window không ở `MediQueue`.
- `InitialCreate` đã áp dụng bằng EF Core CLI ngày 18/11/2026; không chạy lại `InitialCreate.sql` trên database này.

Trong SSMS, kết nối `LAPTOP-AQFGUM9Q\SQLEXPRESS`, chọn database `MediQueue`, mở `VerifyDatabase.sql` và nhấn Execute. Kết quả đầu phải là `ValidationStatus = OK`; kiểm tra summary 22/21 bảng và dòng filtered index cuối script. Chụp ảnh Object Explorer, Migration History và result grid thật nếu cần minh chứng; không tạo ảnh giả.
