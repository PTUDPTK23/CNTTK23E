# ClinicQueue — Thiết kế hệ thống đặt lịch & quản lý hàng đợi

> **Phiên bản:** 1.0 (M0 chỉ setup, không triển khai business logic M1-M6)
> **Trạng thái:** Đã duyệt, bắt đầu M0

---

## A. Yêu cầu chức năng và phi chức năng

### A.1 Chức năng — theo vai trò

| Vai trò | Chức năng | Ghi chú |
|---|---|---|
| **Patient** | Đăng ký / đăng nhập; tìm bác sĩ & phòng khám; xem khung giờ trống; đặt / hủy lịch; nhận số thứ tự; theo dõi hàng đợi realtime | Chỉ thấy hàng đợi của chính mình + trạng thái lượt của mình |
| **Staff** | Đăng nhập (chung trang); xem lịch khám của mình; xem danh sách bệnh nhân chờ; gọi lượt kế tiếp; cập nhật trạng thái khám | 1 tài khoản Staff gắn với 0..1 Doctor; Admin có thể tạo Staff chưa gắn bác sĩ |
| **Admin** | Quản lý bác sĩ, phòng khám, khung giờ (template + slot), tài khoản (tạo/khoá/reset/đổi vai trò) | Admin không tham gia nghiệp vụ đặt lịch |

Nguyên tắc: **một website, một trang đăng nhập**; backend trả về role trong token, frontend route theo role; backend luôn kiểm tra quyền ở từng endpoint (không tin role từ client).

### A.2 Phi chức năng

- **Nhất quán số thứ tự** [BẮT BUỘC]: không trùng số trong cùng phạm vi (bác sĩ, phòng khám, ngày). Số cấp tăng dần, không tái sử dụng sau khi hủy.
- **Không đặt trùng slot** [BẮT BUỘC]: 1 slot = 1 bệnh nhân; chống race bằng concurrency token/unique index.
- **Bất đồng bộ có trải nghiệm tốt** [BẮT BUỘC]: đặt lịch trả về ngay (202) + appointmentId; số thứ tự đến qua SignalR (fallback polling).
- **Tin cậy message** [BẮT BUỘC, mức thiết kế]: durable queue, publisher confirm, manual ack, DLQ, consumer idempotent (at-least-once).
- **Bảo mật**: JWT + ASP.NET Core Identity; hash mật khẩu; phân quyền theo policy; SignalR xác thực & gán group từ claim server-side; HTTPS [M1+].
- **Khả năng vận hành**: toàn bộ chạy bằng `docker compose up`; health check từng service; log có correlation [M5+].
- **Thời gian**: lưu UTC trong DB, hiển thị theo `Asia/Ho_Chi_Minh` [M4+]; "ngày phục vụ" (ServiceDate) tính theo giờ địa phương phòng khám.

---

## B. Kiến trúc tổng thể

### B.1 Topology 3 service + hạ tầng

```
                    ┌──────────────────────────────────────┐
                    │        React SPA (nginx proxy)        │
                    │  Frontend: /api → Api, /hubs → HUB   │
                    └──────────┬───────────────────┬────────┘
                               │ HTTP/REST         │ WebSocket
                               ▼                   ▼
                    ┌──────────────────┐   ┌──────────────────────┐
                    │ ClinicQueue.Api  │   │   SignalR Hub        │
                    │ (ASP.NET Core)   │   │ (trong Notification) │
                    │ Port: 5000       │   │ Port: 5001           │
                    └────────┬─────────┘   └────────┬─────────────┘
                             │ publish               │ consume
                             ▼                       ▼
                    ┌────────────────────────────────────────┐
                    │       RabbitMQ (port 5672)             │
                    │  exchanges: clinic.events (topic)      │
                    └────┬─────────────────────────┬──────────┘
                         │ consume                 │ consume
                         ▼                         ▼
                    ┌────────────────┐      ┌────────────────────┐
                    │ Worker Service │      │ Notification       │
                    │ (Worker)       │      │ Service            │
                    │ Port: (none)   │      │ (ASP.NET Core)     │
                    └────────┬───────┘      │ Port: 5001         │
                             │              └────────────────────┘
                             ▼
                    ┌──────────────────────────┐
                    │  SQL Server (port 1433)  │
                    │  Database: ClinicQueue   │
                    └──────────────────────────┘
```

