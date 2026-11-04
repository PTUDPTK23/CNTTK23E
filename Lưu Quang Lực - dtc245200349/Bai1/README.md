# EduSubmit – Bài 1

MSSV: DTC245200349 | Lớp: CNTT K23E

Yêu cầu: .NET 10 SDK. Mở thư mục này trong VS Code.

```powershell
dotnet build
```

Terminal 1:

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Mở requests.http bằng extension REST Client, gửi lần lượt các yêu cầu.
Gửi request có tên createSubmission trước khi đọc lại lượt nộp.
Các mã cần quan sát: 200, 201, 400, 404.

Đổi cổng: dùng cùng URL cho API (--urls), Client (sau --) và biến @api.
Dữ liệu lưu trong bộ nhớ, mất khi API khởi động lại. ConcurrentDictionary
cho phép truy cập đồng thời; đây là phần điều chỉnh so với gợi ý List trong đề.
RepositoryUrl đang là địa chỉ ví dụ; cần thay khi có repository GitHub thật.

Code và nhật ký được chuẩn bị với trợ giúp của Codex. Nhật ký ghi rõ nguồn
kết quả và phần cần người học thực hiện bổ sung. Chưa đẩy code lên GitHub.
