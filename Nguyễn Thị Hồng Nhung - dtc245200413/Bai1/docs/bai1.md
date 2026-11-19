
# Bài 1 – Tổng quan hệ thống và MEP

Họ tên: Nguyễn Thị Hồng Nhung
MSSV: DTC245200413
Lớp: CNTT K23E

## 3. BasicWebApi – Bước 8.4

### Câu 1
builder.Services.AddSingleton(...) phải đứng trước
builder.Build() vì cần đăng ký dịch vụ trước
khi xây dựng ứng dụng.

### Câu 2
name lấy từ route trong URL.
language lấy từ query string.

### Câu 3
POST hợp lệ trả 201 Created vì đã tạo
tài nguyên mới. Header Location cho biết
đường dẫn tài nguyên vừa tạo.
