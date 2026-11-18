# Bai 2 - RESTful API

**Sinh vien:** Hoang Van Tuan  
**MSSV:** dtc245200758  
**Mon hoc:** Phat trien ung dung phan tan

## 1. Cau truc

```text
Bai2/
├── CatalogApi/
│   └── src/
│       ├── Catalog.Api/
│       └── Catalog.Client/
├── RestfulClientServer/
│   ├── RestfulClientServer.slnx
│   └── src/
│       ├── Submission.Api/
│       └── Submission.Client/
├── requests.http
├── evidence/
└── README.md
```

## 2. Cong nghe

- .NET 10
- ASP.NET Core Minimal API
- C# Console Application
- HttpClient
- HTTP va JSON
- RESTful API

## 3. Bang hop dong API EduSubmit

| Method | URI | Thanh cong | Loi |
|---|---|---|---|
| GET | /api/assignments | 200 danh sach | - |
| GET | /api/assignments/{assignmentId} | 200 chi tiet | 404 ASSIGNMENT_NOT_FOUND |
| POST | /api/assignments/{assignmentId}/submissions | 201 + Location | 400, 404 |
| GET | /api/submissions/{submissionId} | 200 luot nop | 404 SUBMISSION_NOT_FOUND |

Moi loi nghiep vu co dang JSON:

```json
{
  "code": "INVALID_SUBMISSION",
  "message": "Thieu ma sinh vien hoac link ma nguon."
}
```

## 4. Cach chay

Mo terminal thu nhat tai thu muc Bai2:

```powershell
cd RestfulClientServer
dotnet build
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Ket qua mong doi:

```text
Now listening on: http://localhost:5000
Application started.
```

Mo terminal thu hai tai thu muc Bai2:

```powershell
cd RestfulClientServer
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Client thuc hien 4 nac:

1. GET danh sach bai tap.
2. POST nop bai, nhan 201 Created va Location.
3. GET doc lai luot nop.
4. POST du lieu sai, nhan 400 va ApiError.

## 5. Kiem thu bang requests.http

Mo file `requests.http` bang VS Code, dam bao Submission.Api dang chay o cong 5000.

Gui lan luot 7 request. Request 5 phu thuoc request 4 da chay thanh cong.

| Request | Noi dung | Ket qua mong doi |
|---|---|---|
| 1 | Danh sach bai tap | 200 |
| 2 | Chi tiet bai tap 1 | 200 |
| 3 | Bai tap 999 | 404 |
| 4 | Nop bai hop le | 201 |
| 5 | Doc lai luot nop | 200 |
| 6 | Du lieu trong | 400 |
| 7 | GUID khong ton tai | 404 |

## 6. Bang pha thu

| Ma ca | Thao tac | Ket qua mong doi | Giai thich |
|---|---|---|---|
| 5a | POST bai 1, body hop le | 201 Created | Tao luot nop |
| 5b | POST body rong | 400 INVALID_SUBMISSION | Du lieu khong hop le |
| 5c | POST bai 999 | 404 ASSIGNMENT_NOT_FOUND | Bai tap khong ton tai |
| 5d | POST khong co Content-Type | 415 | Framework khong nhan dien body JSON |
| 5e | POST JSON sai cu phap | 400 | Framework khong doc duoc JSON |
| 5f | POST thieu repositoryUrl | 400 INVALID_SUBMISSION | Thieu truong bat buoc |
| 5g | PUT /api/assignments/1 | 405 | Phuong thuc khong duoc ho tro |
| 5h | POST vao bai tap 2 da dong | 400 ASSIGNMENT_CLOSED | Bai tap da dong |

Luu y: Ca 5d, 5e, 5g la loi do framework xu ly, khong phai loi nghiep vu do handler tu tra.

## 7. Tra loi 3 cau hoi

### Cau 1. Vi sao URI dung danh tu, con hanh dong do phuong thuc HTTP dam nhan?

URI mo ta tai nguyen can thao tac, vi du `/api/assignments` la danh sach bai tap va `/api/submissions/{id}` la mot luot nop cu the.

Phuong thuc HTTP the hien hanh dong:
- GET: doc tai nguyen.
- POST: tao tai nguyen moi.
- PUT: cap nhat tai nguyen.
- DELETE: xoa tai nguyen.

Cach nay giup API co quy tac nhat quan, de doc va de mo rong.

### Cau 2. Stateless the hien o dau trong code?

Moi request HTTP tu client gui len deu chua thong tin can thiet de server xu ly. Server khong dua vao trang thai phien cua client de biet client da goi request nao truoc do.

Client gui request doc danh sach, nop bai hoac doc lai luot nop thong qua URI va body. Server xu ly request doc lap.

Du lieu mau va danh sach luot nop trong bai duoc luu trong RAM, day la du lieu ung dung, khong phai session cua client. Khi tat API, du lieu RAM se mat.

### Cau 3. Vi sao client va server khai bao DTO rieng ma van khop nhau?

Client va server la hai chuong trinh doc lap, khong can tham chieu chung code cua nhau.

Hai ben thong nhat thong qua hop dong JSON:
- Ten truong JSON.
- Kieu du lieu.
- Cau truc request va response.
- URI va HTTP method.

Vi vay moi ben co the khai bao record DTO rieng, mien la cung tuan thu hop dong JSON thi van giao tiep duoc.

Dac biet CreateSubmissionRequest chi chua StudentCode va RepositoryUrl. Server tu sinh Id, AssignmentId, Status va ReceivedAt de tranh client tu y quyet dinh cac truong nay.

## 8. Anh minh chung

Luu anh chup ket qua vao thu muc `evidence/`:

- `01-build-success.png`: Build thanh cong.
- `02-api-running.png`: API chay cong 5000.
- `03-client-result.png`: Client chay du 4 nac.
- `04-requests-http.png`: Cac request va response.
- `05-error-tests.png`: Cac ca pha thu.

Chi them anh chup ket qua chay thuc te, khong dung anh minh hoa.

## 9. Lenh build

```powershell
dotnet build RestfulClientServer/RestfulClientServer.slnx
dotnet build CatalogApi/src/Catalog.Api/Catalog.Api.csproj
dotnet build CatalogApi/src/Catalog.Client/Catalog.Client.csproj
```
