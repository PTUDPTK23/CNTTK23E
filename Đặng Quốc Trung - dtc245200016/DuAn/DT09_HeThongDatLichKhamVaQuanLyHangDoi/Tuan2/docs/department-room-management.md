# Quản lý chuyên khoa và phòng khám

## 1. Phạm vi

Prompt 03.6 triển khai hai danh mục nền tảng dành riêng cho `Admin`: Department (chuyên khoa) và Room (phòng khám). Module hỗ trợ tìm kiếm, lọc trạng thái, sắp xếp, phân trang, xem chi tiết, tạo, cập nhật, kích hoạt và vô hiệu hóa. Không có thao tác xóa vật lý.

Không tạo migration mới và không thay đổi schema. Database chính thức vẫn là `MediQueue` trên `LAPTOP-AQFGUM9Q\SQLEXPRESS`. Khảo sát read-only xác nhận bảng, cột, unique index và foreign key hiện có đáp ứng module.

## 2. Quy tắc nghiệp vụ

- Code được trim, chuẩn hóa chữ hoa, tối đa 32 ký tự và chỉ gồm chữ, số, `_`, `-`.
- Department Name và Room Name bắt buộc, tối đa 200 ký tự; Room Location tùy chọn, tối đa 300 ký tự.
- Code là duy nhất theo schema. Service còn ngăn trùng Name để tránh danh mục khó phân biệt.
- Department không thể vô hiệu hóa khi còn Doctor `IsActive = true` thuộc chuyên khoa.
- Room không thể vô hiệu hóa khi còn DoctorSchedule hoặc TimeSlot `IsActive = true` liên kết với phòng.
- Kích hoạt lại được phép. Vô hiệu hóa không cascade, không chuyển quan hệ và không xóa dữ liệu.
- Tạo, sửa, đổi trạng thái và ghi `AuditLog` nằm trong cùng transaction. Isolation `Serializable` giảm lost update/race trong schema chưa có rowversion cho hai bảng này; unique index là lớp bảo vệ cuối cho Code.

## 3. API Admin-only

| Method | Endpoint | Mục đích |
|---|---|---|
| GET | `/api/departments` | Search/status/sort/page/pageSize và summary |
| GET | `/api/departments/{id}` | Chi tiết và số bác sĩ liên kết |
| POST | `/api/departments` | Tạo chuyên khoa |
| PUT | `/api/departments/{id}` | Cập nhật chuyên khoa |
| POST | `/api/departments/{id}/activate` | Kích hoạt |
| POST | `/api/departments/{id}/deactivate` | Vô hiệu hóa có kiểm tra phụ thuộc |
| GET | `/api/rooms` | Search/status/sort/page/pageSize và summary |
| GET | `/api/rooms/{id}` | Chi tiết và số lịch/khung giờ liên kết |
| POST | `/api/rooms` | Tạo phòng |
| PUT | `/api/rooms/{id}` | Cập nhật phòng |
| POST | `/api/rooms/{id}/activate` | Kích hoạt |
| POST | `/api/rooms/{id}/deactivate` | Vô hiệu hóa có kiểm tra phụ thuộc |

API trả `ProblemDetails`/`ValidationProblemDetails`; 400 cho input/query không hợp lệ, 401/403 cho xác thực/phân quyền, 404 khi không tồn tại và 409 khi trùng hoặc bị ràng buộc nghiệp vụ. Không có endpoint `DELETE`.

## 4. MVC và bảo mật

Web không truy cập DbContext. `ICatalogApiClient` gọi API server-to-server và gắn JWT từ session store vào từng request. Token không xuất hiện trong HTML, JavaScript, URL hoặc browser storage. `DepartmentsController` và `RoomsController` yêu cầu role Admin; mọi command MVC dùng POST và antiforgery.

Các route MVC là `/Departments` và `/Rooms`, mỗi module có Index/Create/Edit/Details. UI dùng design token hiện hành, Be Vietnam Pro, responsive table-to-card, focus state, reduced motion, empty/error/confirmation state và menu Admin có trạng thái active.

## 5. Kiểm thử và vận hành

- Unit test: invariant/normalize/state của entity và DataAnnotations/whitelist contract.
- API integration test: anonymous 401, Patient 403, Admin list/create/change state, validation 400; service được fake nên không ghi database.
- MVC integration test: anonymous redirect, Patient access denied, Admin render danh sách và asset; kiểm tra không lộ JWT.
- Test SQL chính thức chỉ được chạy read-only theo opt-in; không dùng database phát triển để kiểm thử command.
- Dịch vụ không tự migrate khi khởi động và module không retry command HTTP.

## 6. Giới hạn đã biết

Department và Room chưa có rowversion trong schema hiện tại. Transaction `Serializable`, kiểm tra phụ thuộc trong transaction và unique index bảo vệ các thao tác Prompt 03.6; nếu tương lai cần optimistic concurrency xuyên nhiều instance ở mức entity thì phải review migration riêng, không tự thêm trong prompt này.
