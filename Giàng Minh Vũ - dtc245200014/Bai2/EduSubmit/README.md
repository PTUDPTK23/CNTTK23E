# EduSubmit - Bai 2 RESTful API

## Gioi thieu

EduSubmit la bai thuc hanh Client - Server voi ASP.NET Core Minimal API va ung dung console dung `HttpClient`. API quan ly bai tap va cac luot nop trong bo nho tien trinh; khong dung database hay Entity Framework.

## Muc tieu

- Thiet ke URI theo tai nguyen, dung phuong thuc HTTP de doc, tao va xoa du lieu.
- Tra cac ma HTTP va JSON error ro rang cho loi nghiep vu.
- Dung JSON lam hop dong doc lap giua Client va Server.
- Xu ly loi HTTP, loi ket noi va kiem tra tinh trang du lieu trong RAM.

## Kien truc va cong nghe

`Submission.Client` gui request HTTP/JSON den `Submission.Api`. Server tra JSON va ma trang thai HTTP. Hai project doc lap, moi ben khai bao DTO rieng va khong tham chieu project con lai.

- .NET 10, C#, ASP.NET Core Minimal API.
- Console Client, `HttpClient`, `System.Net.Http.Json`.
- Du lieu mau va submissions la `List<T>` trong RAM.

## Cau truc thu muc

```text
EduSubmit/
|-- src/
|   |-- Submission.Api/
|   |   |-- Program.cs
|   |   `-- Submission.Api.csproj
|   `-- Submission.Client/
|       |-- Program.cs
|       `-- Submission.Client.csproj
|-- RestfulClientServer.slnx
|-- requests.http
|-- sv001.json
|-- rong.json
|-- README.md
`-- screenshots/       # Thu muc de bo sung anh ket qua
```

## DTO

| DTO | Truong | Muc dich |
|---|---|---|
| `AssignmentDto` | `id`, `title`, `description`, `isOpen` | Mo ta bai tap va trang thai nhan bai |
| `CreateSubmissionRequest` | `studentCode`, `repositoryUrl` | Du lieu Client duoc phep gui khi nop bai |
| `SubmissionDto` | `id`, `assignmentId`, `studentCode`, `repositoryUrl`, `status`, `receivedAt` | Luot nop do Server tao va tra ve |
| `ApiError` | `code`, `message` | Ma va noi dung loi nghiep vu |

## Hop dong API

| Phuong thuc | URI | Thanh cong | Loi chinh |
|---|---|---|---|
| GET | `/api/assignments` | `200`, mang `AssignmentDto` | — |
| GET | `/api/assignments/{assignmentId}` | `200`, `AssignmentDto` | `404 ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{assignmentId}/submissions` | `201`, `SubmissionDto`, header `Location` | `400`, `404`, `409` |
| GET | `/api/submissions/{submissionId}` | `200`, `SubmissionDto` | `404 SUBMISSION_NOT_FOUND` |

Mo rong: `DELETE /api/submissions/{submissionId}` tra `204` va khong co body khi xoa thanh cong.

## Ma trang thai HTTP

| Ma | Y nghia trong EduSubmit |
|---|---|
| `200 OK` | Doc thanh cong |
| `201 Created` | Tao luot nop thanh cong; `Location` tro den luot nop vua tao |
| `204 No Content` | Xoa thanh cong, khong co noi dung phan hoi |
| `400 Bad Request` | Du lieu nop khong hop le hoac bai tap da dong |
| `404 Not Found` | Khong co bai tap/luot nop; URI sai dinh dang route cung co the bi framework tu choi |
| `405 Method Not Allowed` | URI hop le nhung phuong thuc khong duoc ho tro |
| `409 Conflict` | Sinh vien da nop bai tap do |
| `415 Unsupported Media Type` | Body khong duoc gui voi Content-Type JSON |

Error nghiep vu: `ASSIGNMENT_NOT_FOUND`, `ASSIGNMENT_CLOSED`, `INVALID_SUBMISSION`, `DUPLICATE_SUBMISSION`, `SUBMISSION_NOT_FOUND`. Du lieu sai dinh dang JSON va route khong khop do framework xu ly, nen khong nhat thiet co cung `ApiError`.

## Quy tac nop bai

Server kiem tra theo thu tu: bai tap ton tai, bai tap dang mo, hai truong dau vao co gia tri khong trang, sau do moi kiem tra sinh vien da nop bai tap do chua. So sanh ma sinh vien khong phan biet chu hoa/thuong. Khi chap nhan, Server trim du lieu, tu sinh `Guid`, `AssignmentId` lay tu URI, `Status = "received"` va `ReceivedAt` theo UTC. Client khong the tu dat cac truong nay.

## Cai dat va chay

Cai .NET 10 SDK. Chay cac lenh tu thu muc repository goc:

```powershell
dotnet restore EduSubmit/RestfulClientServer.slnx
dotnet build EduSubmit/RestfulClientServer.slnx
dotnet run --project EduSubmit/src/Submission.Api/Submission.Api.csproj --urls http://localhost:5000
```

API co the kiem tra tai `http://localhost:5000/`. Mo terminal PowerShell thu hai de chay Client:

```powershell
dotnet run --project EduSubmit/src/Submission.Client/Submission.Client.csproj
```

Co the truyen dia chi API khac:

```powershell
dotnet run --project EduSubmit/src/Submission.Client/Submission.Client.csproj -- http://localhost:5999
```

