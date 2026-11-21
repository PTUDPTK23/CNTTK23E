# Quản lý bác sĩ và bệnh nhân — Prompt 03.7

## 1. Phạm vi và mô hình dữ liệu

Module dùng lại schema hiện có, không tạo Migration. `Doctor.UserId` và `Patient.UserId` đều có unique index, liên kết một-một với `ApplicationUser` và dùng `rowversion` để phát hiện cập nhật đồng thời. Mỗi Doctor thuộc đúng một Department; Room chỉ được phân công qua lịch/khung giờ ở giai đoạn sau, không tạo quan hệ Doctor–Room trực tiếp.

Tài khoản và hồ sơ nghiệp vụ là hai khái niệm riêng:

- Admin tạo tài khoản vai trò Doctor tại module người dùng, sau đó tạo hồ sơ Doctor và chọn Department đang hoạt động.
- Chỉ tài khoản Doctor đang hoạt động, chưa liên kết hồ sơ mới xuất hiện trong danh sách tạo.
- Patient tự đăng ký; `AccountService` tạo Identity user, gán role Patient và tạo Patient profile trong cùng transaction.
- Quản trị hồ sơ không đổi email, role, mật khẩu, lockout hoặc security stamp; không có xóa vật lý.

## 2. API và quyền

| Method | Route | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/doctors` | Admin | Tìm kiếm, lọc Department/trạng thái, sắp xếp, phân trang |
| GET | `/api/doctors/{id}` | Admin | Chi tiết hồ sơ và phiên bản đồng thời |
| GET | `/api/doctors/create-options` | Admin | Tài khoản Doctor chưa liên kết và Department active |
| POST | `/api/doctors` | Admin | Tạo liên kết hồ sơ duy nhất |
| PUT | `/api/doctors/{id}` | Admin | Sửa tên hiển thị/Department với rowversion |
| GET | `/api/doctors/me` | Doctor | Chỉ hồ sơ gắn với `sub` hiện tại |
| GET | `/api/patients` | Admin | Tìm kiếm, lọc trạng thái, sắp xếp, phân trang |
| GET | `/api/patients/{id}` | Admin | Chi tiết hành chính tối thiểu |
| PUT | `/api/patients/{id}` | Admin | Sửa họ tên, điện thoại, ngày sinh với rowversion |
| GET | `/api/patients/me` | Patient | Chỉ hồ sơ gắn với `sub` hiện tại |

Các response không có password hash, security stamp, token hoặc trường Identity nhạy cảm. Receptionist không có quyền vào module quản trị. API trả `ProblemDetails`/`ValidationProblemDetails`, `401`, `403`, `404`, `409` đúng ngữ nghĩa và đặt `Cache-Control: no-store`. Không cung cấp `DELETE`.

## 3. Query, concurrency và audit

- Query đọc dùng `AsNoTracking`, projection có thể dịch sang SQL, whitelist trường sort và page size `10/20/50`.
- Sort luôn có `Id` làm khóa phụ để phân trang ổn định.
- Tạo hồ sơ Doctor chạy transaction `Serializable`, đồng thời dựa vào unique index `Doctor.UserId` để chống liên kết trùng.
- Update Doctor/Patient gửi rowversion dạng Base64; dữ liệu vừa thay đổi trả `409 Conflict` và yêu cầu tải lại.
- Ngày sinh không được ở tương lai theo ngày nghiệp vụ Việt Nam.
- Tạo/sửa hồ sơ ghi `AuditLog` trong cùng transaction; metadata không chứa PII không cần thiết.

## 4. MVC và UI

Web dùng `IProfilesApiClient`; JWT chỉ nằm trong session store phía server và được gắn vào request server-to-server. Các command MVC dùng POST + antiforgery. Admin có `/Doctors` và `/Patients`; Doctor có `/Doctors/Profile`; Patient tiếp tục dùng hồ sơ cá nhân hiện có để tránh tạo luồng trùng lặp.

Giao diện dùng shared dashboard layout, search/filter/pagination phía server, empty/error/loading state, bảng responsive, focus state và `prefers-reduced-motion`. Sidebar, breadcrumb và quick action chỉ hiển thị theo role; API vẫn là lớp thực thi authorization cuối cùng.

## 5. Kiểm thử và giới hạn

- Unit test bao phủ invariant Doctor/Patient và contract paging/concurrency.
- API integration test bao phủ Admin-only, Doctor/Patient self endpoint, Receptionist bị từ chối, không lộ trường nhạy cảm và không có DELETE.
- MVC integration test bao phủ phân luồng role, static CSS, render dữ liệu và JWT không xuất hiện trong HTML.
- Test SQL thật là opt-in, chỉ query read-only. Lần chạy Prompt 03.7 không xác minh được kết nối do môi trường client không thiết lập được mã hóa; không hạ TLS và không ghi database.
- Chưa triển khai lịch làm việc, khung giờ, đặt lịch hay hàng đợi trong module này.
