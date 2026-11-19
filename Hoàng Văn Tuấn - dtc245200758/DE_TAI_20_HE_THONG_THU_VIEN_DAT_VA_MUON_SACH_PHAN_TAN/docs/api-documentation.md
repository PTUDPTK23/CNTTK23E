# API (base: http://localhost:8081/api) — Swagger: /swagger-ui.html
Mọi phản hồi: `{ "success": bool, "message": str, "data": ... }`. Lỗi: 400 (validation), 404, 409 (xung đột/trạng thái sai), 500 (không lộ stack trace).

| Method | Path | Ghi chú |
|---|---|---|
| GET | /books?page&size&keyword&categoryId&author&availability(AVAILABLE/UNAVAILABLE) | phân trang |
| GET | /books/{id} | kèm số bản total/available/reserved/borrowed |
| POST/PUT/DELETE | /books, /books/{id} | DELETE = xóa mềm |
| GET | /books/{id}/availability | |
| GET/POST | /books/{bookId}/copies | POST body `{}` hoặc `{"copyCode":"..."}` |
| PUT | /copies/{id}/status | chỉ AVAILABLE/MAINTENANCE/LOST; bản RESERVED/BORROWED → 409 |
| GET/POST | /categories, PUT /categories/{id} | |
| GET/POST | /users, GET /users/{id} | |
| PUT | /users/{id}/status | body `{"status":"ACTIVE"\|"LOCKED"}` |
| GET | /users/{userId}/reservations?status&page&size | |
| POST | /reservations | **202**, body `{userId, bookId}` |
| GET | /reservations?status | danh sách (quản trị) |
| GET | /reservations/{id}, /reservations/{id}/events | |
| POST | /reservations/{id}/cancel \| borrow \| return | sai trạng thái → 409 |
| GET | /stats | thêm `overdueLoans`, `reservationsByStatus` |
| GET | /stats/categories | số bản sao theo thể loại |
| GET | /system/pipeline | trạng thái Outbox / RabbitMQ / Worker / DLQ |
| GET | /health; /actuator/health | |

Ví dụ:
```
POST /api/reservations   {"userId":"30000000-0000-0000-0000-000000000003","bookId":"20000000-0000-0000-0000-000000000001"}
→ 202 {"success":true,"message":"Yêu cầu đặt mượn đã được tiếp nhận.","data":{"reservationId":"...","requestId":"...","status":"PENDING"}}
```
