# Thiết kế RESTful API dự kiến

## 1. Quy ước chung

- Base URL Development của Appointment API: `http://localhost:5101` hoặc `https://localhost:7101`.
- Các route tối thiểu giữ nguyên đúng đề bài, không bắt buộc prefix version ở giai đoạn đầu.
- Content type: `application/json; charset=utf-8`.
- Định danh trong ví dụ dùng UUID giả lập.
- Thời gian kỹ thuật dùng ISO 8601 UTC; ngày nghiệp vụ dùng `yyyy-MM-dd`.
- Endpoint API riêng tư nhận `Authorization: Bearer <JWT>` từ MVC/server caller.
- V1 chỉ có JWT Access Token thời hạn hữu hạn, không có Refresh Token. MVC giữ token trong token store phía server và browser chỉ nhận cookie phiên `HttpOnly`; không đưa token vào JavaScript, HTML, `localStorage` hoặc `sessionStorage`.
- Lỗi theo `application/problem+json` (`ProblemDetails`) và có `traceId`.
- Collection phân trang bằng `page`, `pageSize`; giới hạn `pageSize` ở server.
- Lệnh tạo có nguy cơ retry nhận header `Idempotency-Key` dạng UUID.

## 2. Authentication và authorization

### 2.1 Quy chuẩn tổ chức Controller

Cả Web và Backend chỉ tổ chức theo nghiệp vụ: `AccountController`, `PatientsController`, `DoctorsController`, `AppointmentsController`, `QueuesController`, `DepartmentsController`, `RoomsController`, `TimeSlotsController`, `UsersController` và `DashboardController`. `HomeController` của Web và `HealthController` của API là controller nền tảng.

Không tạo riêng `PatientController`, `DoctorController`, `ReceptionistController` hoặc `AdminController` để biểu diễn Role. Cùng một nghiệp vụ dùng chung controller/service; Role-based authorization và resource ownership quyết định Actor nào được thao tác. Frontend vẫn có các khu vực Auth/Patient/Doctor/Receptionist/Admin.

MVC Controller kế thừa `Controller`, nhận tương tác UI, dùng ViewModel và gọi backend qua HTTP API; không tham chiếu DbContext/Infrastructure. API Controller kế thừa `ControllerBase`, dùng `[ApiController]`, kiểm tra Role/quyền trên tài nguyên, gọi service/domain và trả DTO hoặc `ProblemDetails`. Không lặp nghiệp vụ đặt lịch giữa Patient/Receptionist, không truy cập SQL trực tiếp và không gọi controller khác. Route API được khai báo tường minh để bảo toàn sáu route bắt buộc.

Hiện trạng Prompt 03.1.1 kế thừa từ Tuần 1: Web có `HomeController` phục vụ trang chủ/trang lỗi MVC; API chỉ có `HealthController` với `GET /health` để kiểm tra host. Các controller và endpoint nghiệp vụ ở trên **chưa được triển khai**; không có endpoint giả trả thành công.

| Method | Route dự kiến | Quyền | Mức | Mục đích |
|---|---|---|---|---|
| POST | `/auth/register` | Anonymous | Mở rộng | Chỉ đăng ký Patient; server tự gán Role |
| POST | `/auth/login` | Anonymous | Mở rộng | Form chung cho 4 Role, phát token và Role |
| POST | `/auth/logout` | Authenticated | Mở rộng | Kết thúc phiên, xóa token server-side và cookie MVC |
| GET | `/auth/me` | Authenticated | Mở rộng | Trả user, Role và thông tin điều hướng |

Ví dụ login:

```http
POST /auth/login
Content-Type: application/json

{
  "email": "patient.demo@example.test",
  "password": "<not-a-real-password>"
}
```

```json
{
  "accessToken": "<jwt>",
  "expiresAtUtc": "2026-11-17T09:00:00Z",
  "user": {
    "id": "d59a0d4c-fcbe-4dc6-9aee-b55c86be3ffd",
    "displayName": "Bệnh nhân minh họa",
    "role": "Patient"
  },
  "redirectPath": "/patient"
}
```

Không nhận `role` từ request đăng ký. Doctor và Receptionist chỉ do Admin cấp.

