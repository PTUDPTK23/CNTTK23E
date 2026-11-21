# Đối chiếu yêu cầu Đề tài 09

## 1. Nguồn và giới hạn xác minh

Nguồn chính thức đã truy cập là `README.md` trên nhánh `upstream/main` của repository lớp tại commit `d7ada987025fa087d6290ddbc7e31903260edae7` ngày 18/11/2026.

Nguồn này xác nhận:

- Đặng Quốc Trung, MSSV `dtc245200016` được giao đề tài **09 – Hệ thống đặt lịch khám và quản lý hàng đợi**.
- Đường dẫn nộp bắt đầu bằng `Đặng Quốc Trung - dtc245200016/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/`.
- Dự án kéo dài 9 tuần; mỗi tuần có thư mục và Pull Request riêng.

README chính thức không công bố đặc tả chi tiết về tám nhóm chức năng, sáu REST API tối thiểu, RabbitMQ hoặc SignalR. Không tìm thấy tài liệu chính thức chi tiết hơn trong repository lớp hay nguồn công khai đã truy cập.

> **OFFICIAL TOPIC 09 REQUIREMENTS – NOT VERIFIED:** các yêu cầu chi tiết bên dưới là baseline nội bộ của MediQueue được hình thành từ tài liệu phân tích đã thống nhất, chưa thể gắn nhãn “yêu cầu trực tiếp của giảng viên” cho đến khi có đặc tả chính thức.

## 2. Truy vết quy định đã xác minh chính thức

| Mã | Quy định đã xác minh | Bằng chứng trong dự án | Trạng thái |
|---|---|---|---|
| OFF-01 | Đúng sinh viên và MSSV | README này; metadata trong tài liệu | Đạt |
| OFF-02 | Đúng đề tài 09 và tên đề tài | README, `docs/requirements.md` | Đạt |
| OFF-03 | Đúng đường dẫn dự án | `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan2` | Đạt |
| OFF-04 | Phát triển 9 tuần, mỗi tuần một thư mục/PR | `docs/roadmap.md`; cấu trúc `Tuan1`, `Tuan2` | Đạt trong phạm vi hiện tại |

## 3. Baseline chức năng chi tiết của MediQueue

Ký hiệu trạng thái:

- **Đã triển khai:** có mã chạy và test tự động trong Tuần 2.
- **Nền tảng:** có Domain/EF/schema hoặc thiết kế nhưng chưa có luồng nghiệp vụ hoàn chỉnh.
- **Chưa triển khai:** được giữ trong roadmap, không xuất hiện như chức năng sẵn có trên UI.
- **Chưa xác minh chính thức:** chưa đối chiếu được với đặc tả chi tiết của giảng viên.

| Nhóm baseline | Yêu cầu nội bộ | Hiện trạng Tuần 2 | Bằng chứng | Kế hoạch | Nguồn chính thức |
|---|---|---|---|---|---|
| 1. Tra cứu bác sĩ/slot | Tìm Doctor/phòng và slot còn chỗ | Quản trị Doctor/Department/Room đã triển khai; truy vấn slot chưa có | `DoctorsController`, `DoctorManagementService`, entity `TimeSlot` | Tuần 3 | Chưa xác minh |
| 2. Đặt/hủy lịch | Tạo, xem, hủy Appointment đúng quyền và capacity | Mới có entity/configuration/state; chưa có API/MVC nghiệp vụ | `Domain/Appointments`, `MediQueueDbContext` | Tuần 3–4 | Chưa xác minh |
| 3. Yêu cầu cấp số | API ghi outbox và phát yêu cầu qua RabbitMQ | Có schema Outbox/Inbox; chưa có publisher nghiệp vụ | `OutboxMessage`, `InboxMessage` | Tuần 4–5 | Chưa xác minh |
| 4. Worker cấp số | QueueNumber riêng Doctor/ngày, idempotent | Có entity/constraint nền tảng; worker chưa xử lý message | `DailyQueue`, `QueueTicket`, `MediQueue.Queue.Worker` | Tuần 5 | Chưa xác minh |
| 5. Phục vụ hàng đợi | Doctor gọi, hoàn thành, NoShow; một lượt Serving | Có state/filtered index nền tảng; chưa có command API | `QueueTicket`, migration `InitialCreate` | Tuần 6 | Chưa xác minh |
| 6. Queue event | Phát `QueueChanged` qua RabbitMQ | Chưa triển khai message/publisher | Outbox schema; `docs/api.md` | Tuần 5–6 | Chưa xác minh |
| 7. Realtime | Notification Service đẩy SignalR có xác thực group | Mới có host và health; chưa có consumer/Hub | `MediQueue.Notification.Service` | Tuần 6 | Chưa xác minh |
| 8. Tra cứu lịch/queue | Snapshot có phân quyền, không lộ Patient khác | Chưa có query API nghiệp vụ | Thiết kế trong `docs/api.md` | Tuần 4–7 | Chưa xác minh |

Không nhóm nào trong tám nhóm trên được tuyên bố hoàn thành end-to-end ở Tuần 2.

## 4. Sáu REST API tối thiểu trong baseline nội bộ

| Route dự kiến | Trạng thái | Ghi chú |
|---|---|---|
| `GET /doctors` | Chưa triển khai đúng contract công khai tối thiểu | Hiện có `GET /api/doctors` dành cho luồng quản trị/hồ sơ, không thay thế contract tra cứu slot của Patient |
| `GET /doctors/{id}/slots` | Chưa triển khai | Entity `TimeSlot` đã có |
| `POST /appointments` | Chưa triển khai | Entity/EF configuration đã có |
| `DELETE /appointments/{id}` | Chưa triển khai | Quy tắc hủy đã được thiết kế |
| `GET /appointments/{id}` | Chưa triển khai | Cần Role + ownership/scope |
| `GET /queues/{doctorId}` | Chưa triển khai | Cần snapshot và bảo vệ dữ liệu cá nhân |

Sáu route này vẫn được giữ trong `docs/api.md` và `docs/roadmap.md`; không dùng các endpoint quản trị Tuần 2 để báo cáo thay thế.

## 5. Chức năng thực tế của Tuần 2

| Phạm vi | Mã/API chính | Kiểm thử |
|---|---|---|
| Authentication/Profile | `/api/account/*`, MVC `/account/*` | Unit + API/MVC integration |
| Dashboard bốn vai trò | `/api/dashboard`, MVC `/dashboard` | Authorization và Web flow |
| User management | `/api/users`, MVC `/Users` | API, MVC, query/validation/security |
| Department/Room | `/api/departments`, `/api/rooms`, MVC tương ứng | API, MVC, dependency rules |
| Doctor/Patient profile | `/api/doctors`, `/api/patients`, MVC tương ứng | API, MVC, ownership/concurrency |

Các endpoint trong bảng này là chức năng quản trị và hồ sơ đã triển khai; chúng không chứng minh hệ thống đặt lịch/hàng đợi đã hoàn tất.

## 6. Điều kiện cập nhật

Khi giảng viên cung cấp đặc tả chi tiết, cần:

1. Lưu đường dẫn/commit/ngày của nguồn chính thức.
2. Đối chiếu từng yêu cầu với baseline hiện tại.
3. Sửa nhãn nguồn và khoảng trống; không tự thêm hoặc bỏ yêu cầu.
4. Cập nhật `requirements.md`, `api.md`, `roadmap.md` và báo cáo nghiệm thu tương ứng.

