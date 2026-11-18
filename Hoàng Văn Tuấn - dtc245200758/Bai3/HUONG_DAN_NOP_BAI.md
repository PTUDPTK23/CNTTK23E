# HƯỚNG DẪN MỞ VÀ NỘP BÀI 3

## Mở bằng Visual Studio Code
1. Giải nén toàn bộ ZIP.
2. Mở VS Code → File → Open Folder → chọn thư mục `Bai3_SignalR_ThucHanh`.
3. Mở Terminal → New Terminal (PowerShell).
4. Chạy lần lượt:

```powershell
dotnet --version
dotnet restore .\PingSignalR\PingSignalR.slnx
dotnet build .\PingSignalR\PingSignalR.slnx
dotnet restore .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx
dotnet build .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx
```

## Chạy nhanh qua VS Code
Nhấn `Ctrl+Shift+P` → `Tasks: Run Task`, sau đó chọn:
- `Bai 3: Build ca hai solution`
- `Bai 3: Build EduSubmit`
- `Bai 3: Chay Ping API (5200)` và ở terminal khác `Bai 3: Chay Ping Client - An`
- `Bai 3: Chay EduSubmit API (5000)` và ở terminal khác `Bai 3: Chay EduSubmit Client`

Hoặc chạy các lệnh thủ công theo `README.md` gốc.

## Ảnh minh chứng
Chép ảnh chụp màn hình thực tế vào thư mục `anh/`, đúng tên trong `anh/README.txt`.
Các ảnh được yêu cầu trong README:
- `buoc1-build.png`
- `buoc2-negotiate.png`
- `buoc4-echo.png`
- `buoc5-nhom.png`
- `buoc6-noilai.png`
- `buoc7-loi1.png`, `buoc7-loi2.png`, `buoc7-loi3.png`
- `edusubmit-client.png`
- `edusubmit-requests.png`

**Tình trạng gói hiện tại:** source code và cấu hình VS Code được đóng gói; ảnh thật chưa có trong ZIP nguồn bạn tải lên. Không nộp khi chưa chép ảnh thật vào `anh/`.

## Git và commit
Nếu đây là thư mục bài trong repository lớp đã có Git, hãy chép nguyên thư mục này vào đúng vị trí được giảng viên yêu cầu. Từ terminal tại thư mục repository lớp, dùng `git status` để kiểm tra trước khi commit. Không chạy `git init` nếu thư mục đang nằm trong repository lớp.

Gợi ý tối thiểu 8 commit theo tiến trình (thực hiện từng commit khi phần tương ứng đã có trong Git):
1. `Bai3 B1: tao cau truc va hai solution`
2. `Bai3 B2: hoan thien PingSignalR Hub`
3. `Bai3 B3: hoan thien PingSignalR Client`
4. `Bai3 B4: kiem thu Echo va negotiate`
5. `Bai3 B5: kiem thu Group va Broadcast`
6. `Bai3 B6: kiem thu reconnect`
7. `Bai3 B7: bo sung cac ca loi`
8. `Bai3: hoan thien EduSubmit va anh minh chung`

Lưu ý: không thể tạo trung thực các commit tiến trình hồi tố nếu các thay đổi trước đó chưa từng được commit riêng. Không dùng `git add .` một cách mù quáng; kiểm tra `git status` và đảm bảo không có `bin/`, `obj/`, file bí mật hoặc cấu hình chứa mật khẩu.
