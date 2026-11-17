# Thiết kế cơ sở dữ liệu EF Core Code First

## 1. Trạng thái và nguyên tắc

Mô hình Code First được triển khai trong `MediQueue.Infrastructure`. Migration `20261117142359_InitialCreate` đã được áp dụng thành công vào database `MediQueue` trên `LAPTOP-AQFGUM9Q\SQLEXPRESS` ngày 18/11/2026 sau khi sửa lỗi filtered index SQL Server 10611.

Các quyết định kỹ thuật:

- Khóa Identity và các entity dùng `uniqueidentifier`/`Guid`; khóa message/idempotency dùng khóa đơn hoặc composite theo định danh nghiệp vụ.
- Chỉ có một thực thể `Department` cho chuyên khoa; không tạo `Specialty` song song.
- Enum `AppointmentStatus` và `QueueTicketStatus` lưu chuỗi không Unicode để dễ review, ổn định khi bổ sung code và phục vụ filtered index.
- Timestamp kỹ thuật dùng `datetimeoffset(0)` và phải ghi UTC; `DailyQueues.ServiceDate` dùng `date`, được tính từ `Asia/Ho_Chi_Minh` trước khi lưu.
- `rowversion` bảo vệ các aggregate cạnh tranh: Patient, Doctor, TimeSlot, Appointment, DailyQueue, QueueTicket.
- Mọi quan hệ nghiệp vụ dùng `Restrict`/`NoAction`; cascade chỉ tồn tại trong các bảng phụ chuẩn của ASP.NET Core Identity.
- Không seed tài khoản, mật khẩu, Role hoặc dữ liệu người thật trong Migration.

## 2. ERD khớp với model và Migration

