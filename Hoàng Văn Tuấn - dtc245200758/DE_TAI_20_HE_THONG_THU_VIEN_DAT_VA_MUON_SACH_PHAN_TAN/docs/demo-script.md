# Kịch bản demo
1. Mở RabbitMQ UI + trang Quản trị > Tổng quan để thấy số liệu.
2. Chọn người dùng "Nguyễn Minh Anh" → Tra cứu "Java" → chi tiết → Đặt mượn → trang theo dõi: PENDING rồi tự chuyển RESERVED, hiện bản sao.
3. Quản trị > Yêu cầu: **Giao sách** (BORROWED) → **Nhận trả** (RETURNED). "Sách của tôi" cập nhật nhóm.
4. Hết sách: chọn "Trần Quốc Bảo" và "Lê Thu Hà" lần lượt đặt "Cấu trúc dữ liệu và giải thuật" (1 bản) → một RESERVED, một REJECTED kèm lý do.
5. Bất đồng bộ: tắt Worker, đặt sách → PENDING; bật Worker → xử lý. Xem queue trong RabbitMQ UI.
6. Đồng thời: `node scripts/concurrency-test.mjs 20 3` → PASS.
7. Lỗi: đặt trùng cùng sách → thông báo 409; cố đổi bản đang mượn sang "Sẵn sàng" → bị chặn.