### B.2 Quyết định kiến trúc chính

1. **1 database chung `ClinicQueue`** trên SQL Server: DbContext riêng cho mỗi service, migration riêng.
2. **Outbox pattern trong API**: OutboxMessages ghi cùng transaction với Appointment; background publisher đẩy RabbitMQ.
3. **API consume TicketAssigned** để cập nhật trạng thái Appointment từ `PendingTicket` → `Confirmed` + QueueNumber.
4. **Worker là single-writer** của QueueEntries: tránh race condition cấp số.
5. **SignalR xác thực & phân quyền group** server-side từ JWT claim; client không được tự chọn group.
6. **3 service độc lập**: không gọi HTTP trực tiếp trong luồng business; chỉ qua message.

---

## C. Các service và trách nhiệm

| Thành phần | Loại | Sở hữu dữ liệu | Trách nhiệm | Không được làm |
|---|---|---|---|---|
| **ClinicQueue.Api** | ASP.NET Core Web API | Appointments, OutboxMessages, Users, Clinics, Doctors (M1+) | Identity/JWT, CRUD admin, đặt & hủy lịch (M2), publish OutboxMessages, consume TicketAssigned để cập nhật Appointment, health endpoint | Sinh số thứ tự; ghi trực tiếp QueueEntries; xác thực SignalR |
| **ClinicQueue.Worker** | .NET Worker Service | QueueEntries, QueueCounters, ProcessedMessages, WorkerOutboxMessages | Consume TicketRequested → cấp số; consume AppointmentCancelled; publish TicketAssigned & QueueChanged; idempotency check; health endpoint | Không sửa Appointment trực tiếp; không tự quyết định đặt lịch |
| **ClinicQueue.Notification** | ASP.NET Core host | Không DB nghiệp vụ | Host SignalR Hub (xác thực JWT, gán group từ claim); consume TicketAssigned/QueueChanged/AppointmentCancelled → broadcast SignalR; health endpoint | Không là nguồn sự thật của queue; không ghi DB |
| **ClinicQueue.Contracts** | class library | — | Event DTO, message contract, enum trạng thái, routing key constant | Logic business |
| **clinicqueue-web** | React SPA (Vite) | — | Trang login chung, route theo role, UI patient/staff/admin, SignalR client | Logic phân quyền (backend kiểm tra) |

---

## D. Luồng dữ liệu nghiệp vụ đặt lịch (chính thức)

### D.1 Đặt lịch (happy path) — 202 Accepted + Outbox

**Bước 1-6: API xử lý request**
```
Patient: POST /api/appointments { slotId }
API:
  1. Xác thực JWT claim (patientUserId)
  2. Kiểm tra slot tồn tại, ứng với bác sĩ/phòng khám
  3. BEGIN TRANSACTION
  4. SELECT * FROM AppointmentSlots WITH (UPDLOCK, HOLDLOCK) WHERE Id = @slotId -- SQL Server transaction
  5. Kiểm tra slot.Status = Free
  6. UPDATE slot SET status = Booked WHERE Id = @slotId AND Status = Free AND RowVersion = @original -- rowversion generated by SQL Server
     → nếu 0 dòng: ROLLBACK, return 409 Conflict (slot đã có người)
  7. INSERT Appointment (status=PendingTicket, code='A7K2', patientUserId)
  8. INSERT OutboxMessage (eventType=TicketRequested, payload={appointmentId, patientUserId, doctorId, clinicId, serviceDate})
  9. COMMIT
  10. Response: 202 Accepted { appointmentId, status: "PendingTicket" }
```

**Bước 7-9: Outbox Publisher (background service trong API)**
```
Mỗi 5 giây:
  - SELECT TOP 1000 FROM OutboxMessages WHERE processedAt IS NULL ORDER BY createdAt
  - Cho mỗi message:
    * Publish lên RabbitMQ (exchange: clinic.events, routing key: ticket.requested)
    * Đánh dấu processedAt = NOW
    * UPDATE processed = 1
    (hoặc nếu error → retry, log messageId)
```

