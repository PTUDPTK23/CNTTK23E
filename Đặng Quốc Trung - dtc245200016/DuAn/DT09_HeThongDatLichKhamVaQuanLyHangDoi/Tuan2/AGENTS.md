# AGENTS.md — Quy tắc làm việc với MediQueue

Tệp này áp dụng cho toàn bộ cây thư mục `DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan2`.

## 1. Bối cảnh cố định

- Sản phẩm: `MediQueue`, mã đề tài `09`.
- Nhánh làm việc: `duan-tuan2-dtc245200016`; nền tảng được kế thừa từ commit Tuần 1 `dcee864`.
- Stack: C# / .NET 10 / ASP.NET Core MVC / ASP.NET Core Web API / EF Core.
- SQL Server: `LAPTOP-AQFGUM9Q\SQLEXPRESS`; `MediQueue` là database Tuần 1 phải bảo toàn. Database Tuần 2 **đề xuất** là `MediQueue_Tuan2`, chưa được tạo.
- Hạ tầng phân tán: RabbitMQ, .NET Worker Service, SignalR.
- Role chính xác: `Patient`, `Doctor`, `Receptionist`, `Admin`.
- Một website và một form đăng nhập dùng chung; điều hướng theo Role sau xác thực.
- Phiên bản đầu phục vụ một cơ sở y tế, không có mô hình đa chi nhánh.
- Mỗi Doctor thuộc đúng một Department (chuyên khoa); một Department có nhiều Doctor. Không tạo đồng thời `Specialty`.
- API dùng Identity/JWT Access Token có thời hạn; MVC dùng Cookie Authentication và lưu JWT phía server. Không triển khai Refresh Token ở phiên bản đầu.
- Không sử dụng Docker dưới bất kỳ hình thức nào.
- Cấu trúc nộp bài gồm 9 thư mục `Tuan1` đến `Tuan9`; mỗi tuần đúng một Pull Request và có thể có nhiều commit thực tế.
- Pull Request Tuần 2 dự kiến có tiêu đề `DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung`; chưa có hạn nộp Tuần 2 chính thức trong yêu cầu hiện tại.
- Tuần sau phải kế thừa kết quả tuần trước; không tạo lại dự án từ đầu, không âm thầm thay thế quyết định đã chốt và không sửa nội dung tuần đã nộp nếu chưa có yêu cầu.

## 2. Ranh giới an toàn trước khi làm việc

1. Xác nhận thư mục làm việc thuộc `DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi` và xác định đúng thư mục `Tuan{n}` của tuần đang làm.
2. Chạy `git branch --show-current` và `git status --short --branch`.
3. Nếu không ở nhánh `duan-tuan2-dtc245200016`, dừng và báo cáo; không tự chuyển nhánh.
4. Chỉ sửa nội dung trong thư mục tuần được giao; không tự sửa tuần khác, README chung của lớp, `Bai1` đến `Bai9` hoặc thư mục sinh viên khác.
5. Không tạo repository Git lồng trong `DuAn` hoặc thư mục dự án.
6. Giữ nguyên thay đổi có sẵn của người dùng; không reset, checkout hay xóa chúng.

## 3. Hiện trạng nền tảng kỹ thuật

