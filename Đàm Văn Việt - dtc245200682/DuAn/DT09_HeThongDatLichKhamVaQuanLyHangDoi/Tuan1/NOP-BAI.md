# Thông tin nộp dự án Tuần 1

- Sinh viên: **Đàm Văn Việt**, MSSV **dtc245200682**.
- Tài khoản GitHub: **Viet0810**.
- Email Git: **dtc245200682@ictu.edu.vn**.
- Đề tài 09: Hệ thống đặt lịch khám và quản lý hàng đợi.
- Repository lớp: https://github.com/PTUDPTK23/CNTTK23E.
- Fork: https://github.com/Viet0810/CNTTK23E.
- Thư mục: `Đàm Văn Việt - dtc245200682/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/`.
- Nhánh: `duan-tuan1-dtc245200682`.
- Tiêu đề PR: **DuAn-Tuan1-dtc245200682-Đàm Văn Việt**.
- PR: **[#112](https://github.com/PTUDPTK23/CNTTK23E/pull/112)**.
- Tác giả PR: **Viet0810**. Nhánh nguồn `Viet0810:duan-tuan1-dtc245200682`; nhánh đích `PTUDPTK23/CNTTK23E:main`.
- Đã push toàn bộ bản nộp và tạo PR; việc gộp/chấm do giảng viên quyết định.
- Phạm vi nộp: toàn bộ bản dự án hiện có. Các thay đổi tiếp theo sẽ được ghi nhận bằng commit mới; mỗi tuần nộp theo thư mục và PR tương ứng theo hướng dẫn lớp.

## Bản nộp và kiểm chứng

- Mã nguồn đầy đủ, SQL migration, Dockerfile riêng của ba service, docker-compose, cấu hình mẫu và tài liệu chạy/demo.
- Báo cáo Markdown và Word; kèm hai PDF yêu cầu chung và đề tài.
- Log `TestResults/smoke-results.json` ngày 17/11/2026: **14/14 PASS**, run ID `478dcef3`; kèm ảnh bệnh nhân và nhân viên của lần kiểm thử này.
- Build lại ngày 18/11/2026: `dotnet build Clinic.sln --no-restore --tl:off` thành công, **0 warning, 0 error**. Không chạy lại suite Docker trong bước nộp; kết quả 14 ca là của lần chạy ngày 17/11.
- Không nộp `.env`, cache, browser cục bộ, `bin/obj`. Chỉ kèm `.env.example` với giá trị demo localhost.
- Git cục bộ bắt đầu từ bản bàn giao ngày 17/11; các commit sau ghi nhận chuẩn bị nộp. Không tái dựng lịch sử phát triển trước đó.
