# Bài 3 – Giao tiếp thời gian thực với SignalR

Sinh viên: **Đàm Văn Việt** · MSSV: **dtc245200682** · GitHub: **Viet0810**
Email: dtc245200682@ictu.edu.vn

Đề: `Bai3_SignalR_Bai-giang.pdf`, 72 trang. Yêu cầu nộp trang 63, thang điểm trang 70–71.

## Nhật ký triển khai

- Bước Ping 1: tạo solution, hai project .NET 10; thêm SignalR.Client 10.0.15 đúng phiên bản trang 25.
- NuGet bị chặn mạng trong sandbox; đã xin chạy tải gói ngoài sandbox.

## Dự đoán trước 5 thí nghiệm EduSubmit (trang 58)

| Thí nghiệm | Dự đoán trước khi chạy |
|---|---|
| Hai client SV001, SV002 | Mỗi client chỉ nhận ID mình Watch; không lẫn trạng thái. |
| API tắt | StartAsync báo lỗi kết nối, tự nối lại không cứu lần mở đầu. |
| Sai /hub/submissions | Bắt tay trả HTTP 404. |
| Sai SubmissionStatusChange | Không có callback, chờ 10 giây rồi REST trả graded. |
| Watch chậm 4 giây | Lỡ cả processing và graded; REST vẫn đọc graded. |

Các kết quả sẽ được bổ sung từ lần chạy thực tế, không dùng output mẫu làm minh chứng.
