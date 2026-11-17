# API documentation

Swagger UI: `http://localhost:5001/swagger` (EventService), `http://localhost:5002/swagger` (DashboardService). API errors use suitable 400/404/409/503 responses; internal errors include the correlation ID without exposing database details.

## EventService

| Method | Route | Description |
|---|---|---|
| GET | `/events` | Search, filter status/category, sort, paginate |
| GET | `/events/{id}` | Event detail and availability |
| POST | `/events` | Create event |
| PUT | `/events/{id}` | Update event; capacity cannot undercut active registrations |
| DELETE | `/events/{id}` | Delete empty event or close event with registrations |
| POST | `/events/{id}/registrations` | Create participant/registration and return QR data |
| DELETE | `/registrations/{id}` | Cancel registration if not checked in |
| POST | `/checkin/verify` | Verify registration code or QR text |
| POST | `/registrations/{id}/checkin` | Confirm check-in once |
| GET | `/registrations/{id}` | Registration detail |
| GET | `/registrations/{id}/qrcode` | PNG download |
| GET | `/events/{id}/registrations` | Event registration list, search and status filter |
| GET | `/admin/events`, `/admin/participants`, `/admin/registrations` | Admin list APIs |
| POST/PUT/DELETE | `/admin/events[/{id}]` | Admin event aliases |
| GET | `/health`, `/health/live`, `/health/ready` | Liveness/readiness |

Registration body: `{ "fullName": "Nguyen Van A", "email": "a@example.test", "phone": "0901234567", "studentCode": "ICTU001" }`.

## DashboardService

`GET /dashboard/overview`, `/dashboard/chart`, `/dashboard/events/{eventId}`, `/dashboard/events/{eventId}/statistics`, `/dashboard/events/{eventId}/chart`, `/dashboard/recent-activities`, `/admin/audit-logs`, health routes, and SignalR `/hubs/dashboard`.

The SignalR hub emits `RegistrationCreated`, `RegistrationCancelled`, `ParticipantCheckedIn`, `DashboardUpdated`, and `RecentActivityCreated`.
