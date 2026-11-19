# Nhật ký tiến độ MediQueue

## 1. Trạng thái tổng quan

| Thuộc tính | Giá trị |
|---|---|
| Nhánh | `duan-tuan2-dtc245200016` |
| Thư mục nộp | `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan2` |
| Tuần | Tuần 2 — MVC Authentication/Authorization và UI theo Prompt 03.3.1 |
| Nguồn kế thừa | Commit Tuần 1 `dcee864` — 177 tracked file |
| Pull Request | Chưa tạo; dự kiến `DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung` |
| Hạn nộp | Chưa được cung cấp |
| Trạng thái mã nguồn | MVC Cookie, JWT session phía server, typed API client và account UI đã triển khai; dashboard/CRUD chưa triển khai |
| Trạng thái database | Duy nhất `MediQueue`; Prompt 03.3.1 không thực hiện SQL, Migration hoặc ghi database |
| Trạng thái Git | Thay đổi Prompt 03.3.1 chưa stage/commit; không sửa tracked file ngoài `Tuan2` |

## 2. Nhật ký thực tế

### 2026-11-19 — Prompt 03.3.1: MVC Authentication, Authorization và premium UI/UX

**Đã hoàn thành:**

- Khôi phục đúng baseline `cf0791c`; trước khi làm không có tracked/staged/untracked change trong `Tuan2`. Untracked cũ ở `Bai3`/`Tuan1` được giữ nguyên.
- Triển khai typed `AccountApiClient` qua `IHttpClientFactory`, HTTPS base URL, timeout/cancellation, Bearer theo từng request và ánh xạ `ProblemDetails`/`ValidationProblemDetails`; không retry command, không bypass certificate.
- Triển khai `IAuthenticationSessionStore` in-memory singleton với session handle ngẫu nhiên 256 bit, ràng buộc `UserId`, absolute expiry và delete; browser không nhận JWT.
- Cấu hình cookie `.MediQueue.Auth` HttpOnly/Secure/SameSite=Lax, tối đa 30 phút, không sliding; Data Protection và antiforgery cookie Secure/Strict.
- Triển khai MVC `AccountController`: login/register Patient/profile/change password/access denied và logout POST-only; return URL local, xóa password trước khi render lỗi, xử lý 401/403/API unavailable.
- Kiểm tra phiên riêng tư qua `GET /api/account/me`; đổi mật khẩu và logout xóa session/cookie local, logout vẫn an toàn khi API tạm lỗi.
- Xây dựng giao diện healthcare responsive cho Home/Login/Register/Profile/ChangePassword/AccessDenied/Error; có validation/loading/toast/password toggle/checklist, focus, hover/active, page entrance và reduced motion.
- Dùng Be Vietnam Pro 400/500/600/700 lưu local từ Google Fonts chính thức, kèm SIL OFL 1.1; logo/icon/minh họa SVG tự xây dựng, không dùng asset bệnh nhân thật hoặc template thương mại.
- Không tạo dashboard/sidebar/CRUD placeholder; không thêm package; giữ nguyên 9 project và Web không tham chiếu Infrastructure.
- Bổ sung test session isolation/expiry, ViewModel, typed client/error mapping, cookie flags/no JWT, antiforgery, login success/failure, return URL, profile, password và logout.
- Build solution đạt `0` warning/`0` error. Automated test hiện đạt 18 unit + 49 integration, 1 database read test skip opt-in, `0` fail.

**Kết quả gate hiện tại:**

- Restore thành công; build đạt `0` warning/`0` error; test đạt 18 unit + 49 integration, 1 database read test skip opt-in, `0` fail; format verification đạt.
- NuGet audit trực tuyến không phát hiện package vulnerable trong cả 9 project.
- HTTP smoke đạt: API health 200, Account API không token 401; Web home/health/error/access-denied và toàn bộ CSS/JS/4 font/SVG kiểm tra đều 200; anonymous profile trả 302 về shared login. Các process do lượt kiểm tra tạo đã dừng.
- UTF-8 strict đạt, 0 liên kết Markdown nội bộ hỏng; Web chỉ tham chiếu Contracts; không có Migration diff hoặc chuỗi JWT/browser storage trong Views/static asset.
- HTTPS thực tế **NOT VERIFIED**: `dotnet dev-certs https --check --trust` trả `No valid certificate found`; không tự tạo/trust/clean certificate theo ràng buộc.
- Visual QA bằng browser automation **NOT RUN** vì native computer-use pipe không tồn tại. Responsive/accessibility đã được kiểm tra bằng code review, CSS breakpoint, integration render/static asset và reduced-motion rule; chưa có bằng chứng screenshot trình duyệt thật.
- Sandbox ghi nhận key ring DPAPI mặc định cũ không giải mã được; không xóa key cũ. Web Development dùng ephemeral Data Protection, non-Development dùng key ring riêng + DPAPI; cần chạy lại HTTPS account flow trong phiên Windows của sinh viên sau khi certificate hợp lệ.