Khi Access Token hết hạn, v1 yêu cầu đăng nhập lại; cookie không được gia hạn vượt quá hạn token. API/SignalR không ghi token vào log. Thiết kế SignalR ở giai đoạn sau phải xác thực và kiểm tra group mà không làm lộ JWT cho JavaScript.

## 3. API bắt buộc của đề tài 09

| Method | Route | Quyền dự kiến | Requirement |
|---|---|---|---|
| GET | `/doctors` | Anonymous/Authenticated | FR-DOC-01 |
| GET | `/doctors/{id}/slots` | Anonymous/Authenticated | FR-SLOT-01 |
| POST | `/appointments` | Patient, Receptionist | FR-APT-01, FR-QUE-01 |
| DELETE | `/appointments/{id}` | Owner Patient, Receptionist | FR-APT-02 |
| GET | `/appointments/{id}` | Owner/scoped staff | FR-APT-03 |
| GET | `/queues/{doctorId}` | Theo policy queue | FR-QUE-08 |

## 4. Doctor và slot

### `GET /doctors`

Query dự kiến: `search`, `departmentId`, `page=1`, `pageSize=20`.

```json
{
  "items": [
    {
      "id": "41879991-b095-414a-a959-c42b7b388a34",
      "displayName": "Bác sĩ minh họa",
      "department": { "id": "28fcb523-5a52-472b-b88d-3bd68fbe031a", "name": "Nội tổng quát" },
      "rooms": [{ "id": "3f1f5dd8-b277-45cb-a44e-4cbef159ece7", "name": "Phòng A" }]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

Response: `200`; bộ lọc sai `400`. Danh sách rỗng vẫn `200`.

### `GET /doctors/{id}/slots`

Query bắt buộc: `fromDate`, `toDate`; **đề xuất** giới hạn khoảng tra cứu tối đa 31 ngày.

```json
{
  "doctorId": "41879991-b095-414a-a959-c42b7b388a34",
  "timeZone": "Asia/Ho_Chi_Minh",
  "items": [
    {
      "id": "1e9b31c5-6975-4298-ac44-f1e30ff03d40",
      "startAt": "2026-11-20T08:00:00+07:00",
      "endAt": "2026-11-20T08:30:00+07:00",
      "roomName": "Phòng A",
      "remainingCapacity": 2
    }
  ]
}
```

Response: `200`, `400`, `404`. Remaining capacity là snapshot; POST phải kiểm tra lại.

## 5. Appointment

### `POST /appointments`

Headers:

```http
Authorization: Bearer <token>
Idempotency-Key: 86a1eb80-9d18-432d-b11c-b99bd99f2cf8
```

Patient request:

```json
{
  "slotId": "1e9b31c5-6975-4298-ac44-f1e30ff03d40",
  "reason": "Nội dung giả lập, không phải dữ liệu y tế thật"
}
```

Receptionist request hỗ trợ:

```json
{
  "slotId": "1e9b31c5-6975-4298-ac44-f1e30ff03d40",
  "patientId": "943aa22d-2ba3-497f-b57f-d9157136379c"
}
```

Response `201 Created`, header `Location: /appointments/{id}`:

```json
{
  "id": "eb84dc9d-38c4-40aa-92d3-2587f1b05c86",
  "status": "PendingNumber",
  "slotId": "1e9b31c5-6975-4298-ac44-f1e30ff03d40",
  "queue": {
    "status": "PendingIssuance",
    "ticketId": null,
    "queueNumber": null
  },
  "createdAtUtc": "2026-11-17T08:00:00Z"
}
```

| Mã | Khi nào |
|---|---|
| `201` | Appointment và outbox đã commit |
| `400` | Payload/slot quá khứ không hợp lệ |
| `401/403` | Chưa đăng nhập/sai Role hoặc scope |
| `404` | Slot/Patient hỗ trợ không tồn tại trong scope |
| `409` | Hết chỗ, trùng giờ, key khác payload hoặc concurrency conflict |

Việc trả `201` không có nghĩa QueueTicket đã được cấp. Appointment đang `PendingNumber`; Queue Worker xử lý ngay qua RabbitMQ và chuyển sang `Waiting`. Check-in không tham gia luồng cấp số.

### `GET /appointments/{id}`

```json
{
  "id": "eb84dc9d-38c4-40aa-92d3-2587f1b05c86",
  "status": "Waiting",
  "doctor": {
    "id": "41879991-b095-414a-a959-c42b7b388a34",
    "displayName": "Bác sĩ minh họa"
  },
  "slot": {
    "startAt": "2026-11-20T08:00:00+07:00",
    "endAt": "2026-11-20T08:30:00+07:00",
    "roomName": "Phòng A"
  },
  "queue": {
    "ticketId": "70be2230-4edc-40db-bbe6-48588070dc7d",
    "queueNumber": 5,
    "status": "Waiting"
  },
  "updatedAtUtc": "2026-11-17T08:00:02Z"
}
```

Response `200`, `401/403`, `404`. **Đề xuất bảo mật:** với tài nguyên ngoài ownership, trả `404` để hạn chế dò ID.

### `DELETE /appointments/{id}`

Body không bắt buộc; nếu cần lý do có thể dùng:

```json
{ "reason": "Thay đổi kế hoạch" }
```

- `204`: hủy thành công hoặc đã hủy trước đó (idempotent).
- `404`: không tồn tại/không thuộc scope theo policy.
- `409`: đã tới `Slot.StartAt` hoặc trạng thái không còn là `PendingNumber/Waiting`.

Nếu lịch đã có QueueNumber, hủy không làm giảm counter và không tái sử dụng số đó.

### API mở rộng về Appointment

| Method | Route | Quyền | Mục đích |
|---|---|---|---|
| GET | `/appointments?fromDate=&toDate=&status=&page=` | Authenticated, scoped | Danh sách/lịch sử |
| GET | `/patients?search=` | Receptionist, Admin | Tìm Patient để hỗ trợ; dữ liệu tối thiểu |
| POST | `/appointments/{id}/check-in` | Receptionist | Ghi nhận tiếp nhận; không đổi trạng thái/cấp số |

## 6. Queue query và command

### `GET /queues/{doctorId}`

Query: `date=2026-11-20`; mặc định ngày nghiệp vụ hiện tại.

Payload summary dành cho Patient đã đăng nhập không chứa danh tính Patient khác:

```json
{
  "doctorId": "41879991-b095-414a-a959-c42b7b388a34",
  "serviceDate": "2026-11-20",
  "currentServingNumber": 3,
  "currentServingDisplayNumber": "003",
  "waitingCount": 6,
  "myTicket": {
    "queueNumber": 5,
    "displayNumber": "005",
    "status": "Waiting",
    "peopleAhead": 1
  },
  "version": 12,
  "updatedAtUtc": "2026-11-20T01:30:00Z"
}
```

Endpoint luôn yêu cầu đăng nhập. `waitingCount` là `COUNT` các ticket `Waiting`; `peopleAhead` là số bản ghi Waiting đứng trước theo `Slot.StartAt, QueueNumber`, không phải phép trừ QueueNumber. Doctor/Receptionist response có thể có `entries` chi tiết theo đúng scope, nhưng Patient không nhận dữ liệu Patient khác. Response: `200`, `400`, `401/403`, `404`.

### Queue command mở rộng cần thiết để đáp ứng nghiệp vụ

| Method | Route dự kiến | Role | State effect |
|---|---|---|---|
| POST | `/queues/{doctorId}/call-next` | Doctor owner | Lượt đủ điều kiện `Waiting → Serving` |
| POST | `/queue-tickets/{ticketId}/complete` | Doctor owner | `Serving → Completed` |
| POST | `/queue-tickets/{ticketId}/no-show` | Doctor owner | Lượt Waiting kế tiếp đủ điều kiện → `NoShow` |

`call-next` chỉ xét ticket `Waiting` có slot đã đến giờ Việt Nam và sắp theo `Slot.StartAt`, rồi `QueueNumber`; v1 không có trạng thái `Called`. Mỗi command nhận `Idempotency-Key` hoặc expected version và trả `409` nếu state/version không hợp lệ. DoctorId trong token/resource mapping phải trùng route; không tin DoctorId do UI gửi. Receptionist không được gọi/complete/no-show.

Ví dụ `POST /queues/{doctorId}/call-next` response:

```json
{
  "queueTicketId": "70be2230-4edc-40db-bbe6-48588070dc7d",
  "queueNumber": 5,
  "displayNumber": "005",
  "status": "Serving",
  "queueVersion": 13,
  "changedAtUtc": "2026-11-20T01:31:00Z"
}
```

## 7. Admin API mở rộng

Các endpoint sau thuộc phạm vi sản phẩm nhưng không phải API tối thiểu; chi tiết DTO sẽ được chốt ở giai đoạn triển khai.

| Resource | Route dự kiến | Hành động |
|---|---|---|
| Users/roles | `/admin/users` | GET, POST staff, PATCH active/role |
| Doctors | `/admin/doctors` | GET, POST, PUT/PATCH, deactivate |
| Departments | `/admin/departments` | CRUD có kiểm soát |
| Rooms | `/admin/rooms` | CRUD có kiểm soát |
| Schedules | `/admin/doctor-schedules` | CRUD + chống chồng lịch |
| Slots | `/admin/appointment-slots` | Generate/list/lock theo schedule |
| Reports | `/admin/reports/appointments` | GET aggregate có bộ lọc |

Admin endpoints đều yêu cầu policy `AdminOnly` và audit.

## 8. ProblemDetails

```json
{
  "type": "https://mediqueue.local/problems/slot-capacity-exhausted",
  "title": "Khung giờ đã hết chỗ",
  "status": 409,
  "detail": "Vui lòng chọn một khung giờ khác.",
  "instance": "/appointments",
  "code": "SLOT_CAPACITY_EXHAUSTED",
  "traceId": "00-example"
}
```

Mã lỗi dự kiến:

- `VALIDATION_FAILED`
- `APPOINTMENT_TIME_OVERLAP`
- `SLOT_CAPACITY_EXHAUSTED`
- `APPOINTMENT_STATE_CONFLICT`
- `QUEUE_STATE_CONFLICT`
- `IDEMPOTENCY_KEY_REUSED`
- `RESOURCE_NOT_FOUND`

Không trả stack trace, SQL exception, token hoặc thông tin nhạy cảm.

## 9. SignalR contract

- Hub endpoint dự kiến: `/hubs/queues` trên Notification Service; không chấp nhận Anonymous.
- Server event: `QueueChanged`.
- Client không gửi DoctorId để tự join tùy ý; server suy ra/kiểm tra scope.

```json
{
  "eventId": "e02bdf54-186e-4a99-87a2-e74213871896",
  "doctorId": "41879991-b095-414a-a959-c42b7b388a34",
  "serviceDate": "2026-11-20",
  "changeType": "TicketServing",
  "currentServingNumber": 3,
  "queueVersion": 13,
  "occurredAtUtc": "2026-11-20T01:31:00Z"
}
```

Event cá nhân có thể thêm `myTicket`, nhưng chỉ phát group `user:{userId}`. Client:

1. Đăng nhập, tải snapshot trước khi subscribe.
2. Chỉ áp dụng event có version mới hơn.
3. Automatic reconnect.
4. Tải lại snapshot sau reconnect hoặc khi phát hiện khoảng trống version.
5. Khi Access Token hết hạn, dừng reconnect và yêu cầu đăng nhập lại; v1 không Refresh Token.

## 10. OpenAPI và kiểm thử hợp đồng

Ở giai đoạn code:

- Sinh OpenAPI từ API và giữ ví dụ/response code đồng bộ tài liệu.
- Tạo `.http` REST Client cho happy path, authorization failure, conflict, idempotency.
- Integration test tối thiểu cho sáu endpoint bắt buộc.
- Test resource-based authorization để Patient/Doctor không vượt scope.

## 11. Tham số kỹ thuật sẽ chốt khi triển khai

- Thời lượng JWT Access Token và giới hạn khoảng ngày tra cứu slot.
- `reason` khi hủy có bắt buộc hay chỉ phục vụ audit.
- CORS và chính sách HTTPS/WSS giữa các tiến trình Windows; port phát triển đã chốt: API `5101/7101`, Web `5102/7102`, Notification `5103/7103`.
- Route versioning (`/api/v1`) chỉ bổ sung nếu vẫn duy trì/đáp ứng sáu route tối thiểu giảng viên yêu cầu.

