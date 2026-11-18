-- ==============================================================================
-- ĐỀ TÀI 11: HỆ THỐNG GỬI THÔNG BÁO HÀNG LOẠT (BULK NOTIFICATION SYSTEM)
-- Sample Seed Data Script
-- ==============================================================================

-- 1. Dữ liệu Roles
INSERT INTO roles (id, name, description) VALUES
(1, 'ROLE_ADMIN', 'Quản trị viên toàn quyền hệ thống'),
(2, 'ROLE_USER', 'Người dùng vận hành thông thường')
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- 2. Dữ liệu Users (Mật khẩu mặc định: admin123 và user123)
-- Hash BCrypt được mã hóa với BCryptPasswordEncoder tiêu chuẩn
INSERT INTO users (id, username, password, full_name, email, phone, status) VALUES
(1, 'admin', '$2a$10$7R3v5K0GZ1zJ5kK5H9/f1OW2wQZ8d8f9y0x1A2b3C4d5E6f7G8h9i', 'Quản Trị Viên Hệ Thống', 'admin@notification.vn', '0901234567', 'ACTIVE'),
(2, 'user', '$2a$10$7R3v5K0GZ1zJ5kK5H9/f1OW2wQZ8d8f9y0x1A2b3C4d5E6f7G8h9i', 'Nhân Viên Vận Hành', 'user@notification.vn', '0907654321', 'ACTIVE')
ON DUPLICATE KEY UPDATE username=VALUES(username);

-- Gán quyền cho User
INSERT INTO user_roles (user_id, role_id) VALUES
(1, 1), -- admin có ROLE_ADMIN
(1, 2), -- admin có ROLE_USER
(2, 2)  -- user có ROLE_USER
ON DUPLICATE KEY UPDATE user_id=VALUES(user_id);

-- 3. Dữ liệu Recipients (Người nhận thông báo)
INSERT INTO recipients (id, name, email, phone, recipient_group, status) VALUES
(1, 'Nguyễn Văn An', 'nguyenvanan.dev@gmail.com', '0912345678', 'KHACH_HANG', 'ACTIVE'),
(2, 'Trần Thị Bình', 'tranthibinh.mkt@gmail.com', '0923456789', 'KHACH_HANG', 'ACTIVE'),
(3, 'Lê Hoàng Cường', 'cuong.lehoang@company.com', '0934567890', 'NHAN_VIEN', 'ACTIVE'),
(4, 'Phạm Thị Dung', 'dung.phamthi@company.com', '0945678901', 'NHAN_VIEN', 'ACTIVE'),
(5, 'Hoàng Minh Đức', 'duc.hoangminh@partner.vn', '0956789012', 'DOI_TAC', 'ACTIVE'),
(6, 'Vũ Hải Yến', 'yen.vuhai@partner.vn', '0967890123', 'DOI_TAC', 'ACTIVE'),
(7, 'Đỗ Mạnh Hùng', 'hung.domanh.vip@gmail.com', '0978901234', 'VIP', 'ACTIVE'),
(8, 'Ngô Mai Lan', 'lan.ngomai.vip@gmail.com', '0989012345', 'VIP', 'ACTIVE'),
(9, 'Bùi Quốc Tuấn', 'tuan.buiquoc@student.edu.vn', '0990123456', 'SINH_VIEN', 'ACTIVE'),
(10, 'Đinh Thu Hà', 'ha.dinhthu@student.edu.vn', '0901122334', 'SINH_VIEN', 'ACTIVE'),
(11, 'Lý Gia Huy', 'huy.lygia@student.edu.vn', '0911223344', 'SINH_VIEN', 'ACTIVE'),
(12, 'Trịnh Phương Thảo', 'thao.trinh@client.com', '0922334455', 'KHACH_HANG', 'ACTIVE'),
(13, 'Phan Văn Nam', 'nam.phanvan@company.com', '0933445566', 'NHAN_VIEN', 'ACTIVE'),
(14, 'Cao Thị Ngọc', 'ngoc.caothi@client.com', '0944556677', 'KHACH_HANG', 'INACTIVE'),
(15, 'Trương Văn Phát', 'phat.truongvan@partner.vn', '0955667788', 'DOI_TAC', 'ACTIVE');

