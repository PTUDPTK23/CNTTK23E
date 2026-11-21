# Báo cáo nghiệm thu Tuần 2

Ngày rà soát: 21/11/2026. Branch: `duan-tuan2-dtc245200016`. Baseline Prompt 03.7: commit `2e589ca`.

## 1. Phạm vi và kết quả

Tuần 2 triển khai Identity/JWT, MVC Cookie Authentication, bốn dashboard theo vai trò và các module quản trị User, Department, Room, Doctor, Patient. Solution giữ chín project .NET 10; Web gọi API qua typed `HttpClient`, không truy cập DbContext trực tiếp.

Các luồng Appointment, queue, RabbitMQ, Worker nghiệp vụ và SignalR chưa thuộc kết quả hoàn thành của Tuần 2. Trạng thái yêu cầu chi tiết xem [topic09-requirements-traceability.md](topic09-requirements-traceability.md).

## 2. Kiến trúc và bảo mật

- API dùng ASP.NET Core Identity, JWT bearer, kiểm tra `SecurityStamp`, trạng thái tài khoản và Role hiện hành.
- MVC giữ Access Token trong token store in-memory phía server; HTML/JavaScript/browser storage không nhận JWT.
- Cookie dùng `HttpOnly`, `Secure`, `SameSite=Lax`, không sliding expiration; POST MVC có antiforgery.
- API quản trị dùng Role `Admin`; hồ sơ Doctor/Patient tự xem theo identity/ownership. Test có kiểm tra anonymous, sai Role, IDOR và token không lộ.
- EF Core dùng transaction cho command nhiều bước, unique index và `rowversion` cho các quan hệ/cập nhật cần bảo vệ.
- Không có Migration mới hoặc thay đổi schema trong quality gate này.

## 3. Quality gate tự động

| Kiểm tra | Kết quả |
|---|---|
| `dotnet restore MediQueue.slnx --locked-mode` | PASS; có cảnh báo NU1900 do không truy cập được nguồn advisory NuGet |
| `dotnet build MediQueue.slnx --no-restore` | PASS, 0 error; cảnh báo NU1900 môi trường |
| Unit tests | PASS 34/34 |
| Integration tests | PASS 122; SKIP 4 SQL opt-in; 0 fail |
| SQL Server read-only opt-in | NOT VERIFIED: 4/4 fail ở kết nối vì máy không hỗ trợ chế độ mã hóa SQL Server yêu cầu; không có truy vấn ghi |
| `dotnet format --verify-no-changes` | PASS |
| `git diff --check` | PASS |
| UTF-8 / Markdown links | PASS: 0 tệp lỗi UTF-8, 0 liên kết nội bộ hỏng |
| Browser visual QA / Kestrel HTTPS | NOT VERIFIED: Kestrel không khởi động do thiếu development certificate trong môi trường hiện tại |
| NuGet vulnerability audit | NOT VERIFIED vì không truy cập được `https://api.nuget.org/v3/index.json` |

## 4. UI/UX đã rà soát

- Login/register/profile/change-password, shared layout, sidebar/header và bốn dashboard.
- Danh sách, chi tiết, form, empty/error/confirm state của User, Department, Room, Doctor, Patient.
- Loại bỏ microcopy nói về prompt hoặc “giai đoạn tiếp theo” khỏi dashboard người dùng.
- Chuẩn hóa trạng thái hiển thị thành “Đang hoạt động”, “Ngừng hoạt động”, “Kích hoạt lại”.
- Không phát hiện link `href="#"` hoặc `javascript:` trong Razor Views.
- Design token, focus-visible, responsive layout và `prefers-reduced-motion` đã có trong CSS hiện hành.
- Sidebar áp dụng trạng thái thu gọn trước lần render đầu, giữ cùng font-weight giữa trạng thái thường/active, preload các font dùng ngay và không chạy page entrance animation trên workspace. `scrollbar-gutter: stable` giúp giảm dịch ngang giữa trang ngắn và dài.

## 5. Checklist nghiệm thu thủ công

Không tự đánh dấu PASS; sinh viên thực hiện bằng tài khoản hợp lệ trên `https://localhost:7102`.

- **Admin:** login; dashboard; Users/Departments/Rooms/Doctors/Patients; create/edit/search/filter/pagination; validation/duplicate/status; logout.
- **Doctor:** login; dashboard; hồ sơ chuyên môn; không truy cập module Admin.
- **Receptionist:** login; dashboard/profile; không truy cập module Admin.
- **Patient:** login/register; dashboard/profile; cập nhật thông tin; không xem dữ liệu người khác.
- **UI:** desktop/mobile; sidebar/dropdown/modal; hover/focus; loading/empty/error; không có nội dung demo hoặc link giả.
- **Database:** lưu một thay đổi được phép bằng thao tác thủ công, reload và xác minh còn dữ liệu; không xóa bản ghi để thử nghiệm.

## 6. Giới hạn và mục chưa kiểm chứng

- `OFFICIAL TOPIC 09 REQUIREMENTS – NOT VERIFIED`: repository lớp chỉ xác nhận tên đề tài, người thực hiện, đường dẫn và quy định 9 tuần; chưa có đặc tả chi tiết chính thức.
- `SQL SERVER READ-ONLY – NOT VERIFIED` trong lần quality gate này vì lỗi tương thích mã hóa ở máy chạy test.
- `BROWSER VISUAL QA – NOT VERIFIED` và `KESTREL HTTPS – NOT VERIFIED`: lần chạy hiện tại thiếu development certificate; không thay TLS hoặc tạo certificate toàn máy.
- Các command tạo/sửa/trạng thái chưa được tự động chạy trên database phát triển chính thức để tránh ghi dữ liệu.
- Appointment/queue/messaging/realtime vẫn ở roadmap Tuần 3–7, không phải chức năng Tuần 2.

