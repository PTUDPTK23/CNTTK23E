-- Dữ liệu mẫu phục vụ demo.
INSERT INTO categories (id, name, description) VALUES
 ('10000000-0000-0000-0000-000000000001','Công nghệ thông tin','Lập trình, kiến trúc phần mềm, dữ liệu'),
 ('10000000-0000-0000-0000-000000000002','Kỹ năng sống','Phát triển bản thân và giao tiếp'),
 ('10000000-0000-0000-0000-000000000003','Văn học','Tiểu thuyết và truyện'),
 ('10000000-0000-0000-0000-000000000004','Lịch sử','Lịch sử Việt Nam và thế giới'),
 ('10000000-0000-0000-0000-000000000005','Khoa học & Kinh tế','Giáo trình khoa học tự nhiên và kinh tế');

INSERT INTO books (id, title, author, publisher, publication_year, category_id, description) VALUES
 ('20000000-0000-0000-0000-000000000001','Lập trình Java từ cơ bản đến nâng cao','Nguyễn Văn An','NXB Thông tin và Truyền thông',2021,'10000000-0000-0000-0000-000000000001','Giáo trình Java: OOP, collection, stream, concurrency.'),
 ('20000000-0000-0000-0000-000000000002','Spring Boot in Action','Craig Walls','Manning',2016,'10000000-0000-0000-0000-000000000001','Xây dựng ứng dụng Spring Boot thực tế.'),
 ('20000000-0000-0000-0000-000000000003','Clean Code','Robert C. Martin','Prentice Hall',2008,'10000000-0000-0000-0000-000000000001','Nghệ thuật viết mã sạch và dễ bảo trì.'),
 ('20000000-0000-0000-0000-000000000004','Cấu trúc dữ liệu và giải thuật','Nguyễn Đức Nghĩa','NXB Giáo dục',2018,'10000000-0000-0000-0000-000000000001','Sách chỉ có 1 bản - dùng để demo trường hợp hết sách.'),
 ('20000000-0000-0000-0000-000000000005','Đắc nhân tâm','Dale Carnegie','NXB Tổng hợp TP.HCM',2019,'10000000-0000-0000-0000-000000000002','Nghệ thuật đối nhân xử thế.'),
 ('20000000-0000-0000-0000-000000000006','Nhà giả kim','Paulo Coelho','NXB Hội Nhà văn',2020,'10000000-0000-0000-0000-000000000003','Hành trình đi tìm kho báu và ý nghĩa cuộc sống.'),
 ('20000000-0000-0000-0000-000000000007','Dế Mèn phiêu lưu ký','Tô Hoài','NXB Kim Đồng',2017,'10000000-0000-0000-0000-000000000003','Tác phẩm thiếu nhi kinh điển của văn học Việt Nam.'),
 ('20000000-0000-0000-0000-000000000008','Sapiens: Lược sử loài người','Yuval Noah Harari','NXB Tri Thức',2018,'10000000-0000-0000-0000-000000000004','Lịch sử loài người từ thời nguyên thủy.'),
 ('20000000-0000-0000-0000-000000000009','Việt Nam sử lược','Trần Trọng Kim','NXB Văn học',2015,'10000000-0000-0000-0000-000000000004','Tổng quan lịch sử Việt Nam.'),
 ('20000000-0000-0000-0000-000000000010','Vật lý đại cương','Lương Duyên Bình','NXB Giáo dục',2016,'10000000-0000-0000-0000-000000000005','Giáo trình vật lý đại cương cho sinh viên.'),
 ('20000000-0000-0000-0000-000000000011','Kinh tế học vi mô','N. Gregory Mankiw','NXB Hồng Đức',2019,'10000000-0000-0000-0000-000000000005','Nguyên lý kinh tế học vi mô.'),
 ('20000000-0000-0000-0000-000000000012','Thiết kế ứng dụng xử lý dữ liệu lớn','Martin Kleppmann','O''Reilly',2017,'10000000-0000-0000-0000-000000000001','Chưa có bản sao nào - dùng để demo yêu cầu bị từ chối.');

-- Số bản sao mỗi đầu sách (sách 12 không có bản nào)
INSERT INTO book_copies (book_id, copy_code, status)
SELECT b.id, 'BK' || lpad(v.n::text, 3, '0') || '-' || lpad(g::text, 2, '0'), 'AVAILABLE'
FROM (VALUES (1,3),(2,2),(3,2),(4,1),(5,3),(6,2),(7,2),(8,2),(9,1),(10,3),(11,2)) AS v(n, cnt)
JOIN books b ON b.id = ('20000000-0000-0000-0000-' || lpad(v.n::text, 12, '0'))::uuid
CROSS JOIN LATERAL generate_series(1, v.cnt) AS g;

INSERT INTO users (id, full_name, email, phone, role) VALUES
 ('30000000-0000-0000-0000-000000000001','Quản trị viên','admin@library.local','0900000001','ADMIN'),
 ('30000000-0000-0000-0000-000000000002','Thủ thư Lan','librarian@library.local','0900000002','LIBRARIAN'),
 ('30000000-0000-0000-0000-000000000003','Nguyễn Minh Anh','minhanh@library.local','0900000003','USER'),
 ('30000000-0000-0000-0000-000000000004','Trần Quốc Bảo','quocbao@library.local','0900000004','USER'),
 ('30000000-0000-0000-0000-000000000005','Lê Thu Hà','thuha@library.local','0900000005','USER'),
 ('30000000-0000-0000-0000-000000000006','Phạm Gia Huy','giahuy@library.local','0900000006','USER');
