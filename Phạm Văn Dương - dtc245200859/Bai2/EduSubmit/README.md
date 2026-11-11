# EduSubmit - Bai 2

Project nay la phan EduSubmit rieng cua Bai 2, gom hai tien trinh:

- `Submission.Api`: Minimal API chay mac dinh o cong `5000`.
- `Submission.Client`: console client goi API qua HTTP va JSON.

## Chay

Mo terminal 1:

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Mo terminal 2:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

## Ba cau hoi thu hoach

1. URI dung danh tu vi URI dinh danh tai nguyen; hanh dong duoc the hien bang phuong thuc HTTP nhu GET va POST.
2. Stateless the hien o cho moi request tu mang day du thong tin can thiet; server xu ly dua tren URI, method va body JSON ma khong phu thuoc vao bien cua client.
3. Client va server khai bao DTO rieng nhung van khop nhau vi hai ben thong nhat hop dong JSON: ten truong, kieu du lieu va cau truc du lieu.
