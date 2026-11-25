# Đề tài 15 - Hệ thống nộp bài và chấm bài tự động

## 1. Mô tả đề tài

Xây dựng hệ thống cho phép sinh viên nộp bài, sau đó bài nộp được chuyển đến Worker để thực hiện chấm tự động giả lập. Khi chấm xong, kết quả được lưu và thông báo đến sinh viên.

Kiến trúc tối thiểu của hệ thống:

```text
Student Client -> Submission API -> RabbitMQ -> Grading Worker -> RabbitMQ -> Notification Service -> Client
                         |                           |
                         +------> Database <---------+
```

## 2. Chức năng chính

1. Tra cứu danh sách bài tập.
2. Nộp bài cho một bài tập.
3. Tạo submission với trạng thái `Submitted`.
4. Đưa yêu cầu chấm vào RabbitMQ.
5. Worker thực hiện chấm tự động theo quy tắc giả lập.
6. Lưu điểm, nhận xét và trạng thái chấm.
7. Phát sự kiện khi chấm hoàn thành.
8. Gửi thông báo realtime cho sinh viên và cho phép tra cứu kết quả.

## 3. API tối thiểu

```text
GET  /assignments
POST /assignments/{id}/submissions
GET  /submissions/{id}
GET  /students/{studentId}/submissions
```

Dữ liệu tối thiểu gồm bài tập, sinh viên, bài nộp, thời điểm nộp, trạng thái chấm và kết quả.