**Bước 10-14: Worker consume TicketRequested**
```
Worker RabbitMQ consumer (queue.ticket.requested):
  1. Receive message { appointmentId, patientUserId, doctorId, clinicId, serviceDate }
  2. SELECT FROM ProcessedMessages WHERE messageId = @msgId
     → nếu tồn tại: skip (idempotent), ack message, return
  3. BEGIN TRANSACTION
  4. Allocate from QueueCounters(DoctorId, ClinicId, ServiceDate, LastNumber):
     - SERIALIZABLE transaction; lock unique scope key WITH (UPDLOCK, HOLDLOCK), including missing-row key range.
     - Create counter if absent; UPDATE LastNumber = LastNumber + 1 OUTPUT INSERTED.LastNumber.
     - Never decrement counter on cancellation. Never use MAX of active tickets.
  5. INSERT QueueEntries (appointmentId, ticketNumber, status=Waiting, patientUserId, ...)
  6. INSERT ProcessedMessages (messageId) + WorkerOutboxMessages (TicketAssigned, QueueChanged)
  7. COMMIT
  8. Publish TicketAssigned { appointmentId, ticketNumber, serviceDate, waitTime, ... }
  9. Publish QueueChanged { clinicId, serviceDate, queue_snapshot, ... }
  10. RabbitMQ manual ack after commit (queue row + inbox + Worker outbox)
```

**Bước 15-18: Notification consume + SignalR broadcast**
```
Notification (2 consumers, parallel):

Consumer 1: exchange.TicketAssigned
  - Receive { appointmentId, ticketNumber, patientUserId, ... }
  - SignalR.Clients.User(patientUserId).Send("ReceiveTicketAssigned", { appointmentId, ticketNumber })

Consumer 2: exchange.QueueChanged
  - Receive { clinicId, serviceDate, queue_snapshot, ... }
  - SignalR.Clients.Group($"clinic_{clinicId}").Send("ReceiveQueueUpdate", queue_snapshot)
```

**Bước 19-21: Frontend + API polling fallback**
```
Frontend (React):
  1. POST /api/appointments { slotId }
  2. Nhận 202 + appointmentId
  3. Hiển thị "Đang cấp số..."
  4. Setup SignalR listener: hub.on("ReceiveTicketAssigned", (data) => { display number })
  5. Fallback polling: GET /api/appointments/{id} mỗi 10s, kiểm tra QueueNumber
  6. Khi có số → hiển thị, dừng polling

API GET /api/appointments/{id}:
  - SELECT Appointment.*, QueueNumber
    FROM Appointments LEFT JOIN (SELECT appointmentId, number AS QueueNumber FROM QueueEntries) ...
  - Trả { appointmentId, status, queueNumber, ... }
```

### D.2 Hủy lịch

```
Patient: DELETE /api/appointments/{id}
API:
  1. Xác thực chủ nhân (patientUserId == appointment.patientUserId)
  2. BEGIN TRANSACTION
  3. Kiểm tra status không phải Completed/NoShow/Cancelled
  4. Kiểm tra chưa quá 24h trước lịch khám [ĐỀ XUẤT M2]
  5. UPDATE Appointment SET status = Cancelled, cancelReason = ?
  6. UPDATE slot SET status = Free (slot lại trống)
  7. INSERT OutboxMessage (eventType=AppointmentCancelled, payload={appointmentId, clinicId, doctorId, serviceDate})
  8. COMMIT
  9. Response: 204 No Content

Worker consume AppointmentCancelled:
  1. Kiểm tra ProcessedMessages (idempotency)
  2. UPDATE QueueEntries SET status = Cancelled WHERE appointmentId = @id
     (số bị bỏ trống, không renumber — tránh lộn hàng đợi)
  3. INSERT ProcessedMessages
  4. Publish QueueChanged (queue update)
```

### D.3 Vòng đời trạng thái

**Appointment:**
- `PendingTicket` → `Confirmed` (khi nhận TicketAssigned) → `CheckedIn` → `InProgress` → `Completed`
- Nhánh: `Cancelled`, `NoShow`

