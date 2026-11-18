-- ==============================================================================
-- ĐỀ TÀI 11: HỆ THỐNG GỬI THÔNG BÁO HÀNG LOẠT (BULK NOTIFICATION SYSTEM)
-- Database Initialization Script (For Docker MySQL & Local Workbench)
-- ==============================================================================

CREATE DATABASE IF NOT EXISTS notification_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE notification_db;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS notification_logs;
DROP TABLE IF EXISTS campaign_recipients;
DROP TABLE IF EXISTS campaigns;
DROP TABLE IF EXISTS notifications;
DROP TABLE IF EXISTS recipients;
DROP TABLE IF EXISTS user_roles;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;

SET FOREIGN_KEY_CHECKS = 1;

-- 1. Bảng roles
CREATE TABLE roles (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Bảng users
CREATE TABLE users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    phone VARCHAR(20) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_username (username),
    INDEX idx_user_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Bảng user_roles
CREATE TABLE user_roles (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Bảng recipients
CREATE TABLE recipients (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,
    phone VARCHAR(20) NULL,
    recipient_group VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_recipient_email (email),
    INDEX idx_recipient_group (recipient_group),
    INDEX idx_recipient_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Bảng notifications
CREATE TABLE notifications (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    type VARCHAR(50) NOT NULL DEFAULT 'EMAIL',
    target_group VARCHAR(50) DEFAULT 'ALL',
    created_by BIGINT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_notifications_user FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
    INDEX idx_notification_type (type),
    INDEX idx_notification_target_group (target_group)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Bảng campaigns
CREATE TABLE campaigns (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    notification_id BIGINT NOT NULL,
    scheduled_at DATETIME NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    total_recipients INT NOT NULL DEFAULT 0,
    success_count INT NOT NULL DEFAULT 0,
    failure_count INT NOT NULL DEFAULT 0,
    created_by BIGINT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at DATETIME NULL,
    CONSTRAINT fk_campaigns_notification FOREIGN KEY (notification_id) REFERENCES notifications (id) ON DELETE CASCADE,
    CONSTRAINT fk_campaigns_user FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
    INDEX idx_campaign_status (status),
    INDEX idx_campaign_scheduled (scheduled_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. Bảng campaign_recipients
CREATE TABLE campaign_recipients (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    campaign_id BIGINT NOT NULL,
    recipient_id BIGINT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    sent_at DATETIME NULL,
    error_message VARCHAR(500) NULL,
    CONSTRAINT fk_cr_campaign FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_recipient FOREIGN KEY (recipient_id) REFERENCES recipients (id) ON DELETE CASCADE,
    INDEX idx_cr_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 8. Bảng notification_logs
CREATE TABLE notification_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    notification_id BIGINT NULL,
    campaign_id BIGINT NULL,
    recipient_id BIGINT NULL,
    recipient_email VARCHAR(100) NOT NULL,
    recipient_name VARCHAR(100) NULL,
    channel VARCHAR(50) NOT NULL DEFAULT 'EMAIL',
    status VARCHAR(30) NOT NULL,
    sent_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    response_message TEXT NULL,
    sent_by BIGINT NULL,
    CONSTRAINT fk_logs_notification FOREIGN KEY (notification_id) REFERENCES notifications (id) ON DELETE SET NULL,
    CONSTRAINT fk_logs_campaign FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE SET NULL,
    CONSTRAINT fk_logs_recipient FOREIGN KEY (recipient_id) REFERENCES recipients (id) ON DELETE SET NULL,
    CONSTRAINT fk_logs_user FOREIGN KEY (sent_by) REFERENCES users (id) ON DELETE SET NULL,
    INDEX idx_log_status (status),
    INDEX idx_log_email (recipient_email),
    INDEX idx_log_sent_at (sent_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==========================================
-- DỮ LIỆU MẪU BAN ĐẦU
-- ==========================================

-- Roles
INSERT INTO roles (id, name, description) VALUES
(1, 'ROLE_ADMIN', 'Quản trị viên toàn quyền hệ thống'),
(2, 'ROLE_USER', 'Người dùng vận hành thông thường');

-- Users (Mật khẩu mặc định: admin123 và user123 mã hóa BCrypt)
INSERT INTO users (id, username, password, full_name, email, phone, status) VALUES
(1, 'admin', '$2a$10$7R3v5K0GZ1zJ5kK5H9/f1OW2wQZ8d8f9y0x1A2b3C4d5E6f7G8h9i', 'Quản Trị Viên Hệ Thống', 'admin@notification.vn', '0901234567', 'ACTIVE'),
(2, 'user', '$2a$10$7R3v5K0GZ1zJ5kK5H9/f1OW2wQZ8d8f9y0x1A2b3C4d5E6f7G8h9i', 'Nhân Viên Vận Hành', 'user@notification.vn', '0907654321', 'ACTIVE');

-- User Roles
INSERT INTO user_roles (user_id, role_id) VALUES
(1, 1),
(1, 2),
(2, 2);

-- Recipients
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

-- Notifications
INSERT INTO notifications (id, title, content, type, target_group, created_by) VALUES
(1, 'Chào mừng thành viên mới gia nhập hệ thống', 'Kính gửi {name},\n\nChúng tôi rất hân hạnh chào đón bạn đến với hệ thống thông báo tự động. Tài khoản của bạn đã được kích hoạt thành công với email {email}.\n\nTrân trọng,\nBan Quản Trị Hệ Thống', 'EMAIL', 'KHACH_HANG', 1),
(2, 'Thông báo bảo trì máy chủ định kỳ', 'Thân gửi toàn thể anh/chị em cán bộ nhân viên,\n\nHệ thống CNTT sẽ tiến hành bảo trì nâng cấp máy chủ vào lúc 23:00 thứ Bảy tuần này. Trong thời gian bảo trì, một số dịch vụ nội bộ có thể bị gián đoạn tạm thời.\n\nTrân trọng thông báo,\nPhòng Kỹ Thuật & Vận Hành', 'EMAIL', 'NHAN_VIEN', 1),
(3, 'Chương trình tri ân đặc quyền dành cho Khách hàng VIP', 'Kính gửi Quý khách {name},\n\nNhân dịp kỷ niệm thành lập, chúng tôi trân trọng gửi tới Quý khách mã quà tặng độc quyền VIP2026 với ưu đãi 30% cho toàn bộ dịch vụ.\n\nCảm ơn Quý khách luôn đồng hành cùng chúng tôi.\nTrân trọng!', 'EMAIL', 'VIP', 1),
(4, 'Nhắc nhở nộp báo cáo kết quả đồ án học phần', 'Chào bạn {name},\n\nPhòng Đào tạo nhắc nhở sinh viên hoàn thiện báo cáo và mã nguồn đồ án trước 17:00 ngày 25 hàng tháng. Vui lòng nộp đúng hạn.\n\nTrân trọng,\nPhòng Đào Tạo', 'EMAIL', 'SINH_VIEN', 1);

-- Campaigns
INSERT INTO campaigns (id, name, description, notification_id, scheduled_at, status, total_recipients, success_count, failure_count, created_by, completed_at) VALUES
(1, 'Chiến dịch Tri ân Khách hàng VIP Tháng 11', 'Gửi email mã quà tặng ưu đãi 30% cho toàn bộ danh sách khách hàng VIP', 3, NOW() - INTERVAL 2 DAY, 'COMPLETED', 2, 2, 0, 1, NOW() - INTERVAL 2 DAY + INTERVAL 5 MINUTE),
(2, 'Chiến dịch Thông báo bảo trì hạ tầng nội bộ', 'Gửi thông báo bảo trì máy chủ cho phòng ban nhân viên công ty', 2, NOW() - INTERVAL 1 DAY, 'COMPLETED', 3, 3, 0, 1, NOW() - INTERVAL 1 DAY + INTERVAL 3 MINUTE),
(3, 'Chiến dịch Chào đón Tân sinh viên 2026', 'Gửi thư chúc mừng và hướng dẫn đồ án cho sinh viên mới', 4, NOW() + INTERVAL 1 DAY, 'SCHEDULED', 3, 0, 0, 1, NULL);

-- Campaign Recipients
INSERT INTO campaign_recipients (id, campaign_id, recipient_id, status, sent_at, error_message) VALUES
(1, 1, 7, 'SENT', NOW() - INTERVAL 2 DAY + INTERVAL 1 MINUTE, NULL),
(2, 1, 8, 'SENT', NOW() - INTERVAL 2 DAY + INTERVAL 2 MINUTE, NULL),
(3, 2, 3, 'SENT', NOW() - INTERVAL 1 DAY + INTERVAL 1 MINUTE, NULL),
(4, 2, 4, 'SENT', NOW() - INTERVAL 1 DAY + INTERVAL 2 MINUTE, NULL),
(5, 2, 13, 'SENT', NOW() - INTERVAL 1 DAY + INTERVAL 3 MINUTE, NULL),
(6, 3, 9, 'PENDING', NULL, NULL),
(7, 3, 10, 'PENDING', NULL, NULL),
(8, 3, 11, 'PENDING', NULL, NULL);

-- Notification Logs
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
