# Quản lý người dùng và tài khoản nhân sự

## 1. Phạm vi Prompt 03.5

Module `/Users` chỉ dành cho `Admin`. Chức năng đã triển khai: danh sách có tìm kiếm/lọc/sắp xếp/phân trang phía server; xem chi tiết; tạo tài khoản `Doctor` hoặc `Receptionist`; sửa họ tên/số điện thoại của nhân sự; vô hiệu hóa, kích hoạt và mở khóa đăng nhập. Module không tạo `Admin`/`Patient`, không đổi email/Role/mật khẩu, không xóa tài khoản và không tạo hồ sơ bác sĩ giả.

Patient vẫn tự đăng ký qua luồng công khai hiện có. Admin chỉ sửa họ tên của chính mình qua Hồ sơ cá nhân.

## 2. Nguồn tên hiển thị

`ApplicationUser.StaffFullName` là trường nullable tối đa 150 ký tự dành cho tài khoản staff chưa có profile riêng. Patient dùng `Patient.FullName`; Doctor có profile dùng `Doctor.DisplayName`, chưa có profile dùng `StaffFullName`; Receptionist và Admin dùng `StaffFullName`. Email alias chỉ là fallback giao diện, không được lưu như họ tên.

## 3. API và bảo mật

| Method | Route | Mục đích |
|---|---|---|
| GET | `/api/users` | Danh sách, thống kê, lọc và phân trang |
| GET | `/api/users/{id}` | Chi tiết tối thiểu |
| POST | `/api/users` | Tạo Doctor/Receptionist |
| PUT | `/api/users/{id}` | Sửa họ tên/số điện thoại nhân sự |
| POST | `/api/users/{id}/deactivate` | Vô hiệu hóa và thu hồi phiên |
| POST | `/api/users/{id}/activate` | Kích hoạt; không phục hồi token cũ |
| POST | `/api/users/{id}/unlock` | Xóa lockout, không đổi mật khẩu/Role/trạng thái active |

Tất cả endpoint yêu cầu JWT Role `Admin`, trả `ProblemDetails` và `no-store`. MVC dùng typed client với token phía server; mọi command MVC là POST kèm antiforgery. API không trả password hash, SecurityStamp, ConcurrencyStamp, AccessFailedCount hoặc dữ liệu y tế.

`IsActive` và Identity lockout là hai trạng thái độc lập. Không thể vô hiệu hóa chính mình hoặc tác động tài khoản Admin qua module. Vô hiệu hóa/kích hoạt/mở khóa xoay `SecurityStamp`, nên Access Token cũ mất hiệu lực. Thao tác quan trọng được ghi `AuditLogs` với Actor, action, target và metadata không chứa PII.

## 4. Trạng thái Migration

Migration `20261119135325_AddStaffFullNameToApplicationUser` chỉ thêm `AspNetUsers.StaffFullName nvarchar(150) NULL`. `InitialCreate` được giữ nguyên. SQL idempotent để đối chiếu: [AddStaffFullNameToApplicationUser.sql](sql/AddStaffFullNameToApplicationUser.sql).

Sinh viên đã apply migration này vào đúng `LAPTOP-AQFGUM9Q\SQLEXPRESS` / `MediQueue`. Kiểm tra read-only ngày 19/11/2026 xác nhận database ONLINE, có đủ hai migration history, cột nullable đúng 150 ký tự Unicode và không có pending migration. Không lưu connection string vào repository và **không chạy lại migration**.

Không chạy `InitialCreate.sql`, không xóa/tạo lại database và không ghi dữ liệu test vào database thật.

## 5. Khắc phục runtime Prompt 03.5.1

Query danh sách ban đầu project vào positional record `UserRow` rồi mới sắp xếp. EF Core SQL Server không dịch được biểu thức này, khiến API trả lỗi trước khi MVC render danh sách. MVC sau đó lại gọi `View("Error")` trong khi không có view phù hợp, nên lỗi `view not found` che mất nguyên nhân gốc.

Projection đã chuyển sang member-init class mà EF Core dịch được. MVC dùng shared error state có model riêng và giữ nguyên status 400/404/409/500/502/503; 401 kết thúc session, 403 chuyển access denied. Error state không hiển thị stack trace, token hoặc chi tiết nội bộ.

## 6. Kiểm thử và giới hạn

Automated test ghi dữ liệu chỉ dùng service/client test double, không ghi database chính thức. Test SQL Server opt-in chỉ chạy query read-only, bao gồm list/search/filter/sort/pagination/aggregate và fallback khi `StaffFullName` null. Tạo/sửa/kích hoạt/vô hiệu hóa/mở khóa trên tài khoản thật vẫn **NOT VERIFIED** vì không được ghi database phát triển. HTTPS và nghiệm thu trình duyệt với Admin thật cần sinh viên kiểm tra thủ công.
