# Kết quả kiểm thử

## Bản bổ sung đăng nhập và audit

- Build Release thành công, 0 lỗi và 0 cảnh báo. Toàn bộ 11 kiểm tra quy tắc nghiệp vụ đạt.
- Kiểm thử trên bộ Docker Mailpit riêng xác nhận API trả 401 khi chưa đăng nhập, từ chối mật khẩu sai, cho Admin truy cập và xóa phiên khi đăng xuất.
- Subscriber ghi nhận 34 sự kiện của chiến dịch chính, gồm 31 gửi thành công, một lỗi vĩnh viễn và hai lần chờ thử lại. Phát lại cùng EventId không tạo thêm nhật ký.
- Khi tạm dừng Audit, Worker vẫn gửi thử được. Sự kiện nằm trong queue và xuất hiện trong nhật ký sau khi Audit khởi động lại.
- Các ca phục hồi Worker, RabbitMQ và PostgreSQL đều đạt. Mailpit nhận tổng cộng 45 email thử nghiệm.
- Microsoft Edge đã chạy qua màn hình đăng nhập, mật khẩu sai, hộp thoại audit, đăng xuất và kiểm tra bố cục ở 390 px.

## Kiểm thử phiên bản ban đầu

Ngày thực hiện: **16/11/2026**, múi giờ Asia/Bangkok. Môi trường: Windows, .NET SDK 10.0.401, Docker Desktop Linux containers, hai Notification Worker. Kết quả bên dưới là kiểm thử cục bộ; chưa phải kết quả workflow GitHub hay benchmark hiệu năng.

## Build và cấu hình

- `dotnet build NotifyLab.sln -c Release -m:1`: thành công, 0 warnings, 0 errors.
- `docker compose config --quiet`: cấu hình hợp lệ.
- `docker compose up -d --build --scale notification-worker=2`: build thành công hai image .NET, chạy API, hai Worker, PostgreSQL, RabbitMQ và Mailpit.
- `dotnet run --project tests/Notification.Tests -c Release --no-build`: **11/11** kiểm tra đạt (validation, loại trùng, giới hạn đầu vào, header injection, retry hữu hạn và tổng hợp Pending).

## Kiểm thử tích hợp và phục hồi

Chạy `python scripts/integration.py --faults` với cờ demo lỗi bật. Chiến dịch chính của lần chạy: `be64ef9d-9010-4b25-97c3-a56ac845be42`. Kết quả cuối: `ALL INTEGRATION CHECKS PASSED`.

| Tình huống | Kết quả quan sát |
|---|---|
| Email không hợp lệ | HTTP 400 |
| 33 địa chỉ nhập, một địa chỉ lặp hoa/thường | 32 Delivery logic |
| Bốn yêu cầu start đồng thời | Vẫn 32 Delivery; 31 Sent, 1 Failed |
| Địa chỉ retry+ | Thành công sau đúng 3 lần thử |
| Địa chỉ fail+ | Failed sau 1 lần; không retry tự động |
| Hai Worker | Lịch sử có ít nhất 2 WorkerId khác nhau |
| Retry-failed | Chỉ 1 job được xếp lại; các Delivery Sent không thêm lần gửi |
| Lịch sử gửi lại | Delivery Failed giữ 2 lần thử sau thao tác thủ công |
| Phát lại job đã Sent | Không thêm attempt |
| Message không phải JSON | Chuyển vào notification.dead |
| Tắt toàn bộ Worker | 10 job vẫn Pending trong queue |
| Restart broker khi Worker đang tắt | 10 message bền vững vẫn tồn tại |
| Bật Worker lại | Cả 10 job được gửi |
| Broker dừng khi start chiến dịch mới | API lưu 3 job Pending |
| Broker hoạt động trở lại | Outbox phát tiếp, 3 job thành Sent |
| Restart PostgreSQL | Kết quả chiến dịch chính vẫn được giữ |

Trong quá trình polling kiểm tra bất biến `Total = Pending + Sent + Failed`.

## Kiểm thử giao diện

Kiểm tra bằng Microsoft Edge chạy headless qua Playwright:

- Tạo bản nháp bằng form, start chiến dịch với 3 người nhận và quan sát 3 Sent / 0 Failed.
- Mở/đóng lịch sử gửi; lọc Failed hiển thị danh sách rỗng đúng.
- Không phát hiện JavaScript page errors trong luồng kiểm tra.
- Chụp và kiểm tra bố cục desktop 1440px, mobile 390px. Bảng trên mobile cho phép cuộn ngang trong vùng bảng, không kéo tràn toàn trang.

Ảnh minh họa: [desktop](images/dashboard-desktop.png), [mobile](images/dashboard-mobile.png). Dữ liệu trong ảnh là email thử nghiệm, không phải danh sách người nhận thật.

## Chưa kiểm chứng / giới hạn

- Chưa đo throughput, p95 latency, tải 5.000 người nhận hoặc hiệu năng một so với hai Worker.
- Chưa kiểm thử mọi thời điểm crash, đặc biệt cửa sổ SMTP đã chấp nhận nhưng chưa ghi DB; không cam kết exactly-once.
- Chưa thử nhà cung cấp email thực, TLS/SMTP AUTH ngoài Mailpit hoặc Internet deployment.
- Chưa chứng minh HA, mất ổ đĩa, network partition kéo dài hoặc khôi phục backup.
- CI được cung cấp nhưng chỉ có kết quả GitHub sau khi repository được push.

Khi bạn sửa mã hoặc thay dependency, chạy lại các lệnh trong README và cập nhật tài liệu này bằng kết quả thật.