**Không thực hiện:** không SQL/Migration/database write; không reset Admin; không Docker; không sửa `Tuan1`/`Bai3`; không stage/commit/push/PR; không chuyển Prompt 03.4.

### 2026-11-19 — Prompt 03.2: Identity, JWT và Account Management backend

- Tiền kiểm đúng nhánh `duan-tuan2-dtc245200016`; commit nền tảng `24d4e42` có subject `chore(tuan2): initialize week two foundation`.
- Khảo sát SQL read-only: `MediQueue` ONLINE, 22 user table, đúng `InitialCreate`; ban đầu Users/Roles/UserRoles đều 0.
- Triển khai password policy, lockout, unique email, Patient registration transaction, login chung, logout, me, profile và change-password.
- JWT HS256 30 phút, issuer/audience chính thức, key tối thiểu 256 bit, clock skew tối đa 30 giây; mỗi request kiểm tra active/lock/Role/fingerprint SecurityStamp.
- Thêm rate limit cho register/login, `ProblemDetails`, `no-store` cho login và cơ chế bootstrap Role/Admin tường minh không hard-code password.
- Không thay đổi entity shape, DbContext, Fluent API, `InitialCreate` hay snapshot; không tạo Migration/database mới.
- Bootstrap tạo đúng 4 Role `Admin`, `Doctor`, `Patient`, `Receptionist`; lần chạy thứ hai không thay đổi. Sau bootstrap: Users 0, Roles 4, UserRoles 0; không tự tạo Admin.
- Build đạt `0` warning/`0` error. Test an toàn đạt 18 unit + 31 integration; 1 database read test skip theo cơ chế opt-in.
- `dotnet format --verify-no-changes` đạt; NuGet audit không phát hiện package vulnerable trong 9 project; EF xác nhận không có pending model changes.
- Smoke test HTTP: API health/OpenAPI 200 (7 path, có Account API), `/api/account/me` không token 401, register sai validation 400; Web home/health/error/CSS/JS 200; Notification health 200; Queue Worker duy trì running. Toàn bộ process thử nghiệm đã dừng; không ghi user test. HTTPS chưa smoke vì `dotnet dev-certs https --check` báo chưa có certificate hợp lệ.
- Write lifecycle test trên SQL thật không chạy vì chưa có database test cô lập; không tạo user test trong `MediQueue`.
- Không tự tạo Admin; chỉ cung cấp lệnh nhập mật khẩu ẩn cho người dùng chủ động thực hiện.

### 2026-11-19 — Prompt 03.1.1: khởi tạo nền tảng Tuần 2

- Xác nhận nhánh `duan-tuan2-dtc245200016`, tracked diff/staged diff ban đầu rỗng và commit nguồn chính xác `dcee864`.
- Ghi nhận 1.344 untracked file cũ ngoài `Tuan2`, tất cả thuộc `bin/obj` trong `Bai3` và `Tuan1`; không xóa, sửa hoặc di chuyển chúng.
- Dùng `git archive` để kế thừa đúng 177 tracked file của `Tuan1`; không sao chép build output, secret hoặc cấu hình local.
- Giữ nguyên 9 project, Domain, DbContext, Fluent API, `InitialCreate`, snapshot và SQL review script; tách `UserSecretsId` API/Worker cho Tuần 2.
- Rà soát kiến trúc, Domain, EF, MVC/API boundary, DI, configuration và test database read-only.
- Chuẩn hóa thiết kế API JWT + MVC Cookie + JWT token store phía server; bổ sung phân quyền, database isolation, UI/UX, test/risk/acceptance plan trong `week02-plan.md`.
- Không triển khai Login/Register/JWT/CRUD/RabbitMQ/SignalR nghiệp vụ; không tạo Migration/database/seed và không ghi vào `MediQueue`.
- Restore thành công; build đạt `0` warning, `0` error sau khi làm mới NuGet audit. Kiểm tra advisory xác nhận cả 9 project không có package bị báo vulnerable theo nguồn hiện tại.
- Unit test đạt `15/15`; integration test đạt `18`, skip `1` test SQL read-only vì chủ động không cung cấp connection string (`0` fail). Tổng: `33` pass, `1` skip.
- `dotnet ef migrations has-pending-model-changes` xác nhận model khớp snapshot; không tạo Migration và không kết nối database để ghi.
- Smoke test HTTP đạt 200 cho API health, MVC home/health/error/CSS/JavaScript và Notification health; Queue Worker duy trì running. Bốn process do lượt kiểm tra tạo đã được dừng.
- `dotnet format --verify-no-changes --no-restore` trả exit code `0`; workspace loader chỉ từng báo cảnh báo audit mạng trước khi NuGet restore được làm mới.
- Metadata SQL read-only: `MediQueue` ONLINE, 22 user table/21 model table, đúng một `InitialCreate`, không FK/check invalid; không database mới nào được tạo.
- Git cuối: vẫn ở `duan-tuan2-dtc245200016`, HEAD `d7ada98`, không tracked/staged diff; `Tuan2` có 178 file dự kiến nộp và không có `bin/obj` trong status. 1.344 untracked file cũ ngoài `Tuan2` giữ nguyên danh sách/hash ban đầu.

