# Dự án: Hệ thống theo dõi bưu gửi (DT08 - Tuần 1)
- **Sinh viên:** Nguyễn Anh Sơn – **MSSV:** dtc245200852 – **Lớp:** CNTT K23E
- **Đề tài:** 08 – Hệ thống theo dõi bưu gửi (`DT08_HeThongTheoDoiBuuGui`)

## 1. Phân tích & Kiến trúc hệ thống phân tán
- **Nghiệp vụ:** Quản lý vận đơn (`parcels`), ghi nhận sự kiện hành trình tại các bưu cục (`tracking_events`), tra cứu hành trình theo `tracking_code`.
- **Đặc tính phân tán:** Tách luồng đọc/ghi, đồng bộ sự kiện bất đồng bộ qua Message Queue, đảm bảo tính lũy đẳng (Idempotency) theo `event_id` khi thiết bị quét mã gửi lại gói tin.
- **Kiến trúc:** `Client (Web/Shipper App) -> API Gateway -> [Parcel Service | Tracking Service] -> Message Queue -> Distributed DB (hubs, parcels, tracking_events)`.

## 2. Thiết kế CSDL & REST API
- **CSDL:** `hubs(hub_code, hub_name, region)`, `parcels(tracking_code, sender_name, receiver_name, origin_hub, dest_hub, current_status, created_at)`, `tracking_events(event_id, tracking_code, hub_code, status, note, event_time)`.
- **API:** `GET /api/parcels`, `GET /api/parcels/{code}`, `POST /api/parcels`, `POST /api/parcels/{code}/events`.

## 3. Kế hoạch 9 tuần & Hướng dẫn chạy Prototype
- **Tuần 1–3:** Phân tích kiến trúc, xây dựng `Parcel Service`, `Hub Service`, `Tracking Service`.
- **Tuần 4–6:** Tích hợp Message Broker (RabbitMQ/Kafka), `API Gateway`, Redis Caching.
- **Tuần 7–9:** Fault Tolerance (Retry/DLQ), đóng gói Docker Compose, kiểm thử tải và hoàn thiện báo cáo.
- **Chạy thử:** `cd src && python3 server.py` (mặc định tại `http://localhost:8080`).
