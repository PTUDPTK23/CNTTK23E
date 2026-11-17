# Luồng RabbitMQ

1. EventService cập nhật SQL cùng Outbox row trong một transaction.
2. Outbox dispatcher khai báo durable Topic Exchange `event.topic`, durable subscriber queues và bindings.
3. Dispatcher gửi persistent JSON event với `MessageId=EventId`, `CorrelationId` và routing key.
4. DashboardService nhận qua `dashboard.events`; AuditWorker nhận cùng message độc lập qua `audit.events`.
5. Consumer kiểm tra `EventId`, ghi dữ liệu projection/audit trong database riêng, rồi ACK.
6. Khi xử lý lỗi, consumer NACK/requeue; khi mất kết nối, worker reconnect.

| Routing key | EventType | Subscribers |
|---|---|---|
| `event.event.created` | `EventCreated` | Dashboard, Audit |
| `event.event.updated` | `EventUpdated` | Dashboard, Audit |
| `event.event.deleted` | `EventDeleted` | Dashboard, Audit |
| `event.participant.registered` | `ParticipantRegistered` | Dashboard, Audit |
| `event.participant.cancelled` | `ParticipantCancelled` | Dashboard, Audit |
| `event.participant.checkedin` | `ParticipantCheckedIn` | Dashboard, Audit |

Queues tách riêng để mọi subscriber đều nhận được mỗi sự kiện. Dashboard phát SignalR sau khi ghi projection thành công. Nếu cần giới hạn retry, thêm dead-letter exchange/queue và chính sách backoff.