**QueueEntry:**
- `Waiting` → `Called` → `Serving` → `Done`
- Nhánh: `Skipped` (gọi không có mặt), `Cancelled`

---

## E. Luồng RabbitMQ, Worker, SignalR

### E.1 RabbitMQ Topology

**Exchange:**
- Tên: `clinic.events`
- Loại: `topic`
- Durable: `true`
- Auto-delete: `false`

**Queues:**

| Queue | Routing Key | Consumer | Durable | TTL | DLX |
|---|---|---|---|---|---|
| `queue.ticket.requested` | `ticket.requested` | Worker | Yes | 24h | `clinic.events.dlx` |
| `queue.ticket.assigned` | `ticket.assigned` | Notification | Yes | 24h | `clinic.events.dlx` |
| `queue.ticket.assigned.api` | `ticket.assigned` | Api | Yes | 24h | `clinic.events.dlx` |
| `queue.queue.changed` | `queue.changed` | Notification | Yes | 24h | `clinic.events.dlx` |
| `queue.appointment.cancelled` | `appointment.cancelled` | Worker | Yes | 24h | `clinic.events.dlx` |

**Dead Letter Queue:**
- `clinic.events.dlx` (exchange)
- `queue.events.dlq` (queue)

### E.2 Message Format

```json
{
  "eventId": "uuid",
  "eventType": "TicketRequested",
  "timestamp": "2025-01-20T10:00:00Z",
  "payload": {
    "appointmentId": "uuid",
    "patientUserId": "uuid",
    "doctorId": "uuid",
    "clinicId": "uuid",
    "serviceDate": "2025-01-21"
  }
}
```

### E.3 Cấp số chống trùng (Worker)

Worker is the only writer of QueueEntries and QueueCounters.
Within one SQL Server SERIALIZABLE transaction:
1. Check/insert ProcessedMessages with unique messageId; handle concurrent duplicate deliveries.
2. Lock QueueCounters unique scope (DoctorId, ClinicId, ServiceDate) WITH (UPDLOCK, HOLDLOCK), including missing-row key range; insert counter if absent.
3. Increment LastNumber atomically with OUTPUT INSERTED.LastNumber.
4. Insert QueueEntry; enforce UNIQUE(AppointmentId) and UNIQUE(DoctorId, ClinicId, ServiceDate, TicketNumber).
5. Insert Worker outbox events and commit; manual ack after commit. Publish outbox with confirm and retries.
Retry deadlocks/transient conflicts with a bounded policy. Never reuse cancelled numbers, decrement counters or derive the next number from MAX.

### E.4 SignalR Security & Authorization

**Authentication:**
```csharp
// QueueHub.cs
public class QueueHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.FindFirst(ClaimTypes.NameIdentifier) == null)
        {
            Context.Abort(); // reject unauthenticated
            return;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        // Server assign group từ claim, client không được tự chọn
        if (role == "Patient")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"patient_{userId}");
        }
        else if (role == "Staff")
        {
            var doctorId = user.FindFirst("doctorId")?.Value;
            if (doctorId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
        }

        await base.OnConnectedAsync();
    }

    // Client không được gọi đây — server kiểm tra trước broadcast
    // không có [HubMethodName] public method để subscribe group tuỳ ý
}
```

**Broadcast (ở consumer TicketAssigned):**
```csharp
// NotificationService consume TicketAssigned
await hubContext.Clients.User(patientUserId).SendAsync("ReceiveTicketAssigned", ticketData);
await hubContext.Clients.Group($"doctor_{doctorId}").SendAsync("ReceiveQueueUpdate", queueSnapshot);
```

**Không cho phép:**
```csharp
// ❌ KHÔNG được làm (client có thể hack)
public async Task SubscribeToClinic(Guid clinicId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic_{clinicId}");
    // Patient có thể gọi đây với bất kỳ clinicId → leak data
}
```

---

## F. Thiết kế Database

### F.1 Schema ClinicQueue

**Nhóm Identity (sở hữu: Api, migration: Identity default)**
```sql
-- ASP.NET Core Identity tables (auto-create)
-- AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetClaims, etc.
```