### 2026-11-17 — Phân tích và thiết kế nền tảng

**Đã thực hiện:**

- Xác nhận working directory dự án thuộc `DuAn`, nhánh `duan-dtc245200016` và working tree ban đầu sạch.
- Tổng hợp phạm vi, Actor, yêu cầu chức năng/phi chức năng và ma trận truy vết đề tài 09.
- Đặc tả Use Case, quy tắc nghiệp vụ, state machine và các tình huống cạnh tranh.
- Thiết kế kiến trúc phân tán với Appointment API, Queue Worker, RabbitMQ, Notification Service và SignalR.
- Thiết kế transactional outbox/inbox, idempotency, retry/DLQ và queue version.
- Thiết kế logic database/ERD, transaction boundaries và constraint dự kiến.
- Thiết kế sáu API tối thiểu cùng các API queue/auth/admin cần thiết.
- Lập roadmap 9 tuần và hướng dẫn làm việc cho các lượt Codex tiếp theo.

**Không thực hiện:**

- Không tạo solution/project hoặc mã nguồn ứng dụng.
- Không cài package, không chạy SQL/Migration và không thay đổi database `MediQueue`.
- Không cài/chỉnh RabbitMQ/Erlang.
- Không commit, push hoặc tạo Pull Request.

**Trạng thái review:** Người dùng đã review Prompt 01 và cung cấp quyết định thiết kế chính thức; bộ tài liệu đang chờ review chuẩn hóa lần cuối trước commit đầu tiên.

### 2026-11-17 — Chuẩn hóa theo quyết định thiết kế chính thức

- Rà soát toàn bộ 11 tệp tài liệu và loại bỏ mô hình trạng thái cũ `Called/Booked/Confirmed/InProgress/Skipped`.
- Thống nhất Appointment: `PendingNumber`, `Waiting`, `Serving`, `Completed`, `Cancelled`, `NoShow`.
- Chốt QueueNumber theo Doctor/ngày, bắt đầu 1, định dạng hiển thị tối thiểu ba chữ số và không tái sử dụng.
- Chốt Worker cấp QueueTicket ngay sau đặt lịch; check-in chỉ là metadata tiếp nhận.
- Chốt `call-next` chuyển thẳng `Waiting → Serving`; chỉ Doctor phụ trách được gọi/complete/no-show.
- Chốt hạn hủy trước `Slot.StartAt`, queue yêu cầu đăng nhập, một cơ sở y tế và mỗi Doctor thuộc một chuyên khoa (nay chuẩn hóa tên entity là `Department`).
- Chốt JWT Access Token có thời hạn, không Refresh Token ở v1; SignalR xác thực và kiểm tra group.
- Cập nhật ERD, API, Use Case, Actor, authorization, business rules, state diagram, architecture và roadmap tương ứng.
- Không tạo mã nguồn, không chạy SQL/Migration và không thay đổi database.

### 2026-11-17 — Chuẩn hóa cấu trúc nộp Tuần 1

- Xác nhận đúng nhánh `duan-dtc245200016` và working tree sạch trước khi di chuyển.
- Tạo `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1` theo README repository lớp.
- Di chuyển nguyên vẹn `README.md`, `AGENTS.md` và 9 tài liệu trong `docs` vào `Tuan1`; không phát hiện tệp đích trùng.
- Cập nhật quy định 9 tuần: mỗi tuần một thư mục, một Pull Request và có thể có nhiều commit thực tế.
- Ghi nhận tiêu đề PR Tuần 1 và hạn nộp 23h59 ngày 17/11/2026.
- Không commit, push hoặc tạo Pull Request.

