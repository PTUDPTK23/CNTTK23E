# Kiểm tra trước khi chuẩn bị push Bài 1 ngày 18/11/2026

Kết quả dưới đây được ghi từ các lệnh và HTTP thực tế, không phải ảnh chụp màn hình.
Các cổng 15011, 15012, 15081 được dùng để tránh ảnh hưởng dịch vụ khác; URL được truyền qua tham số.

| Nội dung | Kết quả |
|---|---|
| EduSubmit API và Client chính | Build thành công, 0 lỗi; POST và GET lại cùng ID, received |
| MepDemo | Build thành công; chạy đủ 5 MEP, mỗi job được một worker xử lý |
| BasicWebApi | Build thành công; vi/en/ja đúng; HTTP 200, 201 + Location, 400 |
| MiniDistributedSystem | Build thành công; API và Client mẫu chạy ở hai tiến trình |
| EduSubmit HTTP | 200, 201 + Location, ba ca 400, GET lại 200, ID lạ 404 |

Log: [build chính](build-edusubmit.txt), [MEP](mep-all.txt), [build Basic](build-basic.txt),
[build Mini](build-mini.txt), [client chính](edusubmit-client.txt), [client mẫu](mini-client.txt),
[HTTP nguyên bản](http-results.json).

Đây là kiểm tra bổ sung trước push. Bốn thí nghiệm lỗi và 13 ảnh báo cáo cũ vẫn được ghi nhận
trong nhật ký ngày 08/11; không khai báo đã chụp thêm ảnh giao diện hoặc đã làm debug breakpoint.
