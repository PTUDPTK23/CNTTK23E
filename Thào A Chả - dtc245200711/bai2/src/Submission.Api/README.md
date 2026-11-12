# Submission.Api

ASP.NET Core Minimal API cho bai tap RESTful API. Chay tu thu muc goc cua solution:

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

## Endpoint

| Method | URI | Ket qua |
|---|---|---|
| GET | `/` | 200, trang thai dich vu |
| GET | `/api/assignments` | 200, danh sach bai tap |
| GET | `/api/assignments/{assignmentId:int}` | 200 hoac 404 `ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{assignmentId:int}/submissions` | 201 + `Location`; 400 neu bai dong/body sai; 404 neu bai khong ton tai |
| GET | `/api/submissions/{submissionId:guid}` | 200 hoac 404 `SUBMISSION_NOT_FOUND` |

Tat ca loi nghiep vu co cung dang JSON: `ApiError` gom `code` va `message`. Bai tap va luot nop duoc luu trong `List<T>` trong bo nho; khoi dong lai API se xoa cac luot nop da tao.

## DTO

- `AssignmentDto`: du lieu bai tap server tra ve.
- `CreateSubmissionRequest`: hai truong client duoc phep gui; khong nhan `Id`, `AssignmentId`, `Status` hay thoi diem tao tu client.
- `SubmissionDto`: thong tin luot nop server tra ve, bao gom cac gia tri server tu sinh.
- `ApiError`: hop dong loi dung chung.