- Solution chính thức: `MediQueue.slnx`; toàn bộ 9 project target `net10.0`.
- `Domain` và `Contracts` độc lập; không được thêm phụ thuộc EF Core, ASP.NET Core hoặc RabbitMQ vào Domain.
- `Infrastructure` → `Domain`, `Contracts`.
- `Appointment.Api`, `Queue.Worker` → `Domain`, `Infrastructure`, `Contracts`.
- `Notification.Service`, `Web` → `Contracts`; Web không được tham chiếu Infrastructure hoặc truy cập DbContext/SQL Server.
- `MediQueue.Web` dùng MVC (`Controller` + View/ViewModel), gọi backend qua HTTP API; không dùng Razor Pages làm kiến trúc giao diện chính.
- `UnitTests` → `Domain`, `Contracts`; `IntegrationTests` → `Appointment.Api`, `Web`, `Infrastructure`.
- Cổng phát triển: API `5101/7101`, Web `5102/7102`, Notification `5103/7103` (HTTP/HTTPS).
- Chạy kiểm tra nền tảng bằng `dotnet restore MediQueue.slnx`, `dotnet build MediQueue.slnx --no-restore` và `dotnet test MediQueue.slnx --no-build`.
- Đã có Domain entity, `MediQueueDbContext`, Fluent API, `ApplicationUser` và Migration `20261117142359_InitialCreate`. Migration đã áp dụng vào `MediQueue` ngày 18/11/2026; schema có 21 bảng mô hình + bảng lịch sử.
- `Appointment.Api` và `Queue.Worker` dùng `UserSecretsId` riêng của `Tuan2`; không sao chép giá trị User Secrets từ Tuần 1.
- Chưa có Login/Register/JWT hoàn chỉnh, sáu API nghiệp vụ, RabbitMQ business flow hoặc SignalR Hub. Không ghi nhận các phần này là đã hoàn thành.
- Không tạo endpoint/controller giả trả thành công. Ngoài `HealthController`, chỉ tạo controller khi triển khai chức năng thật và có kiểm thử phù hợp.
- Backend tổ chức Controller theo nghiệp vụ: `AccountController`, `PatientsController`, `DoctorsController`, `AppointmentsController`, `QueuesController`, `DepartmentsController`, `RoomsController`, `TimeSlotsController`, `UsersController`, `DashboardController`, `HealthController`. Không tạo controller riêng theo từng Role.
- MVC Controller kế thừa `Controller` và chỉ phục vụ View/ViewModel; API Controller kế thừa `ControllerBase`, trả JSON/HTTP status/`ProblemDetails`. Controller không gọi controller khác.

## 4. Quy ước kiến trúc

- Giữ đúng các thành phần: `MediQueue.Web`, `MediQueue.Appointment.Api`, `MediQueue.Queue.Worker`, `MediQueue.Notification.Service`, `MediQueue.Domain`, `MediQueue.Infrastructure`, `MediQueue.Contracts`.
- Yêu cầu cấp số **bắt buộc** đi qua RabbitMQ; không cấp số đồng bộ trong HTTP request.
- Sự kiện thay đổi hàng đợi **bắt buộc** đi qua RabbitMQ trước khi Notification Service phát SignalR.
- Dùng `MediQueue.Contracts` cho message contract; không chia sẻ EF entity qua message/API.
- Backend luôn kiểm tra Role, quyền sở hữu và phạm vi bác sĩ; không xem việc ẩn nút trên UI là phân quyền.
- Thiết kế consumer theo at-least-once: message có `MessageId`, inbox/idempotency và retry có giới hạn.
- Giữ transactional outbox/inbox trong thiết kế để không mất message và chống xử lý lặp; triển khai tăng dần theo roadmap.
- Mọi thay đổi schema phải đi qua EF Core Migration được review; không tự chạy Migration lên database có sẵn.

## 5. Quy ước nghiệp vụ tối thiểu

- Không đặt lịch quá khứ, hết chỗ hoặc trùng thời gian của Patient.
- Chống overbooking bằng transaction và optimistic concurrency/điều kiện cập nhật nguyên tử.
- Trạng thái Appointment chỉ gồm `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`; phiên bản đầu không có `Called`/`Skipped`.
- Sau khi đặt thành công, Appointment ở `PendingNumber`; Worker cấp QueueTicket ngay qua RabbitMQ rồi chuyển sang `Waiting`.
- Mỗi Appointment tối đa một QueueTicket; QueueNumber là số nguyên duy nhất theo Doctor/ngày, bắt đầu lại từ 1 mỗi ngày, không tái sử dụng và hiển thị tối thiểu ba chữ số.
- Không cấp/gọi số cho Appointment đã hủy.
- Thứ tự phục vụ: khung giờ đăng ký, sau đó QueueNumber trong khung giờ; không gọi slot chưa đến giờ.
- Mỗi Doctor tối đa một lượt `Serving` tại một thời điểm.
- `call-next` chuyển trực tiếp `Waiting → Serving`; chỉ Doctor phụ trách được gọi lượt, hoàn thành hoặc xác nhận `NoShow` cho lượt kế tiếp đủ điều kiện.
- Không gọi lại `NoShow` trong phiên bản đầu.
- Patient được hủy trước `Slot.StartAt` khi Appointment chưa được phục vụ; hủy giải phóng chỗ đúng một lần nhưng không tái sử dụng QueueNumber.
- Check-in chỉ là thông tin tiếp nhận do Receptionist ghi nhận, không phải điều kiện cấp số.
- `WaitingCount` và số người phía trước phải được đếm từ bản ghi theo trạng thái/thứ tự, không suy ra bằng phép trừ QueueNumber.
- Patient chỉ truy cập dữ liệu của mình; Doctor chỉ điều phối hàng đợi của mình.
- Người dùng phải đăng nhập để theo dõi queue; không công khai dữ liệu Patient khác.
- Hủy hợp lệ phải giải phóng chỗ đúng một lần.
- SignalR client phải tự kết nối lại và tải snapshot sau khi kết nối lại.

