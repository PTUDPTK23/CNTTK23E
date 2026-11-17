# Submission.Client

Console client dung `HttpClient` va JSON de giao tiep voi `Submission.Api`. Chay API truoc, sau do chay tu thu muc goc solution:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Co the bo URL; mac dinh client dung `http://localhost:5000`.

Client thuc hien lan luot:

1. GET danh sach bai tap va chon bai dang mo.
2. POST tao luot nop, kiem tra phan hoi `201 Created`.
3. Doc `Location` tu response va GET lai luot nop vua tao.
4. Gui body khong hop le, in `ApiError` tu response `400`.

Client khai bao cac record DTO rieng, khop voi ten truong JSON cua API; hai project khong chia se lop C#. Client doc ma HTTP truoc khi parse body, va bao loi ket noi rieng voi loi HTTP.