```mermaid
erDiagram
    ASP_NET_USERS ||--o| PATIENTS : owns
    ASP_NET_USERS ||--o| DOCTORS : owns
    DEPARTMENTS ||--o{ DOCTORS : classifies
    DOCTORS ||--o{ DOCTOR_SCHEDULES : has
    ROOMS ||--o{ DOCTOR_SCHEDULES : hosts
    DOCTOR_SCHEDULES ||--o{ TIME_SLOTS : generates
    DOCTORS ||--o{ TIME_SLOTS : offers
    ROOMS ||--o{ TIME_SLOTS : hosts
    PATIENTS ||--o{ APPOINTMENTS : books
    TIME_SLOTS ||--o{ APPOINTMENTS : contains
    APPOINTMENTS ||--o| QUEUE_TICKETS : receives
    DOCTORS ||--o{ DAILY_QUEUES : operates
    DAILY_QUEUES ||--o{ QUEUE_TICKETS : contains
    DOCTORS ||--o{ QUEUE_TICKETS : serves
    QUEUE_TICKETS ||--o{ QUEUE_TRANSITIONS : records
    ASP_NET_USERS ||--o{ IDEMPOTENCY_REQUESTS : submits
    ASP_NET_USERS ||--o{ AUDIT_LOGS : performs

    ASP_NET_USERS {
        uniqueidentifier Id PK
        nvarchar UserName
        nvarchar Email
        nvarchar PasswordHash
        bit IsActive
        datetimeoffset CreatedAtUtc
    }
    DEPARTMENTS {
        uniqueidentifier Id PK
        varchar Code UK
        nvarchar Name
        bit IsActive
    }
    PATIENTS {
        uniqueidentifier Id PK
        uniqueidentifier UserId FK_UK
        nvarchar FullName
        date DateOfBirth
        varchar PhoneNumber
        bit IsActive
        rowversion RowVersion
    }
    DOCTORS {
        uniqueidentifier Id PK
        uniqueidentifier UserId FK_UK
        uniqueidentifier DepartmentId FK
        nvarchar DisplayName
        bit IsActive
        rowversion RowVersion
    }
    ROOMS {
        uniqueidentifier Id PK
        varchar Code UK
        nvarchar Name
        nvarchar Location
        bit IsActive
    }
    DOCTOR_SCHEDULES {
        uniqueidentifier Id PK
        uniqueidentifier DoctorId FK
        uniqueidentifier RoomId FK
        int DayOfWeek
        time StartTime
        time EndTime
        bit IsActive
    }
    TIME_SLOTS {
        uniqueidentifier Id PK
        uniqueidentifier DoctorScheduleId FK
        uniqueidentifier DoctorId FK
        uniqueidentifier RoomId FK
        datetimeoffset StartAtUtc
        datetimeoffset EndAtUtc
        int Capacity
        int BookedCount
        bit IsActive
        rowversion RowVersion
    }
    APPOINTMENTS {
        uniqueidentifier Id PK
        uniqueidentifier TimeSlotId FK
        uniqueidentifier PatientId FK
        uniqueidentifier CreatedByUserId FK
        varchar Status
        datetimeoffset CreatedAtUtc
        datetimeoffset UpdatedAtUtc
        datetimeoffset CheckedInAtUtc
        uniqueidentifier CheckedInByUserId FK
        datetimeoffset CancelledAtUtc
        datetimeoffset NoShowAtUtc
        nvarchar CancellationReason
        rowversion RowVersion
    }
    DAILY_QUEUES {
        uniqueidentifier Id PK
        uniqueidentifier DoctorId FK
        date ServiceDate
        int LastIssuedNumber
        uniqueidentifier CurrentTicketId FK
        bigint Version
        rowversion RowVersion
    }
    QUEUE_TICKETS {
        uniqueidentifier Id PK
        uniqueidentifier DailyQueueId FK
        uniqueidentifier DoctorId FK
        date ServiceDate
        uniqueidentifier AppointmentId FK_UK
        int QueueNumber
        varchar Status
        datetimeoffset IssuedAtUtc
        datetimeoffset ServingAtUtc
        datetimeoffset CompletedAtUtc
        datetimeoffset CancelledAtUtc
        datetimeoffset NoShowAtUtc
        rowversion RowVersion
    }
    QUEUE_TRANSITIONS {
        uniqueidentifier Id PK
        uniqueidentifier QueueTicketId FK
        varchar FromStatus
        varchar ToStatus
        uniqueidentifier PerformedByUserId FK
        datetimeoffset OccurredAtUtc
        nvarchar Reason
    }
    OUTBOX_MESSAGES {
        uniqueidentifier Id PK
        varchar Type
        nvarchar Payload
        uniqueidentifier CorrelationId
        datetimeoffset OccurredAtUtc
        datetimeoffset PublishedAtUtc
        datetimeoffset NextAttemptAtUtc
        int RetryCount
        nvarchar LastError
    }
    INBOX_MESSAGES {
        uniqueidentifier MessageId PK
        varchar ConsumerName PK
        datetimeoffset ProcessedAtUtc
        nvarchar Result
    }
    IDEMPOTENCY_REQUESTS {
        uniqueidentifier UserId PK_FK
        uniqueidentifier IdempotencyKey PK
        varchar RequestHash
        int ResponseStatusCode
        uniqueidentifier ResourceId
        datetimeoffset CreatedAtUtc
    }
    AUDIT_LOGS {
        uniqueidentifier Id PK
        uniqueidentifier ActorUserId FK
        varchar Action
        varchar ResourceType
        varchar ResourceId
        datetimeoffset OccurredAtUtc
        nvarchar Metadata
    }
```

Các bảng Identity còn lại: `AspNetRoles`, `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`. Không có bảng Users/Roles tự xây dựng trùng lặp.

## 3. Data Dictionary

