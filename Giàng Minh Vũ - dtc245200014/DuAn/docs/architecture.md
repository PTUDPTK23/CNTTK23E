# Kiến trúc hệ thống

## Service boundaries

- **EventService** sở hữu database nghiệp vụ, là nguồn dữ liệu chuẩn cho event, participant, registration và transactional outbox.
- **DashboardService** tiêu thụ event và lưu read projection riêng cho số liệu/hoạt động. SignalR hub phát cập nhật dashboard.
- **AuditWorker** tiêu thụ cùng event từ queue khác và lưu audit độc lập.
- **Frontend** dùng REST cho command/query nghiệp vụ, SignalR cho dashboard realtime.

```mermaid
flowchart LR
 UI[React SPA] -->|REST commands and queries| ES[EventService]
 UI -->|REST projections| DS[DashboardService]
 UI <-->|SignalR| HUB[/hubs/dashboard]
 HUB --- DS
 ES --> EDB[(EventManagement)]
 ES --> OUT[Outbox dispatcher]
 OUT --> X{{event.topic}}
 X --> DQ[[dashboard.events]] --> DS
 X --> AQ[[audit.events]] --> AW[AuditWorker]
 DS --> DDB[(EventDashboard)]
 AW --> ADB[(EventAudit)]
 DS -. read audit entries .-> ADB
```

## Consistency and availability

Registration/cancellation/check-in mutations run inside SQL Server Serializable transactions. The corresponding event is serialized into OutboxMessages in that same transaction. A hosted dispatcher publishes durable messages and marks the outbox row after broker confirmation through the client publish operation. If publishing fails, the row remains pending for retry. Delivery can occur more than once, therefore subscribers persist ProcessedMessages keyed by EventId.

DashboardService and AuditWorker declare separate durable queues and bind each queue to every business routing key, including `event.event.deleted`. Hard deletion is published through the outbox; DashboardService keeps a versioned tombstone so delayed older messages cannot restore a deleted event. Consumers use manual ACK after the SQL save; a processing error is NACKed with requeue. Consumers reconnect after broker loss. Poison messages currently retry without a dead-letter policy; configure a DLX and retry limit before production deployment.

Dashboard projection is eventually consistent with EventService. It stores activity timestamps and correlation IDs for charts and audit views. EventAudit remains owned by AuditWorker; DashboardService uses an explicit read connection for the Admin audit endpoint.

## Correlation and logging

The API accepts `X-Correlation-ID` or creates one and echoes it. The ID is placed into the outbox event, RabbitMQ properties, dashboard activity, and audit entry. Services use JSON console logging.
