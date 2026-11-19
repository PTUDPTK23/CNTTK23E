# Xác minh Initial Migration trên SQL Server

> Tài liệu này bảo toàn bằng chứng Tuần 1 từ commit `dcee864`. Trong Prompt 03.1.1, `MediQueue` chỉ được phép kiểm tra metadata read-only; không chạy lại Migration/script và không ghi dữ liệu. Database đề xuất `MediQueue_Tuan2` chưa được tạo.

Kiểm tra lại read-only ngày 19/11/2026 xác nhận `MediQueue` vẫn `ONLINE`, có 22 user table/21 model table, đúng một dòng `20261117142359_InitialCreate`, không có FK/check bị disabled hoặc untrusted. `DB_ID('MediQueue_Tuan2')` trả `NULL` cả trước và sau smoke test; không database mới nào được tạo.

## 1. Phạm vi và môi trường

- Ngày kiểm tra: 18/11/2026, múi giờ Việt Nam.
- Server: `LAPTOP-AQFGUM9Q\SQLEXPRESS`; SQL Server Express `16.0.1000.6`.
- Database: `MediQueue`, `ONLINE`, compatibility level `160`, Windows Authentication.
- EF Core: SQL Server provider `10.0.2`; Migration `20261117142359_InitialCreate`.
- Phương pháp: EF Core Code First; không dùng `EnsureCreated`, Docker, SQL tạo bảng thủ công hoặc runtime auto-migration.

Trước lần áp dụng đầu, `MediQueue` có 0 user table. Sau lần lỗi, giao dịch tạo schema đã rollback; chỉ `dbo.__EFMigrationsHistory` rỗng do EF tạo trước giao dịch. Không xóa/tạo lại database hoặc sửa Migration History.

## 2. Lỗi SQL Server 10611 và bản sửa

Lần `database update` đầu thất bại khi tạo `UX_QueueTickets_Doctor_Serving`. `QueueTickets.Status` được enum-convert thành `varchar(32)`, nhưng filter dùng hằng Unicode `N'Serving'` (`nvarchar`). SQL Server không cho phép chuyển kiểu cột trong biểu thức filtered index và trả error 10611.

Bản sửa tối thiểu:

```csharp
.HasFilter("[Status] = 'Serving'")
```

Kiểu `varchar(32)`, enum conversion, tên/cột/thuộc tính unique của index và quy tắc một Doctor tối đa một ticket `Serving` được giữ nguyên. Fluent API, InitialCreate, Designer, ModelSnapshot được đồng bộ; `InitialCreate.sql` được sinh lại bằng EF CLI. Các filter còn lại chỉ kiểm tra `IS NULL` và không có xung đột `varchar`/`nvarchar`.

## 3. Lệnh EF Core thực tế

Sau restore/build/test, `has-pending-model-changes` và tiền kiểm database:

```powershell
dotnet ef database update 20261117142359_InitialCreate `
  --project .\src\MediQueue.Infrastructure\MediQueue.Infrastructure.csproj `
  --startup-project .\src\MediQueue.Appointment.Api\MediQueue.Appointment.Api.csproj `
  --context MediQueueDbContext `
  --no-build
```

Kết quả: EF Core ghi `Applying migration '20261117142359_InitialCreate'.` và `Done.`. `InitialCreate.sql` không được chạy độc lập sau Migration.

## 4. Schema thực tế

21 bảng mô hình và 1 bảng lịch sử, tổng 22:

```text
__EFMigrationsHistory  Appointments          AspNetRoleClaims
AspNetRoles            AspNetUserClaims      AspNetUserLogins
AspNetUserRoles        AspNetUsers           AspNetUserTokens
AuditLogs              DailyQueues           Departments
Doctors                DoctorSchedules       IdempotencyRequests
InboxMessages          OutboxMessages        Patients
QueueTickets           QueueTransitions      Rooms
TimeSlots
```

Kết quả system catalog:

| Hạng mục | Số lượng |
|---|---:|
| Primary Key | 22 |
| Foreign Key | 27 |
| Index ngoài Primary Key | 36 |
| Unique Index/alternate key ngoài PK | 11 |
| Filtered Index | 4 |
| Check Constraint | 12 |
| Default Constraint | 1 |
| Cột `rowversion` | 6 |
| FK/check disabled hoặc untrusted | 0 |

