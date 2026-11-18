# Đề tài 15 - Hệ thống nộp bài và chấm bài tự động

Hệ thống phân tán cho phép sinh viên tra cứu bài tập, nộp bài làm, xử lý chấm điểm tự động bất đồng bộ qua RabbitMQ, lưu kết quả vào PostgreSQL và đẩy thông báo thời gian thực (SignalR) về cho sinh viên.

---

## 1. Kiến trúc hệ thống

```text
Student Client -> Submission API -> RabbitMQ -> Grading Worker -> RabbitMQ -> Notification Service -> Client
                         |                           |
                         +------> Database <---------+
```

### Cấu trúc thư mục dự án

```text
AutoGradingSystem.slnx
docker-compose.yml
requests.http
README.md
src/
├── Shared/                 # Thực thể Database (Assignment, Student, Submission), AppDbContext và контракт sự kiện RabbitMQ
├── Submission.Api/         # REST API (http://localhost:5050) cung cấp đúng 4 endpoints tối thiểu
├── Grading.Worker/         # Worker Service lắng nghe RabbitMQ, chấm điểm giả lập và cập nhật Database
├── Notification.Service/   # SignalR Service (http://localhost:5001) lắng nghe sự kiện chấm xong và đẩy thông báo realtime
└── Student.Client/         # Console App demo trọn vẹn luồng 8 bước từ nộp bài tới nhận thông báo điểm realtime
```

---

## 2. Thiết kế dữ liệu tối thiểu (Database - PostgreSQL)

Chỉ gồm đúng 3 bảng với các trường dữ liệu tối thiểu:

1. **`Assignments` (Bài tập)** — *được seed sẵn 3 bài tập mẫu khi khởi động*:
   - `Id` (`int`)
   - `Title` (`string`)
   - `Description` (`string`)
2. **`Students` (Sinh viên)** — *được seed sẵn 3 sinh viên mẫu (`SV001`, `SV002`, `SV003`)*:
   - `Id` (`string` - `studentId`)
   - `FullName` (`string`)
3. **`Submissions` (Bài nộp)**:
   - `Id` (`int`)
   - `AssignmentId` (`int`)
   - `StudentId` (`string`)
   - `Content` (`string`)
   - `SubmittedAt` (`DateTimeOffset`)
   - `Status` (`string`: khởi tạo là `"Submitted"`, sau khi chấm xong đổi thành `"Graded"`)
   - `Score` (`double?`)
   - `Comment` (`string?`)

---

## 3. Luồng chức năng chính (8 bước)

1. **Bước 1 - Tra cứu danh sách bài tập**: `Student.Client` gọi `GET /assignments` trên `Submission.Api`.
2. **Bước 2 - Nộp bài cho một bài tập**: `Student.Client` gửi `POST /assignments/{id}/submissions` kèm `studentId` và `content`.
3. **Bước 3 - Tạo submission với trạng thái `Submitted`**: `Submission.Api` lưu bản ghi vào PostgreSQL với `Status = "Submitted"` và ghi nhận `SubmittedAt`.
4. **Bước 4 - Đưa yêu cầu chấm vào RabbitMQ**: `Submission.Api` publish message `SubmissionGradingMessage` vào queue `submission.grading`.
5. **Bước 5 - Worker chấm tự động theo quy tắc giả lập**: `Grading.Worker` nhận message từ queue `submission.grading`, delay 3 giây mô phỏng quá trình chấm, sau đó tính `Score` và tạo `Comment`.
6. **Bước 6 - Lưu điểm, nhận xét và trạng thái chấm**: `Grading.Worker` cập nhật bản ghi trong PostgreSQL với `Score`, `Comment` và đổi `Status = "Graded"`.
7. **Bước 7 - Phát sự kiện khi chấm hoàn thành**: `Grading.Worker` publish `SubmissionGradedEvent` (`SubmissionId`, `StudentId`, `Status`, `Score`, `Comment`) vào queue `submission.graded`.
8. **Bước 8 - Gửi thông báo realtime và tra cứu kết quả**: `Notification.Service` tiêu thụ sự kiện từ `submission.graded` và đẩy thông báo realtime qua SignalR (`/hubs/notifications`) tới `Student.Client`; đồng thời `Student.Client` gọi `GET /submissions/{id}` và `GET /students/{studentId}/submissions` để tra cứu lại kết quả.

---

## 4. Hướng dẫn khởi chạy

### Bước 1: Khởi chạy PostgreSQL và RabbitMQ bằng Docker Compose

```powershell
docker compose up -d
```

- **PostgreSQL**: `localhost:5433` (Database: `autograding_db`, User/Pass: `postgres`/`postgres`)
- **RabbitMQ**: `localhost:5672` (Management UI: `http://localhost:15672`, User/Pass: `guest`/`guest`)

### Bước 2: Khởi chạy các dịch vụ Backend (mở 3 cửa sổ terminal)

**Terminal 1 — Chạy `Submission.Api` (cổng `5050`):**

```powershell
dotnet run --project src/Submission.Api
```

**Terminal 2 — Chạy `Grading.Worker`:**

```powershell
dotnet run --project src/Grading.Worker
```

**Terminal 3 — Chạy `Notification.Service` (cổng `5001`):**

```powershell
dotnet run --project src/Notification.Service
```

### Bước 3: Chạy `Student.Client` để demo trọn vẹn luồng 8 bước

**Terminal 4 — Chạy `Student.Client`:**

```powershell
dotnet run --project src/Student.Client
```

`Student.Client` sẽ tự động:
1. Kết nối SignalR tới `Notification.Service` (`http://localhost:5001/hubs/notifications`).
2. Gọi `GET /assignments` hiển thị danh sách bài tập.
3. Gọi `POST /assignments/1/submissions` nộp bài cho sinh viên `SV001`.
4. Chờ ~3 giây để `Grading.Worker` chấm bài và nhận thông báo điểm realtime từ `Notification.Service`.
5. Gọi `GET /submissions/{id}` và `GET /students/SV001/submissions` để tra cứu lại kết quả đã lưu trong Database.

---

## 5. Hướng dẫn kiểm thử 4 API tối thiểu

Bạn có thể sử dụng trực tiếp file `requests.http` (với tiện ích REST Client trên VS Code) hoặc chạy các lệnh `curl` dưới đây:

### 1. Lấy danh sách bài tập (`GET /assignments`)

```powershell
curl http://localhost:5050/assignments
```

### 2. Nộp bài cho bài tập có mã `{id}` (`POST /assignments/{id}/submissions`)

```powershell
curl -X POST http://localhost:5050/assignments/1/submissions `
  -H "Content-Type: application/json" `
  -d "{\"studentId\":\"SV001\",\"content\":\"https://github.com/sv001/assignment-1-solution-pass\"}"
```

- Phản hồi trả về ngay lập tức có `status: "Submitted"`, `score: null`, `comment: null`.

### 3. Tra cứu chi tiết một bài nộp (`GET /submissions/{id}`)

```powershell
curl http://localhost:5050/submissions/1
```

- Ngay sau khi nộp: `status` là `"Submitted"`.
- Sau ~3 giây (khi `Grading.Worker` chấm xong): `status` chuyển thành `"Graded"`, kèm `score` (ví dụ `9.5`) và `comment`.

### 4. Tra cứu danh sách bài đã nộp của sinh viên (`GET /students/{studentId}/submissions`)

```powershell
curl http://localhost:5050/students/SV001/submissions
```
