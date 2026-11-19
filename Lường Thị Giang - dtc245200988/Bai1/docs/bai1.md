
# BÀI 1 – TỔNG QUAN HỆ THỐNG VÀ CÁC MÔ HÌNH TRAO ĐỔI THÔNG ĐIỆP (MEP)

**Sinh viên:** Lường Thị Giang  
**Mã sinh viên:** DTC245200988  
**Lớp:** CNTT K23E

## 1. Môi trường phát triển
- Windows 11
- Visual Studio Code
- .NET SDK 10
- Git
- REST Client

## 2. Các mô hình trao đổi thông điệp (MEP)
Tìm hiểu các mô hình trao đổi thông điệp thông qua dự án mẫu MepDemo.

## 3. BasicWebApi
Thực hành gửi yêu cầu HTTP và quan sát các mã trạng thái:
- HTTP 200 OK: Yêu cầu thành công.
- HTTP 201 Created: Tạo tài nguyên thành công.
- HTTP 400 Bad Request: Dữ liệu gửi không hợp lệ.

## 4. MiniDistributedSystem
Tìm hiểu hệ thống gồm API Server và Console Client, trao đổi dữ liệu bằng HTTP và JSON.

## 5. Các thí nghiệm
Tìm hiểu cách hệ thống phản hồi khi thay đổi dữ liệu, dừng Server, gửi dữ liệu lỗi và thay đổi cổng kết nối.

## 6. Xây dựng EduSubmit cá nhân
Đã xây dựng hai thành phần:
- Submission.Api: Cung cấp API quản lý bài tập và bài nộp.
- Submission.Client: Gọi API và thực hiện nộp bài.

Các chức năng:
- Xem danh sách bài tập.
- Nộp bài qua HTTP POST.
- Kiểm tra thông tin bài nộp.
- Lưu bài nộp tạm thời trong bộ nhớ.
- Tra cứu bài nộp theo GUID.

## 7. Kết quả kiểm thử
- GET danh sách bài tập: HTTP 200.
- POST bài nộp hợp lệ: HTTP 201.
- POST dữ liệu không hợp lệ: HTTP 400.
- GET GUID tồn tại: HTTP 200.
- GET GUID không tồn tại: HTTP 404.

## 8. Kết luận
Qua bài thực hành, em hiểu được cách xây dựng REST API bằng ASP.NET Core, cách Client và Server giao tiếp qua HTTP/JSON và cách kiểm thử các mã trạng thái HTTP.

## 9. Hình ảnh minh chứng

![Minh chung image001](images/image001.png)

![Minh chung image002](images/image002.png)

![Minh chung image003](images/image003.png)

![Minh chung image004](images/image004.png)

![Minh chung image005](images/image005.png)

![Minh chung image006](images/image006.png)

![Minh chung image007](images/image007.png)

![Minh chung image008](images/image008.png)

![Minh chung image009](images/image009.png)

![Minh chung image010](images/image010.png)

![Minh chung image011](images/image011.png)

![Minh chung image012](images/image012.png)

![Minh chung image013](images/image013.png)

![Minh chung image014](images/image014.png)

![Minh chung image015](images/image015.png)

![Minh chung image016](images/image016.png)

![Minh chung image017](images/image017.png)

![Minh chung image018](images/image018.png)

![Minh chung image019](images/image019.png)

![Minh chung image020](images/image020.png)

![Minh chung image021](images/image021.png)

![Minh chung image022](images/image022.png)

![Minh chung image023](images/image023.png)

![Minh chung image024](images/image024.png)

![Minh chung image025](images/image025.png)

![Minh chung image026](images/image026.png)

![Minh chung image027](images/image027.png)

![Minh chung image028](images/image028.png)

![Minh chung image029](images/image029.png)

![Minh chung image030](images/image030.png)

![Minh chung image031](images/image031.png)

![Minh chung image032](images/image032.png)

![Minh chung image033](images/image033.png)

![Minh chung image034](images/image034.png)

![Minh chung image035](images/image035.png)

![Minh chung image036](images/image036.png)

![Minh chung image037](images/image037.png)

![Minh chung image038](images/image038.png)

![Minh chung image039](images/image039.png)

![Minh chung image040](images/image040.png)

![Minh chung image041](images/image041.png)

![Minh chung image042](images/image042.png)

![Minh chung image043](images/image043.png)

![Minh chung image044](images/image044.png)

![Minh chung image045](images/image045.png)

![Minh chung image046](images/image046.png)

![Minh chung image047](images/image047.png)

![Minh chung image048](images/image048.png)

![Minh chung image049](images/image049.png)

![Minh chung image050](images/image050.png)

![Minh chung image051](images/image051.png)

![Minh chung image052](images/image052.png)

![Minh chung image053](images/image053.png)

![Minh chung image054](images/image054.png)

![Minh chung image055](images/image055.png)

![Minh chung image056](images/image056.png)

![Minh chung image057](images/image057.png)

![Minh chung image058](images/image058.png)

![Minh chung image059](images/image059.png)

![Minh chung image060](images/image060.png)

![Minh chung image061](images/image061.png)

![Minh chung image062](images/image062.png)

![Minh chung image063](images/image063.png)

![Minh chung image064](images/image064.png)

![Minh chung image065](images/image065.png)

![Minh chung image066](images/image066.png)

![Minh chung image067](images/image067.png)

![Minh chung image068](images/image068.png)

![Minh chung image072](images/image072.png)

![Minh chung image073](images/image073.png)

![Minh chung image074](images/image074.png)

![Minh chung image075](images/image075.png)

![Minh chung image081](images/image081.png)

![Minh chung image082](images/image082.png)

![Minh chung image083](images/image083.png)

![Minh chung image084](images/image084.png)

![Minh chung image085](images/image085.png)

![Minh chung image086](images/image086.png)

![Minh chung image087](images/image087.png)

![Minh chung image088](images/image088.png)

![Minh chung image089](images/image089.png)

![Minh chung image091](images/image091.png)

![Minh chung image095](images/image095.png)

![Minh chung image099](images/image099.png)

![Minh chung image100](images/image100.png)

![Minh chung image101](images/image101.png)

![Minh chung image102](images/image102.png)

![Minh chung image103](images/image103.png)

![Minh chung image104](images/image104.png)

![Minh chung image106](images/image106.png)

![Minh chung image107](images/image107.png)

![Minh chung image108](images/image108.png)

![Minh chung image109](images/image109.png)

![Minh chung image110](images/image110.png)

![Minh chung image111](images/image111.png)

![Minh chung image112](images/image112.png)

