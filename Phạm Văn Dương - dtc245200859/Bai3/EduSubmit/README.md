# EduSubmit - Bai 3

EduSubmit giu nguyen REST cua Bai 2 va bo sung thong bao trang thai thoi gian thuc bang SignalR.

## Hop dong

- `GET /api/assignments`: lay danh sach bai tap.
- `GET /api/assignments/{assignmentId}`: doc chi tiet bai tap.
- `POST /api/assignments/{assignmentId}/submissions`: tao luot nop, tra `201 Created` va `Location`.
- `GET /api/submissions/{submissionId}`: doc lai luot nop theo `Location`.
- `GET/POST /hubs/submissions`: endpoint SignalR.

Client goi `WatchSubmission(submissionId)` de vao group `submission:{id}`. Server phat event `SubmissionStatusChanged` voi cac trang thai:

```json
{
  "submissionId": "...",
  "status": "processing",
  "updatedAt": "..."
}
```

Luot nop di qua `received -> processing -> graded`. REST va SignalR dung chung `SubmissionStore`, vi vay GET sau khi nhan `graded` cung tra ve `status: "graded"`.

## Chay

Mo terminal 1:

```powershell
dotnet run --project src/Submission.Api
```

Mo terminal 2:

```powershell
dotnet run --project src/Submission.Client
```

Du lieu hien dang luu trong bo nho, phu hop voi bai thuc hanh; khoi dong lai API se xoa cac submission cu.