Mỗi bảng có một PK. PK composite đáng chú ý: `AspNetUserLogins(LoginProvider, ProviderKey)`, `AspNetUserRoles(UserId, RoleId)`, `AspNetUserTokens(UserId, LoginProvider, Name)`, `IdempotencyRequests(UserId, IdempotencyKey)` và `InboxMessages(MessageId, ConsumerName)`.

27 FK gồm 6 FK phụ chuẩn Identity dùng `CASCADE` và 21 FK nghiệp vụ/Identity profile dùng `NO_ACTION`. Nhóm nghiệp vụ liên kết Appointment–Patient–TimeSlot–User, Doctor–Department–User, Schedule–Doctor–Room, TimeSlot–Schedule–Doctor–Room, DailyQueue–Doctor–CurrentTicket, QueueTicket–Appointment–DailyQueue–Doctor, QueueTransition–Ticket–User và audit/idempotency–User. Danh sách tên/quan hệ/delete behavior đầy đủ được truy xuất bởi `VerifyDatabase.sql`.

Unique/filtered index chính:

- `UX_DailyQueues_Doctor_ServiceDate`, `UX_Departments_Code`, `UX_Doctors_UserId`, `UX_Patients_UserId`, `UX_Rooms_Code`.
- `UX_QueueTickets_AppointmentId`, `UX_QueueTickets_Doctor_ServiceDate_Number`.
- `UX_QueueTickets_Doctor_Serving`: unique, enabled, filter `([Status]='Serving')`, key `DoctorId`; `Status` là `varchar(32)`.
- Identity `RoleNameIndex`/`UserNameIndex` là filtered unique index; `IX_OutboxMessages_Pending` là filtered non-unique index.

12 check constraint khóa time range, capacity/booked count, Appointment/QueueTicket status, QueueNumber, DailyQueue counter/version, retry count và HTTP response status. Default constraint duy nhất đặt `AspNetUsers.IsActive = 1`. `RowVersion` tồn tại trên `Appointments`, `DailyQueues`, `Doctors`, `Patients`, `QueueTickets`, `TimeSlots`.

## 5. Migration History, model và DbContext

- `dbo.__EFMigrationsHistory`: một dòng `20261117142359_InitialCreate`, `ProductVersion = 10.0.2`.
- `dotnet ef migrations list`: `InitialCreate` đã áp dụng; không còn pending Migration.
- `dotnet ef migrations has-pending-model-changes`: `No changes have been made to the model since the last migration.`
- Integration test dùng connection string từ `ConnectionStrings__MediQueue`, `CanConnectAsync`, `GetPendingMigrationsAsync` và `AsNoTracking().LongCountAsync()` trên 14 DbSet. Tất cả truy vấn thành công, trả 0 bản ghi và không ghi dữ liệu.

## 6. Kiểm thử và smoke test

- Restore thành công; build `0` warning, `0` error.
- 34/34 test đạt: 15 unit và 19 integration, không skip.
- API `GET /health`: 200; Web `/`, `/health`, `/Home/Error`: 200; Notification `GET /health`: 200.
- Queue Worker duy trì trạng thái running trong smoke test. Bốn process thử nghiệm đã dừng.

## 7. Tự kiểm tra trong SSMS

1. Mở SSMS, chọn Windows Authentication và kết nối `LAPTOP-AQFGUM9Q\SQLEXPRESS`.
2. Mở `Databases > MediQueue > Tables`, nhấn `Refresh` và kiểm tra 22 bảng.
3. Kiểm tra `dbo.__EFMigrationsHistory` có đúng một dòng InitialCreate.
4. Mở `dbo.QueueTickets > Indexes > UX_QueueTickets_Doctor_Serving > Properties` để xem unique/filter.
5. Mở `docs/sql/VerifyDatabase.sql`, đảm bảo database dropdown là `MediQueue` rồi Execute. Script sẽ trả `ABORTED` và `RETURN` nếu chọn nhầm database.
6. Nếu cần minh chứng, chụp ảnh thật Object Explorer, Migration History và result grid summary/filtered index; không tạo ảnh giả.

## 8. Giới hạn hiện tại

Schema và khả năng kết nối EF đã được xác minh, nhưng chưa triển khai Login/Register/JWT flow, API CRUD/booking, transaction tranh chỗ, RabbitMQ publisher/consumer nghiệp vụ hoặc SignalR Hub. Rowversion/index không tự chứng minh các luồng concurrency đã hoàn chỉnh.
