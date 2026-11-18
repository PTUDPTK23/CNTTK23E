# Bài 3 — EduSubmit thời gian thực

**Hoàng Thị Kiều Anh · dtc245200107 · CNTTK23E**

Triển khai phần PI 3.3, tiết 4–5, trang 42–63 của `Bai3_SignalR_Bai-giang.pdf`.

## Luồng nộp bài

1. Console client đăng ký sự kiện `SubmissionStatusChanged` rồi mở kết nối SignalR tại `/hubs/submissions`.
2. Client chọn bài đang mở và gửi REST POST. API trả `201 Created` cùng `Location`, trạng thái `received` và thời điểm `updatedAt`.
3. Client gọi `WatchSubmission(id)` để vào nhóm `submission:{id}`.
4. Tác vụ nền mô phỏng chấm sau khoảng 1 giây (`processing`) và 3 giây kể từ khi tạo (`graded`). API lưu trạng thái trước rồi đẩy sự kiện đến nhóm bằng `IHubContext`. Mỗi thông báo gồm `submissionId`, `status` và `updatedAt`.
5. Client đọc lại URL từ `Location` khi nhận `graded`, hết thời gian chờ hoặc việc theo dõi bị gián đoạn.

API giữ các tuyến REST và cấu trúc lỗi của Bài 2, gồm `409` khi nộp trùng và `204` khi rút bài thành công. Khóa dữ liệu bao trọn thao tác kiểm tra trùng và tạo lượt nộp. Dữ liệu mẫu được lưu trong RAM. Việc vào nhóm dựa trên ID lượt nộp và bản demo chưa có xác thực quyền theo dõi.

## Cách chạy

Cần .NET SDK 10. Mở terminal tại thư mục chứa README này.

```powershell
dotnet run --project ./src/Submission.RealtimeApi --no-launch-profile --urls http://localhost:5000
```

Mở terminal thứ hai để chạy client. Dùng mã sinh viên khác cho mỗi lượt thử trên cùng một phiên API. Tham số thứ ba là URL bài nộp và có thể bỏ qua.

```powershell
dotnet run --project ./src/Submission.RealtimeClient -- http://localhost:5000 SV001
```

`requests.http` có chín ca REST và SignalR negotiate theo trang 61. Chạy ca 3 trước ca 4. Đọc ngay, sau khoảng 1 giây và sau khoảng 3 giây để quan sát các trạng thái. Nếu chỉ cần kiểm tra trạng thái cuối, chạy ca 4 sau 5 giây.

## Kết quả mong đợi

```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap REST API
Da tao luot nop: <id>
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: <id> -> processing luc <gio>
Thong bao SignalR: <id> -> graded luc <gio>
Trang thai doc lai bang REST: graded
```

### Năm thí nghiệm ở bước 7

| Thao tác | Dự đoán | Giải thích |
| --- | --- | --- |
| Chạy hai client với `SV001` và `SV002` | Mỗi client nhận thông báo của lượt nộp mình vừa tạo | Mỗi ID có một nhóm riêng |
| Chạy client khi API chưa bật | Báo lỗi kết nối | Tự kết nối lại áp dụng sau khi đã mở kết nối thành công |
| Đổi URL client thành `/hub/submissions` | Negotiate trả `404` | Đường dẫn khai báo là `/hubs/submissions` |
| Đăng ký sai tên sự kiện | Chờ hết hạn, REST đọc được `graded` | Tên ở `On` phải khớp tên máy chủ gửi |
| Chờ 4 giây rồi mới gọi `WatchSubmission` | Bỏ lỡ hai thông báo, REST đọc được `graded` | Khi vào nhóm, SignalR chỉ chuyển tiếp các sự kiện phát sinh sau đó |

Kết quả trên là dự đoán. Chưa build, khởi động ứng dụng hoặc chạy thí nghiệm trong lần tạo mã nguồn này.

### Ba câu hỏi đầu giờ

1. **Máy chủ có thể chủ động báo không?** Có. Khi client đã mở kết nối SignalR và tham gia nhóm, máy chủ dùng `IHubContext` để gửi sự kiện đến nhóm ngay khi trạng thái đổi.
2. **REST và SignalR chia việc thế nào?** REST tạo lượt nộp và trả trạng thái hiện tại. SignalR chuyển thông báo khi trạng thái thay đổi.
3. **Nếu đứt mạng lúc gửi `graded` thì sao?** Client có thể bỏ lỡ tin. Khi kết nối lại, client gọi `WatchSubmission` lần nữa. Client dùng REST GET để đọc trạng thái cuối nếu hết thời gian chờ mà chưa nhận được sự kiện.