| Bảng | Cột nghiệp vụ chính | Nullable | Mục đích |
|---|---|---|---|
| `AspNetUsers` | `Id`, `UserName`, `Email`, `PasswordHash`, `IsActive`, `CreatedAtUtc` | Các cột chuẩn Identity theo contract; hai cột mở rộng required | Một tài khoản chung cho bốn Role |
| `Departments` | `Id`, `Code`, `Name`, `IsActive` | Không | Danh mục chuyên khoa duy nhất |
| `Patients` | `Id`, `UserId`, `FullName`, `DateOfBirth`, `PhoneNumber`, `IsActive`, `RowVersion` | Ngày sinh/số điện thoại có thể null | Hồ sơ Patient tối thiểu, không lưu bệnh án |
| `Doctors` | `Id`, `UserId`, `DepartmentId`, `DisplayName`, `IsActive`, `RowVersion` | Không | Hồ sơ Doctor và chuyên khoa bắt buộc |
| `Rooms` | `Id`, `Code`, `Name`, `Location`, `IsActive` | `Location` | Phòng khám trong một cơ sở |
| `DoctorSchedules` | `DoctorId`, `RoomId`, `DayOfWeek`, `StartTime`, `EndTime`, `IsActive` | Không | Quy tắc lịch định kỳ |
| `TimeSlots` | `DoctorScheduleId`, `DoctorId`, `RoomId`, `StartAtUtc`, `EndAtUtc`, `Capacity`, `BookedCount`, `IsActive`, `RowVersion` | Không | Slot cụ thể và sức chứa |
| `Appointments` | `TimeSlotId`, `PatientId`, `CreatedByUserId`, `Status`, các timestamp trạng thái, check-in, `RowVersion` | Check-in/hủy/NoShow/lý do tùy trạng thái | Lịch đã giữ chỗ; nguồn trạng thái lịch |
| `DailyQueues` | `DoctorId`, `ServiceDate`, `LastIssuedNumber`, `CurrentTicketId`, `Version`, `RowVersion` | `CurrentTicketId` | Counter chỉ tăng và version queue Doctor/ngày |
| `QueueTickets` | `DailyQueueId`, `DoctorId`, `ServiceDate`, `AppointmentId`, `QueueNumber`, `Status`, timestamp, `RowVersion` | Timestamp theo trạng thái | Số thứ tự bất đồng bộ; nguồn trạng thái ticket |
| `QueueTransitions` | `QueueTicketId`, `FromStatus`, `ToStatus`, `PerformedByUserId`, `OccurredAtUtc`, `Reason` | Người thực hiện/lý do có thể null | Audit chuyển trạng thái append-only |
| `OutboxMessages` | `Type`, `Payload`, `CorrelationId`, thời điểm, retry/error | Published/retry/error nullable | Transactional outbox |
| `InboxMessages` | `MessageId`, `ConsumerName`, `ProcessedAtUtc`, `Result` | `Result` | Deduplication theo từng consumer |
| `IdempotencyRequests` | `UserId`, `IdempotencyKey`, `RequestHash`, response/resource, `CreatedAtUtc` | `ResourceId` | Chống HTTP command lặp |
| `AuditLogs` | Actor, action, resource, timestamp, metadata | Actor/metadata có thể null | Nhật ký quản trị/hỗ trợ, không chứa secret |

## 4. Primary Key, Foreign Key và quan hệ

- PK `Guid`: toàn bộ entity nghiệp vụ, `ApplicationUser`, Role và Outbox/Audit.
- PK composite: `InboxMessages(MessageId, ConsumerName)`, `IdempotencyRequests(UserId, IdempotencyKey)` và các bảng liên kết Identity.
- One-to-one: `AspNetUsers–Patients`, `AspNetUsers–Doctors`, `Appointments–QueueTickets` (0..1).
- One-to-many: Department–Doctors; Doctor/Room–Schedules; Schedule/Doctor/Room–TimeSlots; Patient/TimeSlot–Appointments; Doctor–DailyQueues; DailyQueue/Doctor–QueueTickets; QueueTicket–QueueTransitions.
- QueueTicket dùng composite FK `(DailyQueueId, DoctorId, ServiceDate)` tới alternate key tương ứng của DailyQueue, bảo đảm ticket không lệch Doctor/ngày khỏi queue.
- `DailyQueues.CurrentTicketId` là FK `NoAction` tới QueueTicket để tránh multiple cascade paths; service phải bảo đảm current ticket thuộc đúng DailyQueue.
- User FKs cho created/check-in/transition/idempotency/audit dùng `Restrict` để giữ lịch sử.