**Nhóm Booking (sở hữu: Api, migration: ApiDbContext)**
```sql
-- Bảng:
Appointments (M1+)
OutboxMessages (M1+)

-- Bảng read-model (cập nhật từ Worker event):
QueueReadModel (M3+) -- để API query nhanh
```

**Nhóm Queue (sở hữu: Worker, migration: WorkerDbContext)**
```sql
-- Bảng:
QueueEntries
QueueCounters -- monotonic number per doctor/clinic/service date; never decrement on cancellation
WorkerOutboxMessages -- publish committed events reliably (M3+)
QueueSnapshots (audit history - optional M5+)
ProcessedMessages (inbox/dedup)

-- Constraints:
UNIQUE(appointmentId) -- 1 appointment chỉ cấp 1 lần
UNIQUE(doctorId, clinicId, serviceDate, ticketNumber) -- không trùng số trong ngày
```

### F.2 Table definition (high-level)

**Appointments (Api owned)**
```sql
CREATE TABLE Appointments (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PatientUserId NVARCHAR(450),
    DoctorId UNIQUEIDENTIFIER, -- (M1+)
    ClinicId UNIQUEIDENTIFIER, -- (M1+)
    SlotId UNIQUEIDENTIFIER, -- (M2+)
    Status NVARCHAR(50), -- PendingTicket, Confirmed, Completed, Cancelled
    QueueNumber INT NULL,
    Code NVARCHAR(10), -- A7K2 (human-readable)
    ServiceDate DATE,
    CancelReason NVARCHAR(500) NULL,
    CreatedAtUtc DATETIME2,
    UpdatedAtUtc DATETIME2,
    RowVersion ROWVERSION,
    IdempotencyKey NVARCHAR(50) UNIQUE NULL -- M2+
);
```

**QueueEntries (Worker owned)**
```sql
CREATE TABLE QueueEntries (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AppointmentId UNIQUEIDENTIFIER UNIQUE, -- 1:1
    PatientUserId NVARCHAR(450),
    DoctorId UNIQUEIDENTIFIER,
    ClinicId UNIQUEIDENTIFIER,
    ServiceDate DATE,
    TicketNumber INT,
    Status NVARCHAR(50), -- Waiting, Called, Serving, Done, Cancelled, Skipped
    CalledAtUtc DATETIME2 NULL,
    CompletedAtUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2,
    UpdatedAtUtc DATETIME2,

    UNIQUE(AppointmentId),
    UNIQUE(DoctorId, ClinicId, ServiceDate, TicketNumber),
    INDEX IX_Queue (DoctorId, ServiceDate, Status, TicketNumber)
);
```

**OutboxMessages (Api owned)**
```sql
CREATE TABLE OutboxMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType NVARCHAR(100),
    Payload NVARCHAR(MAX), -- JSON
    ProcessedAt DATETIME2 NULL,
    CreatedAtUtc DATETIME2,

    INDEX IX_Unprocessed (ProcessedAt, CreatedAtUtc) WHERE ProcessedAt IS NULL
);
```

**ProcessedMessages (Worker owned)**
```sql
CREATE TABLE ProcessedMessages (
    MessageId NVARCHAR(100) PRIMARY KEY,
    ProcessedAtUtc DATETIME2
);
```

---

## G. Cấu trúc Solution

