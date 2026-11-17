# Bai 2 - RESTful API cho tuong tac client-server

## Muc tieu

Dung `Submission.Api` va `Submission.Client` thanh hai tien trinh rieng trao doi qua HTTP/JSON. Du lieu mau duoc giu trong bo nho nen se mat khi API khoi dong lai.

## Cau truc

- `src/Submission.Api`: Minimal API voi health check va 4 endpoint nghiep vu.
- `src/Submission.Client`: client console thuc hien chuoi liet ke -> tao -> doc lai -> gui yeu cau loi.
- `requests.http`: 9 ca request kiem tra thanh cong va loi.

## Lenh da chay

```powershell
dotnet --version
dotnet new sln -n RestfulClientServer
dotnet new web -n Submission.Api -o src/Submission.Api
dotnet new console -n Submission.Client -o src/Submission.Client
dotnet sln add src/Submission.Api/Submission.Api.csproj
dotnet sln add src/Submission.Client/Submission.Client.csproj
dotnet new gitignore
dotnet build RestfulClientServer.slnx -m:1 /p:UseSharedCompilation=false
```

SDK tren may: `10.0.403`.

## Chay thu

Terminal 1:

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Hoac mo `requests.http` trong VS Code co REST Client, bam **Send Request** tren tung request. Hay gui POST tao truoc GET doc lai; GET doc lai dung id tu phan hoi POST. API tra `Location` de client khong phai tu ghep URI.

## Hop dong API

| Phuong thuc | URI | Thanh cong | Loi nghiep vu |
|---|---|---|---|
| GET | `/api/assignments` | 200 + danh sach bai tap | - |
| GET | `/api/assignments/{assignmentId}` | 200 + bai tap | 404 `ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{assignmentId}/submissions` | 201 + `Location` + `SubmissionDto` | 400 `ASSIGNMENT_CLOSED` / `INVALID_SUBMISSION`; 404 `ASSIGNMENT_NOT_FOUND` |
| GET | `/api/submissions/{submissionId}` | 200 + `SubmissionDto` | 404 `SUBMISSION_NOT_FOUND` |

Moi loi do handler sinh deu co dang `ApiError { code, message }`. Lo trinh POST kiem tra bai tap ton tai, bai con mo, sau do moi kiem tra du lieu dau vao. Cac route co `{id:int}` hoac `{id:guid}`; URI sai dinh dang khong khop route va framework tu tra 404.

## Bang pha thu

| Ca | Yeu cau | Ket qua mong doi | Ket qua thuc te |
|---|---|---:|---|
| 1 | GET `/` | 200 | 200 |
| 2 | GET `/api/assignments` | 200 | 200, 3 bai tap |
| 3 | GET `/api/assignments/1` | 200 | 200 |
| 4 | GET `/api/assignments/999` | 404 `ASSIGNMENT_NOT_FOUND` | 404 |
| 5 | POST body hop le vao bai 1 | 201 + Location | 201, client doc id tu Location |
| 6 | GET submission theo id vua tao | 200 | 200, du lieu khop voi submission vua tao |
| 7 | POST thieu ma SV/repository vao bai 1 | 400 `INVALID_SUBMISSION` | 400 `INVALID_SUBMISSION` |
| 8 | POST vao bai 3 da dong | 400 `ASSIGNMENT_CLOSED` | 400 `ASSIGNMENT_CLOSED` |
| 9 | POST vao bai 999 | 404 `ASSIGNMENT_NOT_FOUND` | 404 `ASSIGNMENT_NOT_FOUND` |

Da chay API va client tren may hien tai; health check, chuoi 4 buoc client, va cac status 200/201/400/404 da duoc xac nhan. Build solution thanh cong, 0 warning, 0 error.

## Ba cau hoi

1. **Vi sao URI dung danh tu, con hanh dong do phuong thuc HTTP dam nhan?** URI dinh danh tai nguyen on dinh, con GET/POST/PUT/PATCH/DELETE the hien thao tac. Cach nay giup client suy ra y nghia va cho phep dung cung tai nguyen voi nhieu thao tac ma khong tao cac URI dang ham nhu `/getAssignments` hay `/doSubmit`.
2. **Stateless the hien o dau trong code?** Moi request tu mang `assignmentId` tren URI va cac truong can thiet trong body; server khong can nho buoc truoc cua client. Danh sach bai tap va submission van duoc server luu trong bo nho, nen stateless khong co nghia la khong luu du lieu.
3. **Vi sao client va server khai bao DTO rieng ma van khop nhau?** Hai ben doc/ghi cung hop dong JSON: ten truong va kieu du lieu tuong ung (vi du `studentCode`, `repositoryUrl`, `id`, `status`). DTO doc lap giup moi ben thay doi noi bo ma khong can chia se lop C# hay mo hinh noi bo.

## Git va PR

De bai yeu cau commit trong fork repo lop va mo PR voi tieu de `Bai2 - MSSV - Ho ten`. Repo hien tai la `thaoacha68-web/CNTTK23E`, chua co thong tin remote cua repo lop/fork dich. Hay dua thay doi nay vao thu muc Bai2 cua fork duoc giao, commit va mo PR vao repo lop sau khi xac dinh repo dich.