## 5. Unique index và index tra cứu

| Tên | Cột/filter | Ý nghĩa |
|---|---|---|
| `UX_Departments_Code` | `Code` | Không trùng mã chuyên khoa |
| `UX_Rooms_Code` | `Code` | Không trùng mã phòng |
| `UX_Patients_UserId` | `UserId` | Một user tối đa một Patient |
| `UX_Doctors_UserId` | `UserId` | Một user tối đa một Doctor |
| `UX_DailyQueues_Doctor_ServiceDate` | `DoctorId, ServiceDate` | Một queue mỗi Doctor/ngày |
| `UX_QueueTickets_AppointmentId` | `AppointmentId` | Một Appointment tối đa một ticket |
| `UX_QueueTickets_Doctor_ServiceDate_Number` | `DoctorId, ServiceDate, QueueNumber` | Không cấp trùng số Doctor/ngày |
| `UX_QueueTickets_Doctor_Serving` | `DoctorId`, filter `Status='Serving'` | Một Doctor tối đa một ticket Serving |
| `IX_Doctors_DepartmentId_IsActive` | Department/trạng thái | Tra cứu Doctor theo chuyên khoa |
| `IX_TimeSlots_Doctor_Start_IsActive` | Doctor/thời gian/trạng thái | Tra cứu slot theo Doctor và ngày |
| `IX_Appointments_Patient_Status_TimeSlot` | Patient/trạng thái/slot | Tra cứu lịch và hỗ trợ overlap query |
| `IX_QueueTickets_Doctor_Date_Status_Number` | Doctor/ngày/trạng thái/số | Snapshot và thứ tự queue |
| `IX_OutboxMessages_Pending` | thời điểm, filter chưa publish | Outbox publisher scan |
| PK `InboxMessages` | message/consumer | Chống xử lý message trùng |

## 6. Check constraint và concurrency token

Migration tạo 12 check constraint:

- `CK_DoctorSchedules_TimeRange`, `CK_DoctorSchedules_DayOfWeek`.
- `CK_TimeSlots_TimeRange`, `CK_TimeSlots_Capacity`, `CK_TimeSlots_BookedCount`.
- `CK_Appointments_Status` chỉ cho sáu trạng thái chính thức.
- `CK_DailyQueues_LastIssuedNumber`, `CK_DailyQueues_Version` không âm.
- `CK_QueueTickets_QueueNumber`, `CK_QueueTickets_Status`.
- `CK_OutboxMessages_RetryCount`.
- `CK_IdempotencyRequests_ResponseStatusCode`.

`RowVersion` là concurrency token trên `Patients`, `Doctors`, `TimeSlots`, `Appointments`, `DailyQueues`, `QueueTickets`. Filtered unique index là lớp bảo vệ cuối cho một Doctor/một Serving.

## 7. Phân lớp bảo vệ nghiệp vụ

### Database bảo đảm

- PK/FK/nullability, một Appointment–một QueueTicket, số duy nhất Doctor/ngày, một DailyQueue Doctor/ngày.
- Capacity/BookedCount hợp lệ tại thời điểm ghi, QueueNumber > 0, status thuộc tập cho phép.
- Một Doctor tối đa một ticket có trạng thái `Serving`.
- Inbox/idempotency key không trùng và rowversion phát hiện optimistic concurrency.

### Domain bảo đảm