```
C:\Code\ClinicQueue\
├── ClinicQueue.slnx
├── DESIGN.md
├── README.md
├── .env.example
│
├── src/
│   ├── ClinicQueue.Contracts/
│   │   ├── ClinicQueue.Contracts.csproj
│   │   ├── Events/
│   │   │   ├── TicketRequestedEvent.cs
│   │   │   ├── TicketAssignedEvent.cs
│   │   │   ├── QueueChangedEvent.cs
│   │   │   └── AppointmentCancelledEvent.cs
│   │   ├── Dtos/
│   │   │   └── (message DTOs)
│   │   └── Constants/
│   │       └── RoutingKeys.cs
│   │
│   ├── ClinicQueue.Api/
│   │   ├── ClinicQueue.Api.csproj
│   │   ├── Program.cs (M0: minimal setup, health endpoint)
│   │   ├── Controllers/
│   │   │   └── HealthController.cs (M0)
│   │   ├── Data/
│   │   │   ├── ApiDbContext.cs (M0: skeleton)
│   │   │   └── Migrations/ (M0+)
│   │   ├── Infrastructure/
│   │   │   ├── Messaging/
│   │   │   │   └── OutboxPublisher.cs (M1+)
│   │   │   └── Services/
│   │   │       └── (M1+)
│   │   └── appsettings.json, appsettings.Development.json
│   │
│   ├── ClinicQueue.Worker/
│   │   ├── ClinicQueue.Worker.csproj
│   │   ├── Program.cs (M0: minimal setup, health endpoint)
│   │   ├── Data/
│   │   │   ├── WorkerDbContext.cs (M0: skeleton)
│   │   │   └── Migrations/ (M0+)
│   │   ├── Consumers/ (M1+)
│   │   ├── Services/ (M1+)
│   │   ├── Infrastructure/
│   │   │   └── Health/ (M0)
│   │   └── appsettings.json, appsettings.Development.json
│   │
│   └── ClinicQueue.Notification/
│       ├── ClinicQueue.Notification.csproj
│       ├── Program.cs (M0: minimal setup, health endpoint)
│       ├── Hubs/ (M0: skeleton QueueHub.cs)
│       ├── Consumers/ (M1+)
│       ├── Infrastructure/
│       │   └── Health/
│       └── appsettings.json, appsettings.Development.json
│
├── frontend/
│   ├── clinicqueue-web/
│   │   ├── package.json (React, Vite, TypeScript, Tailwind)
│   │   ├── tsconfig.json
│   │   ├── vite.config.ts
│   │   ├── tailwind.config.ts
│   │   ├── postcss.config.js
│   │   ├── .env.example
│   │   ├── src/
│   │   │   ├── main.tsx
│   │   │   ├── App.tsx (M0: skeleton, login placeholder)
│   │   │   ├── index.css (Tailwind imports)
│   │   │   └── (M1+: pages, services, contexts)
│   │   ├── public/
│   │   │   └── index.html
│   │   └── dist/ (output sau build)
│
├── deploy/
│   ├── docker-compose.yml (M0: SQL Server, RabbitMQ)
│   ├── docker-compose.override.yml (dev)
│   ├── .dockerignore
│   ├── Dockerfile.api
│   ├── Dockerfile.worker
│   ├── Dockerfile.notification
│   ├── Dockerfile.web
│   └── nginx.conf (reverse proxy)
│
└── tests/ (M5+)
    ├── ClinicQueue.Api.Tests/
    └── ClinicQueue.Worker.Tests/
```

---

## H. 5 Vấn đề bổ sung (M0+ phải đảm bảo)

### H.1 API consume TicketAssigned để cập nhật Appointment

**Vấn đề:** Luồng đặt lịch trả 202, số thứ tự cấp từ Worker. API phải cập nhật `Appointment.Status = Confirmed` + `QueueNumber`.

**Giải pháp:**
1. **Api.Infrastructure.Messaging.TicketAssignedConsumer** (M1+):
   - Consume từ RabbitMQ queue `queue.ticket.assigned`
   - Deserialize payload → `{ appointmentId, ticketNumber, ... }`
   - BEGIN TRANSACTION
   - SELECT Appointment WHERE id = @appointmentId AND status = 'PendingTicket'
   - UPDATE Appointment SET status = 'Confirmed', QueueNumber = @ticketNumber, UpdatedAtUtc = NOW
   - COMMIT
   - Ack message

2. **Idempotency:** Nếu message redelivery, appointment.Status đã `Confirmed` → SQL `IF` logic hoặc skip nếu status khác.

3. **M0:** Tạo interface IAppointmentUpdateService (stub), ghi chú "implement M1".

### H.2 Worker đảm bảo không mất sự kiện: Outbox + Idempotency

**Vấn đề:** Worker commit dữ liệu xong, phải chắc chắn RabbitMQ nhận event (không bỏ event vì worker crash).

**Giải pháp:**
1. **Outbox pattern trong Worker** (M1+):
   - Không publish trực tiếp RabbitMQ
   - Ghi OutboxMessage cùng transaction: `INSERT OutboxMessage (eventType='TicketAssigned', payload=..., processedAt=NULL)`
   - COMMIT
   - Background publisher (IHostedService) quét OutboxMessage chưa gửi, publish, mark processedAt