-- 4. Dữ liệu Notifications (Mẫu thông báo)
INSERT INTO notifications (id, title, content, type, target_group, created_by) VALUES
(1, 'Chào mừng thành viên mới gia nhập hệ thống', 'Kính gửi {name},\n\nChúng tôi rất hân hạnh chào đón bạn đến với hệ thống thông báo tự động. Tài khoản của bạn đã được kích hoạt thành công với email {email}.\n\nTrân trọng,\nBan Quản Trị Hệ Thống', 'EMAIL', 'KHACH_HANG', 1),
(2, 'Thông báo bảo trì máy chủ định kỳ', 'Thân gửi toàn thể anh/chị em cán bộ nhân viên,\n\nHệ thống CNTT sẽ tiến hành bảo trì nâng cấp máy chủ vào lúc 23:00 thứ Bảy tuần này. Trong thời gian bảo trì, một số dịch vụ nội bộ có thể bị gián đoạn tạm thời.\n\nTrân trọng thông báo,\nPhòng Kỹ Thuật & Vận Hành', 'EMAIL', 'NHAN_VIEN', 1),
(3, 'Chương trình tri ân đặc quyền dành cho Khách hàng VIP', 'Kính gửi Quý khách {name},\n\nNhân dịp kỷ niệm thành lập, chúng tôi trân trọng gửi tới Quý khách mã quà tặng độc quyền VIP2026 với ưu đãi 30% cho toàn bộ dịch vụ.\n\nCảm ơn Quý khách luôn đồng hành cùng chúng tôi.\nTrân trọng!', 'EMAIL', 'VIP', 1),
(4, 'Nhắc nhở nộp báo cáo kết quả đồ án học phần', 'Chào bạn {name},\n\nPhòng Đào tạo nhắc nhở sinh viên hoàn thiện báo cáo và mã nguồn đồ án trước 17:00 ngày 25 hàng tháng. Vui lòng nộp đúng hạn.\n\nTrân trọng,\nPhòng Đào Tạo', 'EMAIL', 'SINH_VIEN', 1);

-- 5. Dữ liệu Campaigns (Chiến dịch gửi)
INSERT INTO campaigns (id, name, description, notification_id, scheduled_at, status, total_recipients, success_count, failure_count, created_by, completed_at) VALUES
(1, 'Chiến dịch Tri ân Khách hàng VIP Tháng 11', 'Gửi email mã quà tặng ưu đãi 30% cho toàn bộ danh sách khách hàng VIP', 3, NOW() - INTERVAL 2 DAY, 'COMPLETED', 2, 2, 0, 1, NOW() - INTERVAL 2 DAY + INTERVAL 5 MINUTE),
(2, 'Chiến dịch Thông báo bảo trì hạ tầng nội bộ', 'Gửi thông báo bảo trì máy chủ cho phòng ban nhân viên công ty', 2, NOW() - INTERVAL 1 DAY, 'COMPLETED', 3, 3, 0, 1, NOW() - INTERVAL 1 DAY + INTERVAL 3 MINUTE),
(3, 'Chiến dịch Chào đón Tân sinh viên 2026', 'Gửi thư chúc mừng và hướng dẫn đồ án cho sinh viên mới', 4, NOW() + INTERVAL 1 DAY, 'SCHEDULED', 3, 0, 0, 1, NULL);

