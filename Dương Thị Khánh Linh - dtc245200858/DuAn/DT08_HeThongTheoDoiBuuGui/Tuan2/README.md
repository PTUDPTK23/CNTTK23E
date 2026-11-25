\# TUẦN 2 – HỆ THỐNG THEO DÕI BƯU GỬI (SIGNALR)



\*\*Sinh viên:\*\* Dương Thị Khánh Linh  

\*\*MSSV:\*\* dtc245200858  

\*\*Đề tài:\*\* 08 – Hệ thống theo dõi bưu gửi



\## 1. Mục tiêu



Tìm hiểu và áp dụng SignalR để xây dựng chức năng theo dõi trạng thái bưu gửi theo thời gian thực (Realtime).



\## 2. Nội dung thực hiện



\- Tạo `ParcelHub` kế thừa lớp `Hub`.

\- Đăng ký dịch vụ SignalR bằng `AddSignalR()`.

\- Ánh xạ Hub bằng `MapHub()`.

\- Kết nối SignalR từ ứng dụng Console Client.

\- Sử dụng Group để quản lý các Client theo dõi từng mã bưu gửi.

\- Sử dụng `WatchParcel()` để bắt đầu theo dõi.

\- Sử dụng `StopWatchingParcel()` để ngừng theo dõi.

\- Sử dụng `Clients.Group()` và `SendAsync()` để gửi thông báo khi trạng thái bưu gửi thay đổi.



\## 3. Công nghệ sử dụng



\- C# và .NET 10

\- ASP.NET Core Minimal API

\- SignalR

\- Console Application

\- Visual Studio Code / PowerShell



\## 4. Cách chạy chương trình



Mở Terminal thứ nhất, chạy API:



```powershell

dotnet run --project src/Parcel.Api --urls http://localhost:5100

```



Mở Terminal thứ hai, chạy Client:



```powershell

dotnet run --project src/Parcel.Client -- http://localhost:5100

```



\## 5. Các chức năng đã kiểm thử



1\. Kết nối SignalR thành công.

2\. Theo dõi bưu gửi BG001.

3\. Cập nhật trạng thái bưu gửi và nhận thông báo Realtime.

4\. Ngừng theo dõi BG001.

5\. Cập nhật trạng thái sau khi ngừng theo dõi và xác nhận Client không còn nhận thông báo.



\## 6. Kết quả



Hai project `Parcel.Api` và `Parcel.Client` đều build thành công bằng lệnh `dotnet build`.



Chức năng theo dõi và ngừng theo dõi bưu gửi Realtime đã được kiểm thử thành công.