2. **Idempotency:**
   - Consumer kiểm tra `ProcessedMessages(messageId)` trước xử lý
   - Nếu tồn tại → skip (message đã xử lý lần trước)
   - Ghi ProcessedMessages cùng transaction với business logic

3. **M0:** Tạo schema sơ bộ cho OutboxMessages, ProcessedMessages.

### H.3 SignalR xác thực & phân quyền group

**Vấn đề:** Client không được tự subscribe group riêng của người khác (leak data bệnh nhân).

**Giải pháp:**
1. **OnConnectedAsync() server-side** (M1+):
   ```csharp
   public override async Task OnConnectedAsync()
   {
       var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

       if (string.IsNullOrEmpty(userId))
       {
           Context.Abort(); // reject
           return;
       }

       // Server tự gán group từ claim, không để client quyết định
       if (role == "Patient")
           await Groups.AddToGroupAsync(Context.ConnectionId, $"patient_{userId}");
       else if (role == "Staff")
       {
           var doctorId = Context.User?.FindFirst("doctorId")?.Value;
           if (!string.IsNullOrEmpty(doctorId))
               await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
       }

       await base.OnConnectedAsync();
   }
   ```

2. **Không expose public method để subscribe group tuỳ ý** → chặn leak.

3. **M0:** Setup skeleton QueueHub.cs với placeholder `OnConnectedAsync`.

### H.4 Chống trùng lịch + trùng số: Transaction + Unique Constraint

**Vấn đề:** Hai bệnh nhân đặt cùng slot đồng thời → race condition.

**Giải pháp:**
1. **Chống trùng lịch:**
   - `UNIQUE(DoctorId, ServiceDate, SlotStartTime)` trên bảng Slots (M1+)
   - API `SELECT * FROM AppointmentSlots WITH (UPDLOCK, HOLDLOCK) WHERE Id = @slotId` (row lock) trong transaction
   - UPDATE slot SET status = Booked WHERE status = Free
   - Nếu 0 dòng → ROLLBACK, return 409

2. **Chống trùng số thứ tự:**
   - `UNIQUE(DoctorId, ClinicId, ServiceDate, TicketNumber)` trên QueueEntries (M1+)
   - Worker transaction với isolation level `Serializable`
   - `QueueCounters WITH (UPDLOCK, HOLDLOCK)` + atomic increment with OUTPUT INSERTED.LastNumber; SERIALIZABLE transaction
   - INSERT mới
   - Nếu unique violation → error signal (không nên xảy ra)

3. **M0:** Tạo schema placeholder, ghi chú constraint.

### H.5 Luồng Staff gọi lượt tiếp theo, cập nhật trạng thái, xử lý hủy

**Vấn đề:** Staff cần gọi lượt tiếp, cập nhật trạng thái khám, hủy lịch đang chờ cấp.

**Giải pháp:**

**1. Gọi lượt tiếp theo (M5):**
```
Staff: PUT /api/queue/{clinicId}/call-next
API: authorize scope; insert CallNextRequested into API outbox; respond 202 + commandId.
Worker: consume idempotently; lock next Waiting entry; update Called and insert QueueChanged into Worker outbox in one transaction.
```

**2. Cập nhật trạng thái khám (M5):**
```
Staff: PUT /api/queue/{entryId}/status
API: authorize; insert QueueStatusChangeRequested into API outbox; respond 202 + commandId.
Worker: validate transition; update QueueEntries and insert QueueChanged into Worker outbox in one transaction.
API never writes queue state. UI obtains results through events/read model/polling.
```

**3. Hủy lịch đang chờ cấp (PendingTicket):**
```
Patient: DELETE /api/appointments/{id}
API:
  1. Kiểm tra appointment.status = PendingTicket hoặc Confirmed
  2. BEGIN TRANSACTION
  3. UPDATE Appointment SET status = Cancelled
  4. UPDATE Slot SET status = Free (nếu đã có slot)
  5. INSERT OutboxMessage (AppointmentCancelled)
  6. COMMIT

Worker consume AppointmentCancelled:
  1. SELECT QueueEntry WHERE appointmentId = @id
  2. UPDATE status = Cancelled
  3. Publish QueueChanged
```

