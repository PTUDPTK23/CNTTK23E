# Thiết kế cơ sở dữ liệu
Migration: `backend/library-api/src/main/resources/db/migration` (V1 schema, V2 dữ liệu mẫu).

Bảng: `users`, `categories`, `books`, `book_copies`, `reservations`, `reservation_events`, `message_outbox`, `processed_events`.

Ràng buộc chính
- `book_copies.copy_code` UNIQUE; `status` CHECK (AVAILABLE/RESERVED/BORROWED/MAINTENANCE/LOST).
- `reservations.request_id` UNIQUE; CHECK `RESERVED/BORROWED` bắt buộc có `book_copy_id`.
- **`uq_res_active_copy`**: UNIQUE(`book_copy_id`) WHERE status IN (RESERVED, BORROWED) → một bản sao không thuộc hai reservation cùng lúc.
- **`uq_res_active_user_book`**: UNIQUE(`user_id`,`book_id`) WHERE status IN (PENDING, RESERVED, BORROWED).
- Mọi FK `ON DELETE RESTRICT`. `books.isbn` UNIQUE (nhiều NULL được phép). `publication_year` 1000–2100.
- Index: books(title, author, category), copies(book_id,status), reservations(user_id,requested_at / book_id / status).
