# EduSubmit-DTC245200287

Du an minh hoa MEP request-response gom hai tien trinh doc lap:

- `src/Submission.Api`: ASP.NET Core Minimal API tren .NET 10.
- `src/Submission.Client`: ung dung console dung `HttpClient` va JSON.

## Chay

Terminal 1:

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

## Endpoint

- `GET /`: kiem tra API.
- `GET /api/assignments`: danh sach 3 bai tap mau.
- `POST /api/submissions`: tao luot nop, tra `201 Created` va `Location`.
- `GET /api/submissions/{id}`: tra cuu luot nop theo `Guid`, tra `404` neu khong ton tai.

Du lieu duoc luu trong bo nho, nen se mat khi API khoi dong lai.