Client tu lay va chi hien thi bai tap dang mo, nop vao bai mo dau tien, doc lai luot nop va gui mot body rong de minh hoa loi `400`. Moi lan chay lai Client voi API chua khoi dong lai, `SV001` se trung voi luot nop truoc va Client hien thi `409` thay vi bi crash; khoi dong lai API de xoa trang thai RAM.

## Kiem tra bang requests.http

Mo `EduSubmit/requests.http` bang REST Client trong VS Code hoac HTTP Client cua IDE, chay API va gui request theo thu tu trong file. Request GET luot nop, POST trung, DELETE va GET sau xoa dung ID tu response cua `createSubmission`. Bai tap dong can thao tac tam thoi voi gia tri `IsOpen` bai 2 trong `Program.cs`: dat `false`, restart API, gui request 11, sau do dat lai `true` va restart API.

## Kiem tra bang curl.exe va PowerShell

Chay API nhu lenh phia tren. Tu PowerShell khac, chuyen vao thu muc EduSubmit de cac duong dan JSON ben duoi dung:

```powershell
Set-Location .\EduSubmit
curl.exe -i http://localhost:5000/
curl.exe -i http://localhost:5000/api/assignments
curl.exe -i http://localhost:5000/api/assignments/1
curl.exe -i http://localhost:5000/api/assignments/999
curl.exe -i http://localhost:5000/api/assignments/abc
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@sv001.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@rong.json"
```

De gui JSON bang PowerShell ma khong phai escape chuoi:

```powershell
$body = @{ studentCode = 'SV002'; repositoryUrl = 'https://example.com/sv002/rest-api' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:5000/api/assignments/1/submissions' -Method Post -ContentType 'application/json' -Body $body

$emptyBody = @{ studentCode = ''; repositoryUrl = '' } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri 'http://localhost:5000/api/assignments/1/submissions' -Method Post -ContentType 'application/json' -Body $emptyBody
} catch {
    $_.ErrorDetails.Message
}
```

Thu nghiem cac truong hop con lai trong `requests.http`, bao gom thieu Content-Type, JSON hong, PUT, bai tap dong, nop trung, DELETE va GET sau xoa.

## Loi ket noi, HTTP va tinh trang RAM

Loi ket noi xay ra khi khong the mo ket noi toi host/cang, chang han API da tat hoac URL dung cang `5999`; khi do khong co HTTP response va `HttpRequestException.StatusCode` rong. Loi HTTP nhu `400` hoac `404` nghia la Client da nhan response tu Server. Client su dung `GetAsync`/`PostAsJsonAsync` de xem `StatusCode` truoc khi doc JSON loi. `GetFromJsonAsync` tien loi cho GET thanh cong nhung nem `HttpRequestException` neu response la 4xx/5xx, vi vay Client bao loi nay va phan biet loi co ma HTTP voi loi ket noi.

`404` tu handler assignment/submission co JSON `ApiError` va ma loi cu the. `GET /api/assignments/abc` khong khop route `{assignmentId:int}`, nen framework tra `404` voi body rong. Tat va khoi dong lai API tao tien trinh moi, do do submissions trong List RAM bien mat. Bai tap chi nhan bai khi `IsOpen` la `true`.

## Cau hoi on tap (trang 90)

### 1. Vi sao URI dung danh tu, con hanh dong do phuong thuc HTTP dam nhan?

URI dinh danh tai nguyen, con HTTP method the hien thao tac tren tai nguyen do. Vi du `/api/assignments/1` la tai nguyen bai tap so 1: `GET` doc bai tap; `/api/assignments/1/submissions` la tap luot nop cua bai tap 1 va `POST` tao mot luot nop moi. Cach nay tao URI on dinh, de hieu va dung y nghia HTTP.

### 2. Stateless the hien o dau trong code?

Client gui moi request kem URI, method va body can thiet; Server xu ly request doc lap, khong can phien hoi thoai truoc do cua Client. Vi du POST mang `StudentCode` va `RepositoryUrl`, con `assignmentId` nam tren URI. Danh sach submissions trong RAM la du lieu tai nguyen cua ung dung, khong phai session cua Client; no bi xoa khi tien trinh API dung.

### 3. Vi sao Client va Server khai bao DTO rieng ma van khop nhau?

Hai ben khop nhau qua hop dong JSON tren HTTP, khong phai qua tham chieu project hay dung chung lop C#. Ten truong JSON nhu `studentCode`, `repositoryUrl`, `id` va kieu gia tri tuong ung phai tuong thich. Vi vay Client va Server co the phat trien doc lap neu cung ton trong hop dong; thay doi ten/kieu JSON can duoc ca hai ben hoac hop dong phoi hop.

## Anh ket qua kiem thu

Thu muc `screenshots/` danh cho anh chay API, ket qua curl/requests.http, Client, loi ket noi va build. Bo sung anh chup man hinh that sau khi thuc hien cac buoc kiem tra.

## Nhat ky thuc hien

- Tao Minimal API va Console Client doc lap tren .NET 10.
- Cai dat cac endpoint bai tap, tao/doc/xoa luot nop, kiem tra loi va chong nop trung.
- Tao requests HTTP, JSON mau va tai lieu chay/kiem tra.
- Build va kiem tra HTTP/Client duoc thuc hien trong qua trinh hoan thien; xem bao cao thuc te o phan tong ket trao doi.
