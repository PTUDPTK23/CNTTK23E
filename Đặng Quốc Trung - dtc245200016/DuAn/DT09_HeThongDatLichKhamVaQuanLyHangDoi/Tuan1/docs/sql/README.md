# SQL script để review

Thư mục này chứa SQL do EF Core sinh từ Migration nhằm phục vụ review. Không thực thi các script khi chưa có phê duyệt rõ ràng.

- `InitialCreate.sql`: script idempotent sinh từ Migration `InitialCreate`.
- Script không phải bằng chứng rằng Migration đã được áp dụng vào database `MediQueue`.