### 2026-11-17 — Rà soát chất lượng hồ sơ Tuần 1

- Xác nhận cấu trúc `Tuan1` đã đúng nên không di chuyển hoặc tạo bản sao lần nữa.
- Rà soát đủ bài toán, lý do xây dựng, mục tiêu, phạm vi, 4 Actor, phân quyền, requirement, Use Case và business rule.
- Xác nhận có Architecture, ERD, Activity, Sequence, State Diagram cùng thiết kế RabbitMQ/SignalR.
- Bổ sung README với bài toán thực tế, mục tiêu, Actor, kết quả Tuần 1 và kế hoạch Tuần 2; không tạo tiến độ hoặc kết quả kiểm thử giả.
- Bổ sung nguyên tắc tuần sau kế thừa tuần trước, bảo toàn tuần đã nộp và tiếp tục dùng đúng PR nếu giảng viên yêu cầu sửa.
- Không tạo mã nguồn, solution, package, SQL/Migration, commit, push hoặc Pull Request.

### 2026-11-17 — Prompt 02.1: khởi tạo solution và nền tảng kiến trúc

- Xác nhận working directory, nhánh `duan-dtc245200016`, remote và working tree ban đầu sạch; máy có .NET SDK `10.0.102` và runtime .NET `10.0.2`.
- Tạo `MediQueue.slnx`, 7 project trong `src`, 2 project xUnit trong `tests`; tất cả target `net10.0` và có ProjectReference đúng hướng, không vòng phụ thuộc.
- Thêm cấu hình `global.json`, `Directory.Build.props`, `.editorconfig`, `.gitignore`; bật Nullable, ImplicitUsings và deterministic build.
- Cài package ổn định cho EF Core SQL Server/Design, Identity EF Core, JWT Bearer và ASP.NET Core integration testing; rà soát không còn package có advisory lỗ hổng theo NuGet source hiện tại.
- Loại bỏ WeatherForecast/code/test mẫu. Thêm enum trạng thái Appointment/Queue, bốn Role, health contract, health endpoint và ba kiểm thử nền tảng có ý nghĩa.
- Chỉ triển khai `HealthController`; các controller/API nghiệp vụ chưa được tạo để tránh giả trạng thái hoàn thành.
- Khảo sát metadata SQL Server read-only: kết nối Windows Authentication thành công; SQL Server `16.0.1000.6` Express; database hiện không có user table và không có `__EFMigrationsHistory`.
- `dotnet restore` thành công; build thành công `0` warning, `0` error; test đạt `3/3` (2 unit, 1 integration).
- Smoke test đạt: API `/health` 200 tại 5101; Web `/health` và `/` 200 tại 5102; Notification `/health` 200 tại 5103; Worker host khởi động đúng thông báo chưa có consumer nghiệp vụ. Các tiến trình smoke test đã dừng.
- `dotnet dev-certs https --check` báo chưa có development certificate hợp lệ; chưa tạo/trust chứng chỉ và chưa smoke test HTTPS. Launch profile/port HTTPS đã được cấu hình để hoàn thiện sau khi người dùng chủ động chuẩn bị certificate.
- Không tạo hoặc áp dụng Migration, không thực thi SQL ghi, không sửa database, không cài/chỉnh RabbitMQ, không commit/push/tạo Pull Request.

### 2026-11-17 — Prompt 02.2: EF Core Code First và Migration review

