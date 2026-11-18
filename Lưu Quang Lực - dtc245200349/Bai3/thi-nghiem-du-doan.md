# Dự đoán trước khi chạy bước 7

| Thí nghiệm | Dự đoán | Lý do |
|---|---|---|
| Hai sinh viên | Mỗi client chỉ nhận ID của mình | Nhóm submission:{id} |
| API tắt | StartAsync báo connection refused | Reconnect không thử lần mở đầu |
| Sai /hub/submissions | HTTP 404 | Không khớp MapHub |
| Sai tên On | Không có push, chờ 10 giây rồi REST graded | Tên sự kiện không khớp |
| Watch muộn 4 giây | Không nhận processing/graded, REST graded | Hai sự kiện đã phát trước khi vào nhóm |
