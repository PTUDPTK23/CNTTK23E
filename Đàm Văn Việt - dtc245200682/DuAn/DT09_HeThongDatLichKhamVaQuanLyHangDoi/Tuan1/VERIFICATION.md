# Kết quả xác minh thực tế

## Kiểm tra bản nộp

Build lại bằng `dotnet build Clinic.sln --no-restore --tl:off`: **0 warning, 0 error**. Bộ smoke Docker không được chạy lại trong bước nộp; 14/14 ca và ảnh bên dưới thuộc lần kiểm thử ngày 17/11/2026. Kết quả JSON và hai ảnh được nộp kèm trong `TestResults/`. Thông tin repository và PR hiện tại xem [NOP-BAI.md](NOP-BAI.md); các mô tả trạng thái Git trong phần lịch sử bên dưới phản ánh thời điểm kiểm thử tương ứng.

## Bản tối ưu ngày 17/11/2026 — kết quả mới nhất

Hoàn tất **08:17:39 ngày 17/11/2026, giờ Việt Nam** (UTC+7), run ID **478dcef3**. Suite trả exit code **0**, `failed=false`, **14/14 ca PASS** trong [TestResults/smoke-results.json](TestResults/smoke-results.json). Mười ca có sẵn tiếp tục đạt; bốn ca mới kiểm tra các thay đổi ở bản tối ưu.

| Ca mới | Bằng chứng quan sát |
|---|---|
| HTTP retry đồng thời, restart và xung đột nội dung | 8 POST cùng khóa trả cùng ID/token; SQL chỉ 1 lịch, reserved=1, 1 number.requested. Nội dung khác 409, khóa sai 400. Restart API vẫn replay; hủy rồi replay trả Cancelled, reserved=0. |
| Chỗ trống khi Worker dừng và reconnect | Chromium thật thấy giảm/tăng chỗ khi API client khác đặt/hủy dù lịch còn Pending. Khung đang chọn được giữ nếu còn chỗ. Offline, hủy ở API rồi reconnect: số chỗ được đồng bộ. |
| Reload đúng bác sĩ/ngày | Lưu lịch của bác sĩ fixture khác, ngày mai; reload giữ đúng bác sĩ, ngày, tên bệnh nhân và kết nối SignalR. |
| Mất phản hồi POST rồi reload/thử lại | Playwright cho server xử lý POST rồi chặn phản hồi. Reload vẫn có nút thử lại; retry lấy được lịch cũ, SQL đúng 1 lịch và reserved=1. |

Build `dotnet build Clinic.sln --no-restore` đạt **0 warning, 0 error**. Đã build/start ba service bằng Docker Compose và áp dụng migration 002 tự động. Dùng PostgreSQL, RabbitMQ và Chromium thật; không dùng mock. Suite tạo và dọn fixture theo ID riêng, giữ nguyên volume. Sau suite Worker/RabbitMQ được bật lại.

Lần kiểm thử đầu của bản sửa phát hiện HTML còn thiếu nút retry nên khởi tạo trang bệnh nhân bị lỗi; đã bổ sung nút, build lại API và chạy lại toàn bộ suite. Kết quả 14/14 ở trên là lần chạy sau sửa lỗi.

Báo cáo được bổ sung ở BAO-CAO.md và BAO-CAO.docx (ba sơ đồ); DEMO.md có kịch bản trình bày. Git cục bộ bắt đầu từ bản tối ưu với tác giả `Viet0810 <dtc245200682@ictu.edu.vn>`. Chưa upload GitHub vì chưa có repository đích. Chưa đo tải, triển khai production/TLS hoặc kiểm thử nhiều Notification replica.

## Lịch sử xác minh bản đầu ngày 15/11/2026

Các đoạn dưới ghi lại quan sát của bản đầu. File JSON/ảnh trong TestResults hiện chứa kết quả lần chạy mới nhất; các câu mô tả Git hoặc số khung giờ bên dưới chỉ phản ánh thời điểm 15/11.

Hoàn tất lúc **07:32:44 ngày 15/11/2026, giờ Việt Nam** (UTC+7). Lần chạy `1ac9961f`, kết quả máy đọc được: [TestResults/smoke-results.json](TestResults/smoke-results.json), `failed=false`, exit code **0**. Không dùng mock database/message bus cho các ca dưới đây.

## Môi trường và build

- Windows, SDK .NET **10.0.403**. `dotnet build Clinic.sln --no-restore`: thành công, **0 warning / 0 error**.
- Docker Engine **29.9.2**, Linux containers qua Docker Desktop. Build thành công Dockerfile riêng của cả ba service.
- Đã chạy thành công `powershell -ExecutionPolicy Bypass -File scripts/start.ps1`: tự tạo `.env` từ mẫu, build/start stack và chờ API ready; exit code 0. Script `test.ps1` đã kiểm tra cú pháp PowerShell; các lệnh build/install browser/run suite bên trong đã được chạy riêng thành công.
- PostgreSQL `17-alpine`, RabbitMQ `4-management-alpine`, API/Worker/Notification chạy độc lập trong Compose.
- Playwright **1.57.0**, Chromium Headless Shell **143.0.7499.4**, browser được tải vào `DuAn/.local/browsers`. Hai browser context độc lập đại diện bệnh nhân/nhân viên; đã xem ảnh chụp để đối chiếu giao diện.
- Hai PDF gốc đã trích xuất bằng `pypdf`. SHA-256 PDF đề tài trong DuAn vẫn trùng bản gốc: `2EFBF11328B577181DAC0747A6D8F62D7FCA1D81EF38761779520A2849666860`.