- Xác nhận nhánh đúng, working tree ban đầu sạch và Prompt 02.1 đã được lưu tại commit `c694cab`.
- Chốt Controller backend theo nghiệp vụ; loại bỏ phương án controller riêng cho Patient/Doctor/Receptionist/Admin. Frontend vẫn chia khu vực theo bốn Role.
- Chốt một thực thể `Department` cho chuyên khoa, không tạo đồng thời `Specialty`; dùng khóa `Guid` nhất quán.
- Tạo Domain entity: Patient, Doctor, Department, Room, DoctorSchedule, TimeSlot, Appointment, DailyQueue, QueueTicket, QueueTransition; thêm invariant capacity, thời gian, state transition, counter và ngày nghiệp vụ Việt Nam.
- Tạo `ApplicationUser : IdentityUser<Guid>`; không tạo bảng tài khoản riêng, không seed user/password/Role.
- Tạo `MediQueueDbContext`, design-time factory, DI cho API/Worker và các `IEntityTypeConfiguration` riêng.
- Tạo các entity persistence OutboxMessage, InboxMessage, IdempotencyRequest, AuditLog; chuẩn bị retry, deduplication, idempotency và audit.
- Ánh xạ PK/FK, required/max length, enum string, 12 check constraint, unique/filtered index, delete behavior và rowversion.
- Bổ sung unit test invariant và test EF metadata; vòng kiểm tra cuối đạt 24/24 test (15 unit, 9 integration/model), build `0` warning và `0` error.
- Sinh Migration `InitialCreate`, Designer, ModelSnapshot và script idempotent `docs/sql/InitialCreate.sql`; `has-pending-model-changes` xác nhận snapshot khớp model.
- Kiểm tra metadata SQL Server sau cùng xác nhận `UserTableCount = 0`, `HasMigrationHistory = 0`.
- Không chạy `database update`, không thực thi script, không dùng EnsureCreated/EnsureDeleted/Migrate và không thay đổi database `MediQueue`.

### 2026-11-18 — Prompt 02.2.1: refactor ASP.NET Core MVC

- Xác nhận baseline Prompt 02.2 tại commit `7273871`, local/origin đồng bộ và working tree ban đầu sạch.
- Chuyển `MediQueue.Web` từ `AddRazorPages`/`MapRazorPages` sang `AddControllersWithViews` và conventional MVC route.
- Chuyển trang chủ, trang lỗi, Layout, validation partial, ViewImports/ViewStart và scoped CSS từ `Pages` sang `Views`; loại bỏ PageModel và thư mục `Pages` cũ sau khi MVC test đạt.
- Tạo `HomeController` mỏng và `ErrorViewModel`; navigation dùng `asp-controller`/`asp-action`.
- Giữ `HealthController` API; không tạo controller/endpoint nghiệp vụ rỗng hoặc giả.
- Bổ sung integration test cho MVC page, error view, health endpoint và Bootstrap/CSS/JavaScript static assets.
- Bảo toàn chín project, ProjectReference kiến trúc và toàn bộ Domain/Identity/DbContext/Fluent API/Migration; Web vẫn không tham chiếu Infrastructure.
- Kiểm tra EF model không có pending change; database read-only vẫn `UserTableCount = 0`, `HasMigrationHistory = 0`.
- Restore thành công; build `0` warning/`0` error; 32/32 test đạt (15 unit, 17 integration); format verification đạt.
- Smoke test HTTP đạt cho MVC `/`, `/health`, `/Home/Error`, Bootstrap asset; API/Notification `/health` đều 200 và Worker khởi động đúng. Toàn bộ process thử nghiệm đã dừng.
- Không cài package/công cụ, không tạo Migration, không chạy SQL ghi/database update và không thay đổi database.

### 2026-11-18 — Prompt 02.3/02.3.1: áp dụng và xác minh InitialCreate

