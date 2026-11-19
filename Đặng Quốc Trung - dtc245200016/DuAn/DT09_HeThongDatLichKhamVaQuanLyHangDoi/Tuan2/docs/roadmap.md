# Lộ trình phát triển và nộp dự án trong 9 tuần

## 1. Nguyên tắc

- Đây là lộ trình nội bộ của MediQueue, không phải tiến độ bắt buộc do giảng viên ban hành.
- Mỗi tuần làm việc trong một thư mục riêng `Tuan1` đến `Tuan9` và chỉ có một Pull Request riêng; một PR có thể chứa nhiều commit thực tế.
- Tuần sau kế thừa tuần trước, không sửa thư mục/PR đã nộp và không làm lại dự án từ đầu.
- Mỗi kết quả phải phân biệt rõ đã triển khai, đã kiểm tra và mới chỉ thiết kế.
- Chỉ commit/push/tạo PR khi sinh viên review và chủ động thực hiện hoặc cho phép.
- Không dùng Docker; không hard-code secret; không tự chạy Migration hay thay đổi hạ tầng khi chưa được duyệt.

Tuần 1 nằm tại `Tuan1`, đã hoàn tất nền tảng đến commit nguồn `dcee864`. Tuần 2 nằm tại `Tuan2`, nhánh `duan-tuan2-dtc245200016`, PR dự kiến `DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung`; hạn nộp Tuần 2 chưa được cung cấp.

## 2. Kế hoạch theo tuần

| Tuần / thư mục | Mục tiêu | Sản phẩm dự kiến | Gate chất lượng |
|---|---|---|---|
| Tuần 1 / `Tuan1` | Phân tích, thiết kế và nền tảng | 9 project .NET 10; MVC/API host; Domain/EF/Identity persistence; InitialCreate | Đã hoàn thành: build/test/smoke và schema được xác minh |
| Tuần 2 / `Tuan2` | Identity, Authentication, Authorization, dashboard và danh mục | JWT API, MVC Cookie + token server-side và 4 dashboard đã có; tiếp tục tài khoản/Department/Room/Doctor/Patient | Role + ownership, CSRF/session và không lộ token được kiểm thử |
| Tuần 3 / `Tuan3` | Lịch làm việc, khung giờ và đặt lịch | DoctorSchedule, TimeSlot, slot query và booking nền tảng | Capacity, overlap, thời gian và concurrency có test |
| Tuần 4 / `Tuan4` | Hoàn thiện đặt/hủy lịch | Sáu REST API tối thiểu; hủy, check-in metadata, idempotency/outbox | Hạn hủy, ownership, tranh chỗ và gọi lặp được kiểm thử |
| Tuần 5 / `Tuan5` | RabbitMQ, Queue Worker và cấp số | Message contracts; publisher/consumer; inbox/outbox; QueueTicket | Redelivery không cấp trùng; QueueNumber Doctor/ngày đúng |
| Tuần 6 / `Tuan6` | Hàng đợi và SignalR | Queue commands; Notification Service; Hub/group auth; snapshot/reconnect | Một Doctor tối đa một Serving; không lộ dữ liệu Patient khác |
| Tuần 7 / `Tuan7` | Hoàn thiện chức năng và UI/UX | Luồng end-to-end; responsive UI; trạng thái loading/empty/error | Đủ 4 Actor và 8 yêu cầu bắt buộc; UI không thay backend auth |
| Tuần 8 / `Tuan8` | Kiểm thử, bảo mật và tối ưu | Concurrency, idempotency, authorization, performance, hardening | Regression sạch; không secret/PII; recovery paths được kiểm tra |
| Tuần 9 / `Tuan9` | Báo cáo và bảo vệ | Tài liệu chạy Windows; demo script; báo cáo/traceability | Build/test sạch; minh chứng thật; tài liệu nhất quán |

## 3. Truy vết tám yêu cầu bắt buộc

| Yêu cầu | Tuần chính |
|---|---|
| Tra cứu Doctor/phòng và slot còn chỗ | 2–3 |
| Đặt/hủy Appointment | 3–4 |
| API phát yêu cầu cấp số qua RabbitMQ | 4–5 |
| Worker sinh số và cập nhật queue | 5 |
| Doctor gọi/hoàn thành/NoShow | 6 |
| Phát QueueChanged qua RabbitMQ | 5–6 |
| Notification Service đẩy SignalR | 6 |
| Tra cứu Appointment/queue có phân quyền | 4–7 |

Sáu REST API tối thiểu được thiết kế trong `api.md` và phải hoàn thiện chậm nhất Tuần 4: danh sách Doctor, slot theo Doctor, tạo Appointment, hủy Appointment, xem Appointment và xem queue.

## 4. Chiến lược kiểm thử tăng dần

| Lớp | Trọng tâm |
|---|---|
| Domain | State, overlap, capacity, queue order, hạn hủy/NoShow |
| API | Validation, Role/ownership, idempotency, ProblemDetails |
| MVC | Cookie, antiforgery, redirect, access denied, token không lộ browser |
| Database | Constraint, rowversion, hai booking tranh chỗ, exact-target guard |
| Messaging | Duplicate, redelivery, retry/DLQ, outbox recovery |
| Realtime | Group authorization, reconnect, version/snapshot |
| End-to-end | Bốn Actor và luồng đề tài 09 trên Windows |

## 5. Rủi ro xuyên suốt

| Rủi ro | Kiểm soát |
|---|---|
| Ghi nhầm database test vào database chính | Duy nhất `MediQueue` cho runtime; write test dùng DB test cô lập và exact-target guard |
| JWT/session lộ hoặc quyền cũ kéo dài | Token server-side, cookie bảo vệ, token ngắn hạn và revocation |
| Overbooking đồng thời | Transaction, conditional update/rowversion và integration test |
| RabbitMQ gửi lặp | Inbox unique, idempotent handler và unique constraint |
| SignalR mất sự kiện | Version + snapshot là source of truth |
| Phạm vi mở rộng quá sớm | Hoàn thành yêu cầu bắt buộc và test trước mục tiêu tùy chọn |

## 6. Quy tắc commit và Pull Request

Không làm giả lịch sử. Mỗi tuần chỉ mở một Pull Request nhưng được phép có nhiều commit. Commit chỉ mô tả kết quả thực tế đã hoàn thành và được review. Prompt/commit Tuần 2 được chia chi tiết tại [week02-plan.md](week02-plan.md).

PR Tuần 2 dự kiến:

```text
DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung
```

Prompt 03.1.1 đề xuất commit:

```text
chore(tuan2): initialize week two foundation
```