## Sáu tình huống bắt buộc

| Ca | Kết quả | Bằng chứng đã quan sát |
|---|---|---|
| 1. Hai bệnh nhân cùng đặt chỗ cuối | **Đạt** | Hai HTTP POST đồng thời: đúng một 201, một 409; SQL xác nhận đúng một lịch và reserved=1. |
| 2. Worker dừng rồi bật lại | **Đạt** | Thực sự `docker compose stop worker`; đặt lịch rồi GET thấy Pending/number=null. `start worker`, GET chuyển Waiting/number=1. |
| 3. RabbitMQ giao lại yêu cầu | **Đạt** | Lấy message chưa ACK rồi đóng channel; lần nhận tiếp theo có `Redelivered=true`. Worker xử lý, sau đó publish cùng message thêm hai lần; số vẫn 1, SQL last_number=1. |
| 4. Hủy trước khi Worker xử lý | **Đạt** | Worker đang dừng, đặt rồi DELETE hai lần: reserved=0. Bật Worker: lịch vẫn Cancelled và number=null. Đặt lại vào chỗ vừa trả và nhận số 1. |
| 5. Hai cửa sổ cập nhật SignalR | **Đạt** | Đặt lịch qua form browser bệnh nhân, nhân viên đăng nhập và bấm gọi. Browser bệnh nhân tự đổi current=1 và trạng thái Serving; không reload, chỉ một navigation, không PageError JavaScript. |
| 6. Ngắt kết nối rồi reconnect | **Đạt** | Đặt browser context bệnh nhân offline; hoàn thành lượt trên cửa sổ nhân viên trong lúc offline. Bật mạng: client tự reconnect, ghi nhận một GET queue, current trở về “—”, lịch Completed, không reload. |

Ảnh chụp ca 5: [bệnh nhân](TestResults/patient-live.png), [nhân viên](TestResults/staff-live.png). Bác sĩ có tiền tố TEST trên ảnh là fixture kiểm thử đã được dọn sau lần chạy.

## Các ca bổ sung đã chạy

| Ca | Kết quả | Bằng chứng |
|---|---|---|
| Phân quyền REST | **Đạt** | GET/DELETE lịch không có khóa: 403; gọi lượt không đăng nhập: 401. |
| Thứ tự khung giờ | **Đạt** | Lịch khung sớm được gọi trước dù số lớn hơn lịch khung muộn; lịch trong khung chưa bắt đầu không được gọi, trả 409. |
| Outbox khi broker dừng và API restart | **Đạt** | Thực sự dừng RabbitMQ, đặt thành công; SQL thấy outbox chưa gửi. Restart API vẫn tra cứu được Pending. Bật RabbitMQ: Worker nhận yêu cầu, lịch có đúng số 1. |
| Quyền tham gia nhóm SignalR cá nhân | **Đạt** | Browser gọi JoinAppointment với UUID thật và khóa sai; Hub từ chối. |

Tổng **10/10 ca đạt**. Sau khi dọn fixture: **2 bác sĩ mẫu, 42 khung giờ, 0 outbox chưa gửi**. Năm container đang chạy; PostgreSQL/RabbitMQ healthy, `/health` của API và Notification trả ready.

## Lỗi phát hiện và đã xử lý

Lần chạy đầu phát hiện cấu hình Urls khiến API/Notification chỉ bind localhost trong container và Worker image thiếu ASP.NET shared runtime. Đã sửa ưu tiên ASPNETCORE_URLS và dùng image runtime phù hợp. Chrome cài sẵn tự đóng khi automation; đã dùng Chromium tương thích với Playwright trong DuAn. Test harness đặt đường dẫn browser trước khi khởi tạo Playwright driver. Các kết quả đạt ở trên thuộc lần chạy sau các sửa lỗi này; không suy diễn từ build đơn thuần. Khi kiểm tra script bàn giao, Windows PowerShell đọc sai chuỗi UTF-8 không BOM; đã đổi thông báo của script sang ASCII và chạy lại start.ps1 thành công.

## Những phần chưa xác minh

Chưa chạy triển khai production/TLS, tải lớn, nhiều replica Notification/SignalR backplane hoặc sự cố mất điện/đĩa vì môi trường kiểm chứng là một stack Docker cục bộ. Chưa khởi động đồng thời ba service bằng `dotnet run` trên host vì các port đang phục vụ stack Compose; build trên host đã đạt. Các mục này không được tính là kiểm thử đạt.

Không có Git repository ban đầu, không commit/push. Tất cả mã nguồn, cấu hình, tài liệu bổ sung và artifact kiểm thử nằm trong DuAn; không sửa bai1, bai2 hoặc PDF gốc.