- Tiền kiểm đúng nhánh/HEAD, working tree sạch; SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS`, database `MediQueue` ONLINE, compatibility level 160.
- Lần áp dụng đầu dừng với SQL Server error 10611: cột `QueueTickets.Status` là `varchar(32)` nhưng filtered index dùng literal `N'Serving'`; transaction rollback, chỉ để lại `__EFMigrationsHistory` rỗng.
- Theo phê duyệt Prompt 02.3.1, sửa tối thiểu filter thành `[Status] = 'Serving'`, đồng bộ Fluent API, InitialCreate, Designer, ModelSnapshot và sinh lại SQL script bằng EF CLI.
- Bổ sung test metadata và SQL generation; bổ sung integration test read-only truy vấn 14 DbSet trên SQL Server thật, không ghi dữ liệu.
- Áp dụng thành công `20261117142359_InitialCreate`; Migration History có đúng một dòng, không còn pending Migration/model change.
- Xác minh 21 bảng mô hình + 1 bảng lịch sử, 22 PK, 27 FK, 36 index ngoài PK, 11 unique index, 4 filtered index, 12 check constraint, 1 default và 6 rowversion; không có FK/check bị disabled hoặc untrusted.
- `UX_QueueTickets_Doctor_Serving` unique/enabled, filter `([Status]='Serving')` trên `Status varchar(32)`; không thay đổi quy tắc một Doctor tối đa một ticket Serving.
- Tạo `docs/sql/VerifyDatabase.sql` read-only có guard database và `docs/database-validation.md`; không seed user/Role/dữ liệu mẫu.
- Restore/build thành công, `0` warning/`0` error; 34/34 test đạt (15 unit, 19 integration).
- Smoke test HTTP đạt: API `/health`, Web `/`, `/health`, `/Home/Error`, Notification `/health` đều 200; Queue Worker duy trì chạy. Bốn process đã dừng.
- Không cài package/công cụ, không dùng Docker, không commit/push/tạo Pull Request và không chuyển sang Prompt 02.4.

## 3. Mức độ đáp ứng yêu cầu đề tài

| Hạng mục | Trạng thái hiện tại |
|---|---|
| Doctor/phòng khám và slot còn chỗ | Có Domain/EF model, constraint/index; API chưa triển khai |
| Đặt/hủy Appointment | Có entity/invariant/mapping; transaction/service/API chưa triển khai |
| Cấp số qua RabbitMQ | Có DailyQueue/QueueTicket/Outbox/Inbox schema; publisher/consumer chưa triển khai |
| Worker sinh số/cập nhật queue | Có counter invariant và constraint; Worker nghiệp vụ chưa triển khai |
| Doctor cập nhật lượt phục vụ | Có state machine và filtered index; command API chưa triển khai |
| QueueChanged qua RabbitMQ | Có Outbox persistence; event/publisher chưa triển khai |
| Notification Service + SignalR | Mới ở mức thiết kế; chưa có consumer/Hub |
| Tra cứu Appointment/queue | Có model/index; query API và authorization chưa triển khai |

Chỉ nền tảng Domain/persistence/Migration đã triển khai và schema SQL Server đã được xác minh. Không khẳng định các luồng API nghiệp vụ, booking transaction, RabbitMQ hoặc SignalR đã hoạt động.

## 4. Quyết định thiết kế chính thức

1. QueueNumber riêng theo Doctor/ngày, bắt đầu 1, hiển thị `001...`, không tái sử dụng.
2. QueueTicket được Worker cấp qua RabbitMQ ngay sau đặt lịch, không đợi check-in.
3. V1 không có `Called`; gọi lượt chuyển trực tiếp `Waiting → Serving`.
4. Receptionist hỗ trợ lịch/check-in/theo dõi; chỉ Doctor phụ trách gọi, hoàn thành và NoShow.
5. Hủy trước `Slot.StartAt` nếu chưa phục vụ; NoShow là kết thúc và v1 không gọi lại.
6. Queue yêu cầu đăng nhập; không công khai dữ liệu Patient khác.
7. Một cơ sở y tế; mỗi Doctor thuộc đúng một Department (chuyên khoa).
8. API dùng Identity + JWT Access Token có thời hạn; MVC dùng Cookie và token store phía server; v1 không Refresh Token; SignalR kiểm tra quyền group.
9. SQL Server `LAPTOP-AQFGUM9Q\SQLEXPRESS`; duy nhất database `MediQueue` được dùng xuyên suốt 9 tuần, không tạo database theo tuần.
10. Giữ transactional outbox/inbox, idempotency, concurrency control; SQL là source of truth và event có `QueueVersion`.
11. Timestamp lưu UTC; mọi quyết định nghiệp vụ dùng `Asia/Ho_Chi_Minh`.

## 5. Quyết định Prompt 03.2 đã chốt

- Duy nhất database `MediQueue` trong 9 tuần; không database theo tuần.
- JWT HS256, issuer `MediQueue.Appointment.Api`, audience `MediQueue.Web`, 30 phút, key ngẫu nhiên tối thiểu 256 bit và không Refresh Token.
- Bootstrap Role/Admin tường minh; password Admin chỉ nhập ẩn từ terminal.
- Thu hồi bền vững bằng `SecurityStamp`; mỗi request đều kiểm tra active, lock và Role hiện tại.

Các tham số nghiệp vụ dài hạn như capacity mặc định, độ dài slot, retry/DLQ và retention vẫn được chốt ở prompt triển khai tương ứng.

## 6. Bước tiếp theo đề xuất

1. Sinh viên review toàn bộ `Tuan2`, đặc biệt [week02-plan.md](week02-plan.md), bằng chứng test và trạng thái Git.
2. Sinh viên tự stage/commit/push khi chấp nhận kết quả; commit đề xuất `feat(tuan2): implement MVC authentication and authorization`.
3. Mọi Migration/database change tiếp theo vẫn cần phê duyệt riêng.

Không tự động chuyển sang prompt tiếp theo.
