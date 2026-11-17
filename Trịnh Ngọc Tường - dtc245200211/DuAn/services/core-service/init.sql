-- ============================================================
-- CSDL HỆ THỐNG NỘP VÀ CHẤM BÀI TỰ ĐỘNG PHÂN TÁN
-- Sinh viên: Trịnh Ngọc Tường - DTC245200211
-- Học phần: Phát triển ứng dụng phân tán
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. BẢNG USERS
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    student_code VARCHAR(30) UNIQUE,
    role VARCHAR(20) DEFAULT 'STUDENT', -- 'ADMIN', 'TEACHER', 'STUDENT'
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. BẢNG COURSES
CREATE TABLE IF NOT EXISTS courses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    course_code VARCHAR(30) UNIQUE NOT NULL,
    course_name VARCHAR(150) NOT NULL,
    description TEXT,
    semester VARCHAR(20) DEFAULT 'Học kỳ 1 - 2026',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 3. BẢNG ASSIGNMENTS (9 TUẦN)
CREATE TABLE IF NOT EXISTS assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    course_id UUID REFERENCES courses(id) ON DELETE CASCADE,
    week_no INT NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    allowed_languages JSONB DEFAULT '["JAVA", "PYTHON", "CPP", "GO"]',
    time_limit_ms INT DEFAULT 2000,
    memory_limit_mb INT DEFAULT 256,
    deadline TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 4. BẢNG TESTCASES
CREATE TABLE IF NOT EXISTS testcases (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    assignment_id UUID REFERENCES assignments(id) ON DELETE CASCADE,
    input_data TEXT NOT NULL,
    expected_output TEXT NOT NULL,
    score_weight INT DEFAULT 10,
    is_hidden BOOLEAN DEFAULT FALSE,
    order_num INT DEFAULT 1
);

-- 5. BẢNG SUBMISSIONS
CREATE TABLE IF NOT EXISTS submissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    assignment_id UUID REFERENCES assignments(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    language VARCHAR(20) NOT NULL,
    source_code TEXT,
    source_file_path VARCHAR(255),
    status VARCHAR(30) DEFAULT 'PENDING', -- PENDING, COMPILING, RUNNING, COMPLETED, ERROR
    verdict VARCHAR(50) DEFAULT 'WAITING', -- ACCEPTED, WRONG_ANSWER, TIME_LIMIT_EXCEEDED, MEMORY_LIMIT_EXCEEDED, COMPILE_ERROR
    total_score NUMERIC(5, 2) DEFAULT 0.0,
    max_score NUMERIC(5, 2) DEFAULT 10.0,
    execution_time_ms INT DEFAULT 0,
    memory_used_kb INT DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 6. BẢNG SUBMISSION_DETAILS
CREATE TABLE IF NOT EXISTS submission_details (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    submission_id UUID REFERENCES submissions(id) ON DELETE CASCADE,
    testcase_id UUID REFERENCES testcases(id) ON DELETE CASCADE,
    status VARCHAR(30) NOT NULL, -- PASSED, FAILED, TLE, MLE
    actual_output TEXT,
    error_log TEXT,
    execution_time_ms INT DEFAULT 0
);

-- ============================================================
-- DỮ LIỆU KHỞI TẠO (SEED DATA)
-- ============================================================

-- Thêm Giảng viên & Sinh viên mẫu
INSERT INTO users (id, username, full_name, student_code, role, password_hash)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'giangvien', 'TS. Nguyễn Văn A', 'GV001', 'TEACHER', '$2b$10$abcdefghijklmnopqrstuv'),
    ('22222222-2222-2222-2222-222222222222', 'tuongdtc', 'Trịnh Ngọc Tường', 'DTC245200211', 'STUDENT', '$2b$10$abcdefghijklmnopqrstuv')
ON CONFLICT DO NOTHING;

-- Thêm Môn học
INSERT INTO courses (id, course_code, course_name, description)
VALUES 
    ('33333333-3333-3333-3333-333333333333', 'PTUDPT', 'Phát triển ứng dụng phân tán', 'Học phần thực hành kiến trúc hệ thống và lập trình ứng dụng phân tán 9 tuần')
