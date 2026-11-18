# Bàn giao Git và GitHub

Thông tin tác giả theo yêu cầu ngày 17/11/2026:

- Tên Git: `Viet0810`.
- Email Git: `dtc245200682@ictu.edu.vn`.
- Tài khoản GitHub kết nối trong ứng dụng: `Viet0810` (Đàm Văn Việt).

Tài khoản GitHub dùng để nộp bài là `Viet0810`; email tác giả và người tạo commit là `dtc245200682@ictu.edu.vn`. Đối chiếu thông tin PR tại [NOP-BAI.md](NOP-BAI.md).

## Trạng thái và quy tắc bàn giao

Repository cục bộ được bắt đầu từ bản tối ưu hiện tại. Không có lịch sử commit của giai đoạn trước để khôi phục. Các commit tiếp theo cần ghi nhận thay đổi thật; không tạo commit giả hoặc lùi ngày để mô phỏng quá trình làm bài.

`.gitignore` loại `.env`, NuGet cache, browser cục bộ và bin/obj. Ba minh chứng kiểm thử được giữ lại là `TestResults/smoke-results.json`, `patient-live.png` và `staff-live.png`. `.env.example` và appsettings chỉ chứa cấu hình demo localhost; ảnh kiểm thử dùng dữ liệu fixture.

## Repository nộp bài

- Repo lớp: https://github.com/PTUDPTK23/CNTTK23E.
- Fork: https://github.com/Viet0810/CNTTK23E.
- Nhánh: `duan-tuan1-dtc245200682`.
- Thư mục: `Đàm Văn Việt - dtc245200682/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/`.
- Tiêu đề PR: `DuAn-Tuan1-dtc245200682-Đàm Văn Việt`.

Khi sửa bài Tuần 1, mở bản clone fork, chuyển về nhánh của PR rồi commit và push thêm:

```powershell
git switch duan-tuan1-dtc245200682
git add "Đàm Văn Việt - dtc245200682/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1"
git commit -m "DuAn-Tuan1: cap nhat du an"
git push origin duan-tuan1-dtc245200682
```

Đăng nhập tài khoản có quyền vào repository khi Git Credential Manager yêu cầu. Nếu repository đích đã có dữ liệu, kiểm tra lịch sử trước khi tích hợp; không dùng force push. Sau khi push, đối chiếu `git rev-parse HEAD` với commit trên GitHub. Bản trên máy dùng đi thi phải tương ứng bản đã upload.

```powershell
git status --short
git log -5 --format=fuller
git remote -v
git config --get user.name
git config --get user.email
```

Mỗi tuần chỉ có một PR. Các tuần tiếp theo dùng thư mục `Tuan2`, `Tuan3`... và PR riêng theo hướng dẫn lớp; không tạo PR trùng cho cùng tuần.
