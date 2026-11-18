# Bài 3 — EduSubmit realtime
Phần diễn giải chi tiết theo từng mục của bài giảng, quy trình chạy và tiêu chí nghiệm thu nằm trong [GIAI-THICH-BAI-GIANG.md](GIAI-THICH-BAI-GIANG.md). Các ảnh theo yêu cầu trang 22–23 nằm trong images/; PNG là bản trình bày lại log đầu ra thực tế của chương trình, không phải ảnh chụp màn hình desktop.


Bài 3 minh họa cách kết hợp API REST và SignalR để cập nhật trạng thái chấm bài realtime. Giao diện web responsive nằm trong `EduSubmit.Api/wwwroot`; hình minh họa dashboard được dựng bằng CSS và SVG inline.

## Yêu cầu

- .NET 10 SDK
- Kết nối internet khi mở dashboard để tải SignalR JavaScript client và font chữ

## Chạy API và giao diện

```powershell
dotnet run --project EduSubmit.Api --urls http://localhost:5000
```

Mở giao diện tại `http://localhost:5000/`. Các endpoint và request mẫu nằm trong `EduSubmit.requests.http`.

## Chạy máy khách EduSubmit

Mở terminal khác:

```powershell
dotnet run --project EduSubmit.Client -- http://localhost:5000 SV001
```

Client lấy bài tập, nộp qua REST, theo dõi nhóm `submission:{id}` bằng SignalR và đọc lại trạng thái sau khi chấm xong. Mở thêm terminal để theo dõi một lượt nộp thứ hai. Để mô phỏng nhánh `failed`:

```powershell
dotnet run --project EduSubmit.Client -- http://localhost:5000 SV002 fail
```

## Chạy ví dụ PingHub

```powershell
dotnet run --project EduSubmit.Client -- ping http://localhost:5000 An
```

Mở terminal khác với tên `Binh` để xem Echo, tin nhắn nhóm và broadcast.

## Build

```powershell
dotnet build EduSubmit.slnx
```

