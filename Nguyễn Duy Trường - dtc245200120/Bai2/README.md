# BAI 2 - RESTFUL API CHO TUONG TAC CLIENT-SERVER

## 1. Thong tin sinh vien

- Ho ten: Nguyen Duy Truong
- MSSV: DTC245200120
- Lop: CNTTK23E
- Mon hoc: Phat trien ung dung phan tan
- Ngay thuc hien: 11/11/2026

## 2. Gioi thieu

Bai 2 gom hai du an:

1. Catalog API: He thong quan ly san pham.
2. EduSubmit: He thong nop bai tap qua RESTful API.

Ca hai du an su dung:
- .NET 10
- ASP.NET Core Minimal API
- C#
- HTTP va JSON
- HttpClient
- REST Client trong VS Code

## 3. Cau truc thu muc

Bai2/
- CatalogApi/
- EduSubmit-DTC245200120/
- README.md

## 4. Phan 1 - Catalog API

### Chuc nang

- GET / : Kiem tra API.
- GET /api/products : Lay danh sach san pham.
- GET /api/products/{id} : Xem chi tiet san pham.
- POST /api/products : Tao san pham moi.

### Huong dan chay

Mo Terminal tai thu muc CatalogApi:

```powershell
dotnet build
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```

Mo Terminal thu hai:

```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

### Ket qua kiem thu mong doi

| Truong hop | HTTP |
|---|---|
| GET danh sach | 200 |
| GET chi tiet hop le | 200 |
| GET khong ton tai | 404 |
| POST hop le | 201 |
| POST du lieu sai | 400 |
| POST thieu Content-Type | 415 |

## 5. Phan 2 - EduSubmit

### Chuc nang

- GET / : Kiem tra API.
- GET /api/assignments : Danh sach bai tap.
- GET /api/assignments/{id} : Chi tiet bai tap.
- POST /api/assignments/{id}/submissions : Nop bai.
- GET /api/submissions/{id} : Doc lai luot nop.
- DELETE /api/submissions/{id} : Xoa luot nop.

### Huong dan chay

Mo Terminal tai thu muc EduSubmit-DTC245200120:

```powershell
dotnet build
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Mo Terminal thu hai:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

### Ket qua kiem thu mong doi

| Chuc nang | HTTP | Ket qua |
|---|---|---|
| Lay danh sach bai tap | 200 | Thanh cong |
| Xem chi tiet bai tap | 200 | Thanh cong |
| Nop bai hop le | 201 | Created |
| Nop thieu du lieu | 400 | INVALID_SUBMISSION |
| Bai tap khong ton tai | 404 | ASSIGNMENT_NOT_FOUND |
| Luot nop khong ton tai | 404 | SUBMISSION_NOT_FOUND |
| Nop bai trung | 409 | DUPLICATE_SUBMISSION |
| Xoa luot nop | 204 | No Content |
| Xoa lai luot nop | 404 | SUBMISSION_NOT_FOUND |
| Nop bai da dong | 400 | ASSIGNMENT_CLOSED |

## 6. Thi nghiem he thong phan tan

- Client va Server chay thanh hai tien trinh rieng.
- Client giao tiep voi Server qua HTTP.
- Client gui JSON, Server tra ma trang thai va JSON.
- Khi Server dung, Client khong the ket noi.
- Khi Server khoi dong lai, du lieu luu trong RAM bi mat.
- POST co the tao tai nguyen moi.
- Kiem tra trung giup ngan mot sinh vien nop lap lai.

## 7. Tra loi cau hoi ly thuyet

### Cau 1: Vi sao URI dung danh tu?

URI dung de dinh danh tai nguyen, con phuong thuc HTTP
nhu GET, POST, DELETE the hien hanh dong.
Thiet ke nay giup API ro rang va nhat quan.

### Cau 2: Stateless the hien o dau?

Moi request mang du thong tin de Server xu ly.
Vi du assignmentId nam tren URI, studentCode va
repositoryUrl nam trong JSON body.
Server khong can ghi nho phien yeu cau truoc.
Du lieu bai nop van duoc luu trong bo nho Server.

### Cau 3: Vi sao Client va Server dung DTO rieng?

Client va Server giao tiep qua JSON theo hop dong API.
Hai ben chi can thong nhat ten thuoc tinh va kieu du lieu.
Vi vay khong can dung chung class C# van trao doi duoc.

## 8. Minh chung

Anh ket qua duoc luu trong thu muc docs/images cua tung du an.

- Anh build thanh cong.
- Anh GET 200.
- Anh POST 201.
- Anh Bad Request 400.
- Anh Not Found 404.
- Anh Conflict 409.
- Anh DELETE 204.
- Anh Client chay thanh cong.

## 9. Ket luan

Bai thuc hanh minh hoa co che giao tiep client-server
qua HTTP va JSON, thiet ke RESTful API, DTO,
ma trang thai HTTP va xu ly loi trong he thong phan tan.