## 6. Quy ước tài liệu và mã nguồn

- Tài liệu viết tiếng Việt có dấu, UTF-8; tên Role, class, entity, endpoint và thuật ngữ kỹ thuật dùng tiếng Anh nhất quán.
- Phân biệt `[Bắt buộc]` với `[Mở rộng]`; quyết định chưa xác nhận phải ghi **Đề xuất**.
- API dùng DTO riêng, validation ở biên hệ thống và lỗi theo `ProblemDetails`.
- Dùng thời gian UTC trong lưu trữ kỹ thuật; mọi quyết định nghiệp vụ quy đổi nhất quán theo `Asia/Ho_Chi_Minh`.
- Không chuyển JWT xuống JavaScript hoặc lưu trong `localStorage`/`sessionStorage`; cookie MVC phải HttpOnly/Secure/SameSite và form thay đổi trạng thái phải có antiforgery. SignalR phải xác thực và kiểm tra quyền group ở server.
- Không ghi secret, mật khẩu, connection string thật hoặc dữ liệu bệnh nhân thật vào Git.
- Test tối thiểu gồm unit test quy tắc nghiệp vụ, integration test API/database và test consumer idempotency.

## 7. Các hành động cần sự đồng ý rõ ràng

Không tự thực hiện các hành động sau nếu người dùng chưa yêu cầu trong lượt hiện tại:

- Khởi tạo solution/project hoặc cài package.
- Thực thi SQL, Migration, seed hoặc thay đổi database `MediQueue`.
- Cài/chỉnh RabbitMQ, Erlang hay dịch vụ Windows.
- Thay đổi công nghệ, kiến trúc hoặc bổ sung chức năng ngoài phạm vi đề tài.
- Commit, push, tạo branch, Pull Request hoặc làm giả lịch sử phát triển.

Migration `InitialCreate` đã áp dụng thành công cho `MediQueue`; không chạy lại, không sửa `__EFMigrationsHistory` và không xóa/tạo lại database. Không tạo `MediQueue_Tuan2`, không chạy Migration/seed cho tới khi người dùng xác nhận phương án. Mọi test database tương lai phải có guard đích và không được ghi vào `MediQueue` của Tuần 1.

## 8. Quy tắc lịch sử Git và Pull Request

- Mỗi tuần chỉ có một Pull Request; nhiều commit trong Pull Request được phép và nên phản ánh các lát cắt công việc thực tế.
- Không sửa, squash, chia nhỏ hoặc tạo thêm commit chỉ để mô phỏng lịch sử 9 tuần.
- Không đưa thay đổi của hai thư mục tuần vào cùng một Pull Request.
- Nếu giảng viên yêu cầu sửa một tuần đang review, tiếp tục commit và push vào đúng nhánh/PR của tuần đó; không mở PR thứ hai cho cùng tuần.
- Bảo toàn thư mục của các tuần đã nộp. Mọi điều chỉnh hồi tố phải có yêu cầu rõ ràng và được ghi vào progress của tuần liên quan.
- Trước khi đề xuất Pull Request, kiểm tra diff chỉ thuộc thư mục tuần tương ứng và báo cáo tiêu đề PR bắt buộc.
- Không tự động commit, push hoặc mở Pull Request.

## 9. Kiểm tra trước khi bàn giao

- So khớp yêu cầu bắt buộc với Use Case, API, dữ liệu và kiến trúc.
- Kiểm tra Mermaid và liên kết tài liệu.
- Chạy các kiểm tra phù hợp với đúng giai đoạn; không tạo mã chỉ để kiểm tra tài liệu.
- Báo cáo file đã đổi, cây thư mục tuần, quyết định thiết kế, điểm còn cần xác nhận, tiêu đề/hạn PR và `git status` cuối cùng.

