# Báo cáo Tuần 1 - Đề tài 12: Hệ thống phê duyệt tài liệu phân tán

## 1. Mô tả kiến trúc và cấu trúc dự án
- **Kiến trúc:** Mô hình Client - Server phân tán gồm 2 tiến trình độc lập (`Document.Api` và `Document.Client`), giao tiếp đồng bộ qua HTTP/JSON[cite: 8].
- **Cấu trúc thư mục:**
  ```text
  Tuan1/
  ├── src/
  │   ├── Document.Api/       # Dự án Minimal API (Server, lưu trữ in-memory, xử lý nghiệp vụ)
  │   └── Document.Client/    # Dự án Console Application (Client gọi API)
  ├── DocumentApprovalSystem.slnx # Tệp Solution của .NET
  ├── requests.http           # Tệp kiểm thử API trực tiếp trên VS Code
  └── README.md               # Báo cáo và ghi chú tuần 1
  ```

## 2. Các chức năng đã hiện thực
- **Quản lý trạng thái tài liệu:** Hỗ trợ các trạng thái `Draft` (Bản thảo), `Submitted` (Đã gửi duyệt), `Approved` (Đã duyệt), `Rejected` (Từ chối).
- **API Endpoints:**
  - `GET /api/documents`: Lấy danh sách tài liệu (hỗ trợ lọc theo `status` và `assignee`).
  - `POST /api/documents`: Tạo mới tài liệu (`201 Created` kèm đường dẫn `Location`).
  - `POST /api/documents/{id}/submit`, `approve`, `reject`: Cập nhật luồng phê duyệt và ghi nhận lịch sử xử lý (`History`).
- **Xử lý ngoại lệ chuẩn hóa:** Trả về cấu trúc lỗi chuẩn `ApiError` với các mã trạng thái HTTP phù hợp (`400 Bad Request`, `404 Not Found`, `409 Conflict`).

