# Bài 3 – SignalR

## Bước 7: Ba thí nghiệm lỗi

### Thí nghiệm 1: Máy chủ chưa chạy

- **Thực hiện:** Tắt máy chủ Ping.Api, sau đó chạy Ping.Client.
- **Kết quả:** Xuất hiện lỗi `HttpRequestException`, `Connection refused (localhost:5200)`.
- **Giải thích:** `StartAsync` ném lỗi ngay khi không thể kết nối đến máy chủ. `WithAutomaticReconnect` chỉ hỗ trợ kết nối đã được mở, không tự khắc phục lần kết nối ban đầu thất bại.

### Thí nghiệm 2: Sai đường dẫn Hub

- **Thực hiện:** Sửa đường dẫn trong `WithUrl` thành `/hub/ping` (thiếu chữ s).
- **Kết quả:** Xuất hiện lỗi `404 Not Found`.
- **Giải thích:** Lỗi xảy ra ở bước negotiate. Đường dẫn trong `WithUrl` phải khớp với đường dẫn `MapHub` ở máy chủ.

### Thí nghiệm 3: Sai tên thông điệp

- **Thực hiện:** Sửa tên thông điệp trong `On("EchoResponse")` thành `On("EchoRespone")`.
- **Kết quả:** Không có lỗi ngoại lệ nhưng không xuất hiện dòng `[Echo]`.
- **Giải thích:** Tên thông điệp không khớp giữa hai phía nên máy khách không nhận được phản hồi. Khi không nhận được thông điệp, cần kiểm tra tên ở cả máy chủ và máy khách.

## Kết luận

Qua ba thí nghiệm, em hiểu rằng cần kiểm tra trạng thái máy chủ, đường dẫn Hub và tên thông điệp khi xử lý lỗi kết nối SignalR.