- End phải sau Start, Capacity dương, không reserve vượt Capacity hoặc release dưới 0.
- Chỉ transition Appointment/QueueTicket đúng state machine.
- Không hủy tại/sau `Slot.StartAt`; check-in không đổi trạng thái.
- QueueNumber dương, định dạng `D3`; counter DailyQueue chỉ tăng.
- Ngày dịch vụ được tính theo múi giờ Việt Nam, không theo ngày UTC.

### Service/transaction phải triển khai ở giai đoạn sau

- Atomic capacity update để chống overbooking; rowversion/index riêng lẻ chưa đủ.
- Chống Appointment chồng giờ dưới request đồng thời bằng transaction/khóa phù hợp.
- Đồng bộ trạng thái Appointment và QueueTicket trong cùng transaction.
- Cấp counter, tạo ticket, Inbox và Outbox trong cùng transaction; filtered index conflict phải được chuyển thành lỗi nghiệp vụ.
- Bảo đảm `CurrentTicketId` thuộc đúng DailyQueue và thứ tự gọi theo slot rồi QueueNumber.

`Appointment.Status` là nguồn chuẩn cho vòng đời lịch/giữ chỗ; `QueueTicket.Status` là nguồn chuẩn cho vòng đời hàng đợi. Từ khi ticket được cấp, các transition chung (`Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`) phải được service cập nhật đồng bộ trong cùng transaction; schema riêng lẻ không thể tự bảo đảm quy tắc xuyên hai aggregate.

## 8. Identity và bốn Role

`ApplicationUser : IdentityUser<Guid>` bổ sung `IsActive`, `CreatedAtUtc`. `MediQueueDbContext` kế thừa `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`. Patient và Doctor liên kết one-to-one bằng `UserId`; Receptionist/Admin không cần bảng hồ sơ riêng ở phạm vi hiện tại. Bốn Role sẽ được quản lý bằng bảng Identity chuẩn, chưa seed và chưa triển khai Login/Register/JWT trong Prompt 02.2.

## 9. Migration và SQL review

- Migration: `src/MediQueue.Infrastructure/Persistence/Migrations/*_InitialCreate.cs`.
- Designer và snapshot: cùng thư mục Migration.
- SQL idempotent: `docs/sql/InitialCreate.sql`; script xác minh read-only: `docs/sql/VerifyDatabase.sql`.
- Script có guard `__EFMigrationsHistory`, transaction, 21 bảng ứng dụng/Identity và bảng lịch sử Migration nếu được chạy; không chứa `CREATE DATABASE`.
- `Down` tháo các FK vòng trước rồi drop bảng theo thứ tự phụ thuộc.
- `dotnet ef migrations has-pending-model-changes` xác nhận snapshot khớp model; `migrations list` xác nhận không còn pending Migration.
- `InitialCreate.sql` chỉ dùng review/khôi phục có kiểm soát; không thực thi lại sau khi Migration đã áp dụng. Runtime không gọi `EnsureCreated`, `EnsureDeleted` hoặc `Database.Migrate`.

## 10. Kết quả database thực tế

Trước Migration, database trống; sau lần lỗi 10611 chỉ cò `__EFMigrationsHistory` rỗng và không có schema dở dang. Sau bản sửa, kiểm tra system catalog xác nhận:

- 21 bảng mô hình + `__EFMigrationsHistory` = 22 bảng.
- 22 PK, 27 FK, 36 index ngoài PK, 11 unique index, 4 filtered index.
- 12 check constraint, 1 default constraint, 6 cột `rowversion`.
- Tất cả FK/check constraint đều enabled và trusted.
- `UX_QueueTickets_Doctor_Serving` unique, enabled, filter `([Status]='Serving')` trên `Status varchar(32)`.
- `__EFMigrationsHistory` có đúng một dòng `20261117142359_InitialCreate`, EF Core `10.0.2`.

Chi tiết bằng chứng và lệnh kiểm tra xem `docs/database-validation.md`.
