# Bai 2

## Lenh da chay

```bash
dotnet run --urls http://localhost:5000
dotnet run --project src/Submission.Client -- http://localhost:5000
```

## Tra loi cau hoi

### Cau 1

Vi sao URI dung danh tu, con hanh dong do phuong thuc HTTP dam nhan?

Tra loi:
URI dung de dinh danh tai nguyen. Hanh dong duoc bieu dien bang cac phuong thuc HTTP nhu GET, POST, PUT, DELETE, giup API ro rang va tuan theo REST.

### Cau 2

"Stateless" the hien o dau trong code da viet?

Tra loi:
Moi request deu mang day du thong tin can thiet nhu assignmentId, studentCode va repositoryUrl. Server khong luu trang thai cua client giua cac request.

### Cau 3

Vi sao client va server moi ben khai bao DTO rieng ma van khop nhau?

Tra loi:
Vi client va server cung tuan theo hop dong JSON. Chi can ten thuoc tinh va kieu du lieu giong nhau thi du lieu se duoc serialize/deserialze dung.