-- 6. Dữ liệu Campaign Recipients
INSERT INTO campaign_recipients (id, campaign_id, recipient_id, status, sent_at, error_message) VALUES
(1, 1, 7, 'SENT', NOW() - INTERVAL 2 DAY + INTERVAL 1 MINUTE, NULL),
(2, 1, 8, 'SENT', NOW() - INTERVAL 2 DAY + INTERVAL 2 MINUTE, NULL),
(3, 2, 3, 'SENT', NOW() - INTERVAL 1 DAY + INTERVAL 1 MINUTE, NULL),
(4, 2, 4, 'SENT', NOW() - INTERVAL 1 DAY + INTERVAL 2 MINUTE, NULL),
(5, 2, 13, 'SENT', NOW() - INTERVAL 1 DAY + INTERVAL 3 MINUTE, NULL),
(6, 3, 9, 'PENDING', NULL, NULL),
(7, 3, 10, 'PENDING', NULL, NULL),
(8, 3, 11, 'PENDING', NULL, NULL);

-- 7. Dữ liệu Notification Logs (Lịch sử gửi chi tiết)
-- Lưu ý: Phân định rõ trạng thái SUCCESS (gửi thực tế qua SMTP), SIMULATED (chế độ mô phỏng), FAILED (thất bại)
INSERT INTO notification_logs (id, notification_id, campaign_id, recipient_id, recipient_email, recipient_name, channel, status, sent_at, response_message, sent_by) VALUES
(1, 3, 1, 7, 'hung.domanh.vip@gmail.com', 'Đỗ Mạnh Hùng', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 2 DAY + INTERVAL 1 MINUTE, 'Mô phỏng gửi thành công: Email đã được xử lý giả lập (Chế độ Simulation Mode đang bật)', 1),
(2, 3, 1, 8, 'lan.ngomai.vip@gmail.com', 'Ngô Mai Lan', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 2 DAY + INTERVAL 2 MINUTE, 'Mô phỏng gửi thành công: Email đã được xử lý giả lập (Chế độ Simulation Mode đang bật)', 1),
(3, 2, 2, 3, 'cuong.lehoang@company.com', 'Lê Hoàng Cường', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 1 DAY + INTERVAL 1 MINUTE, 'Mô phỏng gửi thành công: Email đã được xử lý giả lập (Chế độ Simulation Mode đang bật)', 1),
(4, 2, 2, 4, 'dung.phamthi@company.com', 'Phạm Thị Dung', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 1 DAY + INTERVAL 2 MINUTE, 'Mô phỏng gửi thành công: Email đã được xử lý giả lập (Chế độ Simulation Mode đang bật)', 1),
(5, 2, 2, 13, 'nam.phanvan@company.com', 'Phan Văn Nam', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 1 DAY + INTERVAL 3 MINUTE, 'Mô phỏng gửi thành công: Email đã được xử lý giả lập (Chế độ Simulation Mode đang bật)', 1),
(6, 1, NULL, 1, 'nguyenvanan.dev@gmail.com', 'Nguyễn Văn An', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 12 HOUR, 'Gửi trực tiếp: Mô phỏng thành công tới nguyenvanan.dev@gmail.com', 1),
(7, 1, NULL, 2, 'tranthibinh.mkt@gmail.com', 'Trần Thị Bình', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 12 HOUR, 'Gửi trực tiếp: Mô phỏng thành công tới tranthibinh.mkt@gmail.com', 1),
(8, 1, NULL, 12, 'thao.trinh@client.com', 'Trịnh Phương Thảo', 'EMAIL', 'FAILED', NOW() - INTERVAL 12 HOUR, 'Lỗi kết nối máy chủ gửi: Địa chỉ hộp thư không tồn tại hoặc từ chối kết nối', 1),
(9, 4, NULL, 9, 'tuan.buiquoc@student.edu.vn', 'Bùi Quốc Tuấn', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 3 HOUR, 'Gửi trực tiếp: Mô phỏng thành công tới tuan.buiquoc@student.edu.vn', 2),
(10, 4, NULL, 10, 'ha.dinhthu@student.edu.vn', 'Đinh Thu Hà', 'EMAIL', 'SIMULATED', NOW() - INTERVAL 3 HOUR, 'Gửi trực tiếp: Mô phỏng thành công tới ha.dinhthu@student.edu.vn', 2);
