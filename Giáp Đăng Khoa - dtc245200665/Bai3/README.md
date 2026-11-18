# Bài 3: Lập trình thời gian thực với ASP.NET Core SignalR

- **Sinh viên thực hiện:** Giáp Đăng Khoa
- **Mã sinh viên:** dtc245200665
- **Lớp:** CNTTK23E

---

## 1. Mục tiêu & Kiến trúc tổng quan
Bài thực hành xây dựng và kiểm thử ứng dụng giao tiếp hai chiều theo thời gian thực (Real-time) sử dụng **ASP.NET Core SignalR**, giải quyết bài toán cập nhật dữ liệu tức thì mà không cần Client phải gửi yêu cầu thăm dò (Polling) liên tục.

### Cấu trúc thư mục bài nộp
```text
Bai3/
├── PingSignalR/                    # Phần 1: Thực hành Hub, Group, Broadcast & Vòng đời kết nối
│   ├── src/
│   │   ├── Ping.Api/               # ASP.NET Core Web API cấu hình SignalR Hub (/hubs/ping)
│   │   └── Ping.Client/            # Console Client kết nối Hub, gọi RPC & tự động nối lại
│   └── PingSignalR.slnx
├── RealtimeSubmissionSystem/       # Phần 2: Hệ thống nộp bài trực tuyến EduSubmit
│   ├── src/
│   │   ├── Submission.RealtimeApi/ # REST API nộp bài kết hợp IHubContext đẩy thông báo tiến độ
│   │   └── Submission.RealtimeClient/ # Client theo dõi trạng thái chấm bài theo SubmissionId
│   ├── requests.http               # Kịch bản kiểm thử luồng REST API -> SignalR Push
│   └── RealtimeSubmissionSystem.slnx
├── anh/                            # Ảnh chụp màn hình kết quả thực thi và kiểm thử
└── README.md                       # Báo cáo tổng kết thực hành