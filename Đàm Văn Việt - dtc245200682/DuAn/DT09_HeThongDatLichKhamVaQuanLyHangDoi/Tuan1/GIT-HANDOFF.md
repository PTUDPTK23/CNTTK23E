# Bàn giao Git và GitHub

Thông tin tác giả theo yêu cầu ngày 17/11/2026:

- Tên Git: `Viet0810`.
- Email Git: `dtc245200682@ictu.edu.vn`.
- Tài khoản GitHub kết nối trong ứng dụng: `Viet0810` (Đàm Văn Việt).

Tên/email Git là thông tin gắn vào commit, không phải bằng chứng đăng nhập GitHub trên máy. Máy hiện chưa có lệnh `gh`; tài khoản kết nối ứng dụng không tự cấu hình credential cho `git push`.

## Trạng thái và quy tắc bàn giao

Repository cục bộ được bắt đầu từ bản tối ưu hiện tại. Không có lịch sử commit của giai đoạn trước để khôi phục. Các commit tiếp theo cần ghi nhận thay đổi thật; không tạo commit giả hoặc lùi ngày để mô phỏng quá trình làm bài.

`.gitignore` loại `.env`, NuGet cache, browser cục bộ, bin/obj và kết quả test. `.env.example` và appsettings chỉ chứa cấu hình demo localhost. Khi cần công bố bản có cấu hình riêng, kiểm tra secrets trước khi push. Không đưa dữ liệu bệnh nhân thật vào repository.

## Đưa lên GitHub khi đã chọn repository đích

Tạo repository GitHub trống trong tài khoản muốn sử dụng. Chạy trong thư mục `DuAn` và thay `TEN_REPOSITORY` bằng tên đã chọn:

```powershell
git remote add origin https://github.com/Viet0810/TEN_REPOSITORY.git
git push -u origin main
```

Đăng nhập tài khoản có quyền vào repository khi Git Credential Manager yêu cầu. Nếu repository đích đã có dữ liệu, kiểm tra lịch sử trước khi tích hợp; không dùng force push. Sau khi push, đối chiếu `git rev-parse HEAD` với commit trên GitHub. Bản trên máy dùng đi thi phải tương ứng bản đã upload.

```powershell
git status --short
git log -5 --format=fuller
git remote -v
git config --get user.name
git config --get user.email
```

Hướng dẫn chung yêu cầu GitHub và lịch sử commit. Repository cục bộ chỉ hoàn thành phần chuẩn bị; việc upload cần có repository đích cụ thể.