**M0:** Tạo controller skeleton, ghi chú "implement M2/M3".

---

## I. RỦI RO VÀ GIẢI PHÁP

| # | Rủi ro | Mức | Cách xử lý |
|---|---|---|---|
| 1 | **Cấp trùng số** | Cao | Serializable transaction + UNIQUE constraint + idempotency |
| 2 | **Trùng slot** | Cao | Row-level lock + conditional update + 409 response |
| 3 | **Mất event** | Cao | Outbox pattern + publisher confirm + consumer manual ack |
| 4 | **Xử lý lặp** | Cao | Inbox pattern: ProcessedMessages table + idempotency check |
| 5 | **Data leak SignalR** | Cao | Server-side group assign từ JWT claim, chặn client subscribe |
| 6 | **Race condition Appointment** | TB | Optimistic concurrency (RowVersion) hoặc pessimistic lock |
| 7 | **Eventual consistency** | TB | UI hiển thị updatedAt, polling fallback, inform user trễ |
| 8 | **Worker crash giữa logic** | TB | Transaction rollback, message redelivery từ RabbitMQ |
| 9 | **DLQ poison message** | TB | Alert + log, hạn chế retry (5 lần), đưa vào quarantine |
| 10 | **SignalR scale** | Thấp | 1 instance M0-M4, Redis backplane sau M6 (không triển khai M0) |

---

## J. Phạm vi M0 — Khởi tạo hệ thống (CHỈ CÓ SETUP, KHÔNG TRIỂN KHAI BUSINESS)

**Mục tiêu:** Dự án chạy được, health check tất cả service, database sẵn sàng, Docker Compose hoạt động.

**Không triển khai:**
- ❌ Authentication/JWT (M1)
- ❌ CRUD admin (M1)
- ❌ Booking business logic (M2)
- ❌ RabbitMQ consumer (M1-M3)
- ❌ SignalR realtime (M4)
- ❌ Outbox publisher (M1)

**Triển khai M0:**
✅ Solution + 3 service projects + Contracts
✅ DbContext skeleton + migrations chạy được
✅ RabbitMQ topology (exchange, queue setup)
✅ Health endpoint `/health` cho mỗi service
✅ React SPA skeleton + Vite build
✅ Docker Compose: SQL Server + RabbitMQ
✅ .env configuration
✅ README chạy project

---

## K. Phạm vi M1-M6 (tương lai)

| Mục | Nội dung |
|---|---|
| **M1** | JWT auth, CRUD admin (doctors, clinics, schedules, users), Outbox pattern, TicketAssignedConsumer |
| **M2** | Booking business (tìm slot, đặt lịch → 202, hủy lịch), AppointmentSlots table, concurrency |
| **M3** | RabbitMQ TicketRequestedConsumer, cấp số, QueueEntries, AppointmentCancelledConsumer |
| **M4** | SignalR Hub, TicketAssigned/QueueChanged consumer, frontend realtime |
| **M5** | Staff UI (call-next, status, queue display), hardening (DLQ, retry, log) |
| **M6** | Integration tests, load test, deployment, production config, README final |

---

## Kết luận

Thiết kế ClinicQueue đã hoàn tất với 5 vấn đề bổ sung:
1. ✅ Api consume TicketAssigned cập nhật Appointment
2. ✅ Worker outbox + idempotency chống mất event
3. ✅ SignalR xác thực & phân quyền server-side
4. ✅ Chống trùng lịch + số bằng transaction + constraint
5. ✅ Staff gọi lượt, cập nhật trạng thái, hủy lịch

**Bước tiếp theo:** Triển khai M0 — setup solution, Docker, health endpoint.

### M0 implementation note
API and Worker use independent empty initial migrations/history tables in schemas api/worker in the shared ClinicQueue database. Business entities, consumers, authentication and SignalR are deferred. QueueCounters belongs to Worker (M3). RabbitMQ imports durable topology at startup, with a separate TicketAssigned API queue for fan-out. Worker readiness uses a database-backed heartbeat file.
