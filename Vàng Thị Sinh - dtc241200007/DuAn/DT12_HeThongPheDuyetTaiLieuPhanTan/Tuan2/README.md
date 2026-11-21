# Đề tài 12: Hệ thống phê duyệt tài liệu phân tán — Tuần 2 (REST + SignalR)

- **Sinh viên:** Vàng Thị Sinh — **MSSV:** `dtc241200007`
- **Thư mục:** `Vàng Thị Sinh - dtc241200007/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan2`

## 1. Cách chia việc giữa REST và SignalR

- **REST API (`GET /api/documents`, `GET /api/documents/{documentId}`, `POST /api/documents`):** Tạo yêu cầu phê duyệt tài liệu, trả mã trạng thái HTTP rõ ràng (`201 Created`, `400 Bad Request`, `404 Not Found`) và giữ vai trò **nguồn sự thật** khi mới mở ứng dụng hoặc khi mất kết nối thời gian thực.
- **SignalR Hub (`/hubs/approvals`):** Đẩy thông báo theo thời gian thực tới đúng nhóm theo dõi từng tài liệu (`document:{id}`) mỗi khi trạng thái thay đổi (`submitted` -> `reviewing` -> `approved` / `rejected`).

## 2. Thiết kế kênh thời gian thực (4 câu hỏi)

1. **Sự kiện nào kích hoạt thông điệp?** Khi trạng thái phê duyệt tài liệu thay đổi trong luồng xử lý nền (`submitted` -> `reviewing` -> `approved` hoặc `rejected`).
2. **Gửi cho ai?** Gửi cho nhóm `document:{id}` qua `Clients.Group(...)` / `hubContext.Clients.Group(...)`, đúng người theo dõi tài liệu đó, riêng tư và tiết kiệm băng thông hơn `Clients.All`.
3. **Gửi kèm dữ liệu gì?** Gửi thông tin tối thiểu (`DocumentStatusMessage` gồm `DocumentId`, `Status`, `Note`, `UpdatedAt`) để client cập nhật nhanh và đối chiếu thời điểm cập nhật.
4. **Mất kết nối thì làm sao?** Client bật `WithAutomaticReconnect()` cho SignalR và luôn gọi lại REST `GET /api/documents/{documentId}` để chốt/khôi phục trạng thái chính xác.

## 3. Hướng dẫn chạy thử

Mở terminal tại thư mục `Tuan2`:

- **Chạy máy chủ (cổng 5000):**
  `dotnet run --project src/Document.Api --urls http://localhost:5000`

- **Chạy máy khách — luồng phê duyệt hợp lệ (`approved`):**
  `dotnet run --project src/Document.Client -- http://localhost:5000 VangThiSinh "https://ictu.edu.vn/docs/de-cuong-dt12.pdf"`

- **Chạy máy khách — luồng từ chối (`rejected`):**
  `dotnet run --project src/Document.Client -- http://localhost:5000 VangThiSinh "http://khong-bao-mat.pdf"`

## 4. Kết quả kiểm thử

- **Luồng `approved`:** Máy chủ đẩy trạng thái `reviewing` rồi `approved` tới nhóm `document:{id}`; máy khách nhận thông báo SignalR và chốt trạng thái qua REST là `approved`.
- **Luồng `rejected`:** Khi `FileUrl` không bắt đầu bằng `https://`, trạng thái cuối chuyển thành `rejected` kèm lý do từ chối và được xác nhận lại qua REST.
- **Kiểm tra HTTP & Negotiate:** Các kịch bản REST (`200`, `201`, `400`, `404`) và bắt tay hub (`/hubs/approvals/negotiate` trả về `200` kèm `connectionId`) được kiểm tra trong `requests.http`.