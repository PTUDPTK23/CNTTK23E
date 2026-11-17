# Thiết kế cơ sở dữ liệu

Mỗi service dùng SQL Server database và EF Core migration độc lập.

## EventManagement

| Bảng | Khóa/quan hệ | Mục đích |
|---|---|---|
| Events | GUID PK; status/start index; check constraints | Thông tin, trạng thái, sức chứa và counters của sự kiện |
| Participants | GUID PK; unique normalized email | Thông tin liên hệ người tham dự |
| Registrations | GUID PK; FK Event/Participant; unique RegistrationCode; filtered unique EventId+ParticipantId cho bản ghi chưa hủy | Vé, trạng thái, QR data, thời điểm đăng ký/check-in |
| OutboxMessages | GUID message ID; pending index | Phát event nhất quán sau commit |

## EventDashboard

- DashboardStatistics: active event counter snapshots; deleted events remain as `IsDeleted` tombstones excluded from dashboard queries. snapshot counter và capacity theo EventId.
- DashboardActivities: message/event type, mã đăng ký, participant, thời gian và correlation.
- ProcessedMessages: unique EventId để chống tiêu thụ lặp trong mỗi database consumer.
- DashboardRegistrations: projection trạng thái theo RegistrationId, phục vụ đếm participant duy nhất.

## EventAudit

- AuditLogs: payload sự kiện đã nhận, participant/event/registration, description, timestamp, correlation.
- ProcessedMessages: unique EventId để chống ghi log trùng.

Các migration được lưu tại `backend/<service>/<project>/Data/Migrations`. Startup gọi `Database.MigrateAsync`. Seed chỉ chạy khi EventManagement chưa có event nào; không ghi đè dữ liệu hiện có.