ON CONFLICT DO NOTHING;

-- Thêm Đề bài 9 Tuần
INSERT INTO assignments (course_id, week_no, title, description, time_limit_ms, memory_limit_mb)
VALUES
    ('33333333-3333-3333-3333-333333333333', 1, 'Tuần 1: Lập trình Mạng Socket TCP/UDP', 'Viết chương trình Client - Server tính toán biểu thức số học qua giao thức TCP Socket.', 2000, 256),
    ('33333333-3333-3333-3333-333333333333', 2, 'Tuần 2: Đa luồng & Concurrency trong Hệ phân tán', 'Xây dựng Multi-threaded Server hỗ trợ tối thiểu 50 client kết nối và truyền nhận dữ liệu đồng thời.', 3000, 256),
    ('33333333-3333-3333-3333-333333333333', 3, 'Tuần 3: Gọi hàm từ xa (Java RMI / gRPC)', 'Cài đặt dịch vụ tra cứu thông tin sinh viên và quản lý tín chỉ sử dụng giao thức RMI hoặc gRPC.', 2000, 256),
    ('33333333-3333-3333-3333-333333333333', 4, 'Tuần 4: Kiến trúc RESTful Web Services', 'Thiết kế REST API quản lý tài nguyên phân tán, định dạng JSON chuẩn và xử lý HTTP status codes.', 2000, 256),
    ('33333333-3333-3333-3333-333333333333', 5, 'Tuần 5: Message-Oriented Middleware (RabbitMQ)', 'Xây dựng hệ thống Pub/Sub xử lý đơn hàng bất đồng bộ sử dụng Topic Exchange trong RabbitMQ.', 3000, 256),
    ('33333333-3333-3333-3333-333333333333', 6, 'Tuần 6: Caching & Khóa phân tán (Redis Distributed Lock)', 'Ứng dụng Redis cache và cơ chế Redlock giải quyết bài toán đặt vé đồng thời chống Race Condition.', 2000, 256),
    ('33333333-3333-3333-3333-333333333333', 7, 'Tuần 7: Xác thực & Bảo mật Phân tán (JWT & API Gateway)', 'Xây dựng cơ chế SSO xác thực tập trung với JWT và định tuyến qua Reverse Proxy / Gateway.', 2000, 256),
    ('33333333-3333-3333-3333-333333333333', 8, 'Tuần 8: Tính chịu lỗi & Tải cân bằng (Load Balancing & Failover)', 'Cấu hình cân bằng tải Round Robin và cơ chế Healthcheck phát hiện node chết (Heartbeat).', 3000, 256),
    ('33333333-3333-3333-3333-333333333333', 9, 'Tuần 9: Đồ án Cuối kỳ - Tích hợp Hệ thống Phân tán Hoàn chỉnh', 'Triển khai cụm microservices hoàn chỉnh với Docker Compose, giám sát log và kiểm thử tự động.', 5000, 512);

-- Thêm Testcases mẫu cho Tuần 1
INSERT INTO testcases (assignment_id, input_data, expected_output, score_weight, is_hidden, order_num)
SELECT id, 'ADD 15 25', 'RESULT 40', 2, FALSE, 1 FROM assignments WHERE week_no = 1
UNION ALL
SELECT id, 'SUB 50 12', 'RESULT 38', 2, FALSE, 2 FROM assignments WHERE week_no = 1
UNION ALL
SELECT id, 'MUL 7 8', 'RESULT 56', 2, FALSE, 3 FROM assignments WHERE week_no = 1
UNION ALL
SELECT id, 'DIV 100 4', 'RESULT 25', 2, FALSE, 4 FROM assignments WHERE week_no = 1
UNION ALL
SELECT id, 'DIV 10 0', 'ERROR DIVISION_BY_ZERO', 2, TRUE, 5 FROM assignments WHERE week_no = 1;
