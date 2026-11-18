# EduSubmit – Bài 1

Họ tên: Lưu Quang Lực

MSSV: DTC245200349 | Lớp: CNTT K23E

Yêu cầu: .NET 10 SDK. Mở thư mục này trong VS Code.

```powershell
dotnet build EduSubmit.slnx -m:1 --tl:off
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
RepositoryUrl trong lượt nộp hợp lệ dùng repository thật: https://github.com/dtc245200349-collab/EduSubmit-DTC245200349.

Code và nhật ký được chuẩn bị với trợ giúp của Codex. Nhật ký ghi rõ nguồn
kết quả và phần cần người học thực hiện bổ sung.

## Tài liệu và bản chuẩn bị push

- [Nhật ký Bài 1](docs/bai1.md).
- [Kết quả kiểm tra trước khi push ngày 18/11/2026](docs/evidence/prepush-2026-11-18/kiem-tra.md).
- Repository cá nhân: https://github.com/dtc245200349-collab/EduSubmit-DTC245200349.
- Tag `bai1` giữ bản đã công bố ngày 08/11/2026; không ghi đè tag cũ.
- Nhánh chuẩn bị trên fork lớp: `codex/bai1-dtc245200349`. Chưa mở PR nộp muộn.

## Các chương trình mẫu đi kèm

`Demos/` chứa mã mẫu theo tài liệu học phần, tách khỏi API/Client chính trong `src/`:

| Chương trình | Mục đích | Cách chạy từ thư mục này |
|---|---|---|
| MepDemo | Mô phỏng năm MEP trong một tiến trình | `dotnet run --project Demos/MepDemo/MepDemo.csproj` |
| BasicWebApi | Route, query, body, DI, HTTP 200/201/400 | `dotnet run --project Demos/BasicWebApi/src/BasicWebApi --urls http://localhost:5081` |
| MiniDistributedSystem | API và client mẫu giao tiếp HTTP | Xem [README mẫu](Demos/MiniDistributedSystem/README.md) |

Thư mục CatalogApi nằm lồng trong bản mẫu gốc là nội dung Bài 2, không đưa vào gói Bài 1.
Trước khi build, thoát các API/client cũ để tránh khóa DLL/EXE.

## Minh chứng còn cần người học bổ sung

13 ảnh cũ là ảnh báo cáo dựng từ log/phản hồi HTTP thật, không phải ảnh chụp trực tiếp
giao diện Terminal/REST Client. Nhật ký đã ghi rõ điều này. Buổi debug với breakpoint
chưa được thực hiện; cần bổ sung ảnh đúng giao diện nếu giảng viên yêu cầu.
