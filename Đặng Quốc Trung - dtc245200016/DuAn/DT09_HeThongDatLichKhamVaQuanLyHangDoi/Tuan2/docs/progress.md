# Nhật ký tiến độ MediQueue

## 1. Trạng thái tổng quan

| Thuộc tính | Giá trị |
|---|---|
| Nhánh | `duan-tuan2-dtc245200016` |
| Thư mục nộp | `DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan2` |
| Tuần | Tuần 2 — Quản lý bác sĩ và bệnh nhân, Prompt 03.7 |
| Nguồn kế thừa | Commit Tuần 1 `dcee864` — 177 tracked file |
| Pull Request | Chưa tạo; dự kiến `DuAn-Tuan2 - dtc245200016 - Đặng Quốc Trung` |
| Hạn nộp | Chưa được cung cấp |
| Trạng thái mã nguồn | Prompt 03.6 đã commit tại `9c515e7`; Prompt 03.7 đã hoàn thành, chờ sinh viên review/commit |
| Trạng thái database | Duy nhất `MediQueue`; đủ hai migration đã apply, Prompt 03.5.1 chỉ xác minh read-only và không ghi dữ liệu |
| Trạng thái Git | Baseline `Tuan2` sạch trước Prompt 03.5; không sửa tracked file ngoài `Tuan2` |

## 2. Nhật ký thực tế

### 2026-11-21 — Prompt 03.7: Doctor & Patient Management

| Milestone | Trạng thái | Kết quả hiện tại | Bước tiếp theo |
|---|---|---|---|
| A — Discovery & Data Model Review | COMPLETED | Baseline `9c515e7`; entity/schema snapshot đáp ứng, không cần migration | Doctor backend |
| B — Doctor Backend | COMPLETED | Contract/service/API Admin + self, transaction, audit, idempotent unique link và rowversion | Hoàn tất |
| C — Patient Backend | COMPLETED | Admin list/detail/update + self, registration giữ nguyên, ngày Việt Nam và rowversion | Hoàn tất |
| D — MVC Integration | COMPLETED | Typed client, MVC controllers, session JWT server-side, antiforgery và typed ViewModel | Hoàn tất |
| E — Doctor Management UI | COMPLETED | Index/Create/Edit/Details/Self, filter/paging/responsive/empty state | Hoàn tất |
| F — Patient Management UI | COMPLETED | Index/Edit/Details; dùng lại Account/Profile cho Patient self | Hoàn tất |
| G — Dashboard, Profile & Security | COMPLETED | Sidebar/breadcrumb/quick action theo role; API authorization và token-leak tests | Hoàn tất |
| H — SQL, Regression & Documentation | COMPLETED | Build 0/0; 144 test pass, 4 SQL opt-in skip; tài liệu đồng bộ | Sinh viên review thủ công |

**Checkpoint Milestone A:**

- Git đúng nhánh `duan-tuan2-dtc245200016`; HEAD/local/origin `9c515e7` (`feat(tuan2): implement department and room management`); không có tracked/staged change. Untracked cũ ngoài Tuan2 `Bai3/`, `Tuan1/` được giữ nguyên.
- Baseline chạy lại: restore up-to-date, build 0 warning/0 error; Unit 30 pass, Integration 98 pass/3 SQL opt-in skip.
- `Doctor`: `Id`, unique `UserId`, `DepartmentId` FK restrict, `DisplayName nvarchar(200)`, `IsActive`, `rowversion`; quan hệ 1–1 với `ApplicationUser`, n–1 với Department.
- `Patient`: `Id`, unique `UserId`, `FullName nvarchar(200)`, `DateOfBirth date NULL`, `PhoneNumber varchar(32) NULL`, `IsActive`, `rowversion`; quan hệ 1–1 với `ApplicationUser` restrict.
- Registration hiện tạo Identity Patient + Patient profile trong cùng transaction; account profile ưu tiên `Patient.FullName`, `Doctor.DisplayName`, `StaffFullName`, rồi email alias.
- Chốt `Migration: NOT REQUIRED`: unique UserId, FK, rowversion và các cột cần thiết đã có trong migration snapshot. Không thêm Doctor–Room vì phòng được phân qua schedule/time slot ở giai đoạn sau.
- SQL metadata read-only lần này **NOT VERIFIED** do ODBC/SqlClient không thiết lập được kênh mã hóa (`Encryption not supported on the client`); không thay đổi TLS/SQL certificate và không có lệnh ghi dữ liệu.

**Checkpoint Milestone B–C:**

- Thêm contract, query whitelist, DTO không chứa trường Identity nhạy cảm, `DoctorManagementService`, `PatientManagementService` và hai API controller theo nghiệp vụ.
- Doctor create chỉ nhận tài khoản role Doctor active chưa liên kết và Department active; transaction `Serializable`, unique index và audit bảo vệ liên kết một-một. Update Doctor/Patient dùng rowversion và trả `409` khi xung đột.
- Patient registration cũ không thay đổi. Admin chỉ sửa dữ liệu hành chính tối thiểu; ngày sinh tương lai bị từ chối theo múi giờ Việt Nam. `/me` suy ra owner từ claim `sub`.

**Checkpoint Milestone D–G:**

- Thêm `IProfilesApiClient`, MVC controller Admin/Doctor, ViewModel typed và 8 View Doctor/Patient. JWT tiếp tục ở session store phía server; command MVC có antiforgery và không tự retry.
- Sidebar, breadcrumb và quick action được nối theo role. Admin quản lý Doctor/Patient; Doctor chỉ mở hồ sơ chuyên môn của mình; Receptionist không có quyền; Patient tiếp tục dùng trang hồ sơ hiện hành.
- UI dùng design token sẵn có, bảng responsive, focus, empty state và reduced motion; không thêm package/thư viện.
- Thêm 4 unit test và 12 integration test cho invariant, contract, typed client, API role/self/no-delete/no-sensitive-data và MVC role/token leak/static asset.

**Checkpoint Milestone H — final gate:**

- Restore up-to-date; build `MediQueue.slnx` đạt 0 warning/0 error.
- Unit 34/34 pass; Integration 110 pass, 4 SQL opt-in skip; tổng cộng 144 pass, 0 fail, 4 skip.
- `dotnet format --verify-no-changes`, `git diff --check`, NuGet vulnerability audit và scan secret đạt; 306 tệp source/docs UTF-8 hợp lệ, 0 `bin/obj` được track, 0 file staged.
- Không tạo/apply Migration, không seed/chạy command ghi trên `MediQueue`, không thêm package, không Docker, không stage/commit/push/PR.
- SQL metadata/service query thật vẫn **NOT VERIFIED** do lỗi mã hóa client đã ghi ở Milestone A. TestServer đã smoke render MVC/API và authorization; nghiệm thu browser HTTPS/visual với tài khoản thật còn dành cho sinh viên vì không sử dụng credential/database chính thức.
- Tài liệu chi tiết: `docs/doctor-patient-management.md`. Không tự chuyển Prompt 03.8.

### 2026-11-21 — Prompt 03.6: Department & Room Management

| Milestone | Trạng thái | Kết quả hiện tại | Bước tiếp theo |
|---|---|---|---|
| A — Entity, schema, quan hệ, nghiệp vụ | COMPLETED | Baseline `e831bd5` sạch trong Tuan2; SQL read-only xác nhận schema hiện có đủ, không cần migration | Department backend |
| B — Department API/service/DTO/test | COMPLETED | Admin-only list/detail/create/update/activate/deactivate; audit và dependency guard | Hoàn tất |
| C — Room API/service/DTO/test | COMPLETED | Admin-only list/detail/create/update/activate/deactivate; audit và dependency guard | Hoàn tất |
| D — MVC controller/client/ViewModel | COMPLETED | Typed client, session JWT server-side, MVC authorization và antiforgery | Hoàn tất |
| E — Department UI | COMPLETED | Index/Create/Edit/Details, filter, pagination, empty/error/confirm state | Hoàn tất |
| F — Room UI | COMPLETED | Index/Create/Edit/Details, filter, pagination, empty/error/confirm state | Hoàn tất |
| G — Authorization/integration/responsive/security | COMPLETED | Unit/API/MVC coverage; responsive, focus, reduced motion; không lộ token | Hoàn tất |
| H — SQL/regression/docs/final review | COMPLETED | Restore/build/format đạt; 128 test pass, 3 opt-in SQL skip; tài liệu và diff đã review | Sinh viên review thủ công |

**Checkpoint Milestone A:**

- Git đúng nhánh `duan-tuan2-dtc245200016`; HEAD/local/origin `e831bd5`; không có thay đổi trong Tuan2 trước Prompt 03.6. Untracked cũ `Bai3/` và `Tuan1/` được giữ nguyên.
- `Department`: `Id`, `Code varchar(32)` unique, `Name nvarchar(200)`, `IsActive`; quan hệ 1-n với Doctor, delete restrict.
- `Room`: `Id`, `Code varchar(32)` unique, `Name nvarchar(200)`, `Location nvarchar(300) NULL`, `IsActive`; quan hệ 1-n với DoctorSchedule và TimeSlot, delete restrict. Không có quan hệ Department–Room.
- Database `MediQueue` ONLINE, đủ hai migration đã apply; Department/Room/Doctor/DoctorSchedule/TimeSlot hiện đều 0 bản ghi. Chỉ truy vấn metadata/count read-only, không ghi dữ liệu.
- Chốt không tạo migration: schema đã có Code/Name/Location/IsActive, unique Code và các FK cần thiết. Deactivate Department sẽ chặn khi có Doctor active; deactivate Room sẽ chặn khi có DoctorSchedule hoặc TimeSlot active; không cascade/chuyển dữ liệu liên quan.
- File đã sửa: `docs/progress.md`. Kiểm thử code chưa chạy ở Milestone A; bước tiếp theo là Department Contracts/Service/API và test.

**Checkpoint Milestone B–C:**

- Bổ sung contract có DataAnnotations, whitelist sort/page size, DTO list/detail/summary cho Department và Room.
- Service dùng `AsNoTracking` cho query; command dùng transaction `Serializable`, kiểm tra duplicate/dependency và ghi AuditLog cùng transaction.
- API Controller yêu cầu role Admin, trả ProblemDetails, không cache và không cung cấp DELETE.

**Checkpoint Milestone D–F:**

- `ICatalogApiClient` giữ JWT hoàn toàn phía server; MVC Controller Admin-only, command POST + antiforgery, không retry command.
- Hai module có đủ Index/Create/Edit/Details, sidebar/breadcrumb/quick action, empty/error/confirm/loading state và nội dung tiếng Việt.
- CSS dùng design token hiện hành, responsive table-to-card, focus-visible và reduced-motion; không thêm package hoặc thư viện frontend.

**Checkpoint Milestone G:**

- Thêm unit test entity/contract, API integration test xác thực/phân quyền/validation/command và MVC flow test cho Admin/Patient/anonymous/static asset/token leak.
- Phát hiện và sửa DataAnnotations target trên positional record gây HTTP 500 khi model validation; regression API catalog sau sửa đạt 4/4.

**Checkpoint Milestone H — final gate:**

- `dotnet restore`: tất cả project up-to-date; `dotnet build --no-restore`: 0 warning, 0 error.
- `dotnet test --no-build`: Unit 30/30 pass; Integration 98 pass, 3 test SQL opt-in skip; tổng cộng 128 pass, 0 fail, 3 skip.
- `dotnet format --verify-no-changes`: pass. `git diff --check`: không có whitespace error (chỉ cảnh báo Git chuyển LF/CRLF cho bốn Razor file đã theo cấu hình repository).
- Smoke/authorization/UI render được bao phủ bằng TestServer; Admin mở được cả hai danh mục và static CSS, Patient bị từ chối, anonymous bị chuyển đăng nhập, JWT không xuất hiện trong HTML.
- Test catalog opt-in với SQL Server thật đã được gọi riêng ở chế độ chỉ đọc nhưng SqlClient của môi trường sandbox không thiết lập được kênh mã hóa (`server requires encryption but this machine does not support it`); không phải lỗi dịch LINQ và không có câu lệnh ghi nào được gửi. Khảo sát schema/count bằng `sqlcmd` ở Milestone A đã thành công.
- Thử khởi động Kestrel HTTPS thật không thành công do máy hiện không có development certificate hợp lệ và sandbox không đọc được DPAPI key ring/Event Log. Không tự tạo/trust certificate hoặc sửa cấu hình bảo mật; cần nghiệm thu HTTPS/visual bằng môi trường người dùng đã cấu hình certificate.
- Không tạo/apply migration, không chạy command test trên `MediQueue`, không thêm package, không có DELETE, không stage/commit/push/PR.

### 2026-11-19 — Prompt 03.5.1: sửa lỗi runtime User Management

| Hạng mục | Trạng thái | Kết quả hiện tại | Bước tiếp theo |
|---|---|---|---|
| Git/database precheck | COMPLETED | Đúng nhánh `duan-tuan2-dtc245200016`, HEAD `4536737`; database ONLINE, đủ 2 migration, `StaffFullName nvarchar(150) NULL`, 2 user/4 Role | Hoàn tất |
| Điều tra lỗi gốc | COMPLETED | EF Core không dịch được `OrderBy` sau projection vào positional record `UserRow`; tái hiện bằng query SQL Server thật | Hoàn tất |
| Điều tra Error View | COMPLETED | `UsersController` gọi `View("Error")` nhưng không có view phù hợp; lỗi thứ cấp che lỗi API | Hoàn tất |
| Runtime/error handling fix | COMPLETED | Projection member-init; typed client phân biệt 502/503; shared error state giữ đúng HTTP status và không lộ chi tiết nội bộ | Hoàn tất |
| Regression/final gate | COMPLETED | Full test 113 pass/0 fail/2 SQL skip; opt-in SQL read-only 2 pass; restore/build/format/diff/smoke đạt | Sinh viên nghiệm thu HTTPS/Admin thật |

- Lỗi gốc đã được tái hiện bằng integration test opt-in trên đúng `MediQueue`, không ghi dữ liệu: LINQ expression `OrderByDescending(new UserRow(...).CreatedAtUtc)` không thể translate.
- Lỗi `The view 'Error' was not found` là lỗi thứ cấp trong MVC và che mất ProblemDetails/lỗi API ban đầu.
- SQL read-only xác nhận `20261117142359_InitialCreate` và `20261119135325_AddStaffFullNameToApplicationUser` đã apply; cột mới nullable, max length 150 ký tự; 2 account, 4 Role, 2 user-role; list/aggregate SQL thuần hoạt động.
- `UserManagementService` đã chuyển cả list/details projection sang member-init class. Test opt-in chạy thật list/search/filter/sort/pagination/aggregate/details và fallback tên khi `StaffFullName` null; không ghi database.
- `UsersController` dùng `Views/Shared/UserManagementError.cshtml` với model riêng; 401 kết thúc session, 403 chuyển access denied, 400/404/409/500/502/503 giữ đúng ngữ nghĩa. Lỗi 5xx không hiển thị detail nội bộ.
- Bổ sung regression test cho Admin GET `/Users`, Admin-only, success, API 500, 400/404/409, unavailable, timeout, response không hợp lệ, Error View, token leak và SQL Server thật. Baseline cũ không phát hiện lỗi vì API test thay `IUserManagementService`, Web test thay `IUsersApiClient`, nên không thực thi LINQ provider SQL Server.
- Restore up-to-date; build 0 warning/0 error; unit 26 pass; integration mặc định 87 pass/2 skip; SQL opt-in 2/2 pass; format verification và `git diff --check` đạt.
- Kiểm tra strict UTF-8 đạt 252/252 tệp văn bản; kiểm tra liên kết nội bộ Markdown không phát hiện liên kết hỏng.
- HTTP smoke cô lập đạt: API health 200, `/api/users` anonymous 401, Web health 200, `/Users` anonymous 302, CSS/JS 200. Process do lượt kiểm tra tạo đã dừng.
- Rà soát giữ nguyên antiforgery trên mọi command, cookie Secure/HttpOnly/SameSite và Data Protection. Development dùng ephemeral provider nên cookie/form token cũ sau khi restart không còn giải mã được; production persist key và bảo vệ DPAPI. Không tắt CSRF, không xóa key và không bypass TLS.
- HTTPS và luồng Admin thật trên trình duyệt **NOT VERIFIED**: máy hiện không có development certificate hợp lệ và không yêu cầu mật khẩu Admin. Các command ghi Create/Edit/Activate/Deactivate/Unlock trên database thật cũng **NOT VERIFIED** theo chủ đích an toàn.

### 2026-11-19 — Prompt 03.5: Quản lý người dùng và tài khoản nhân sự

| Milestone | Trạng thái | Kết quả hiện tại | Bước tiếp theo |
|---|---|---|---|
| A — Phân tích dữ liệu và thiết kế | COMPLETED | Xác nhận baseline `4536737`; khảo sát SQL read-only; chốt `StaffFullName` nullable và nguồn tên hiển thị theo profile ưu tiên | Sinh migration cộng dồn review-only |
| B — Schema và migration | COMPLETED | Model/configuration, migration cộng dồn, snapshot và SQL idempotent đã review; không apply | Sinh viên review/apply khi sẵn sàng |
| C — Backend service và API | COMPLETED | DTO, 7 endpoint Admin-only, query phân trang, command, audit, revocation và concurrency | Automated/API gate |
| D — MVC client và controller | COMPLETED | Typed client, controller Admin-only, POST antiforgery và error mapping | UI và regression test |
| E — Premium UI/UX | COMPLETED | Danh sách/form/chi tiết/modal/badge/responsive/reduced motion | Visual QA thủ công |
| F — Dashboard/sidebar/profile | COMPLETED | Sidebar/quick action Admin và nguồn tên staff/profile đồng bộ | Regression test |
| G — Automated test | COMPLETED | 26 unit + 76 integration pass; 1 database test skip | Final gate |
| H — Tài liệu và final gate | COMPLETED | Restore/build/test/format/audit/smoke/UTF-8/link/Git review đạt; giới hạn được ghi rõ | Sinh viên review, chưa commit/apply migration |

**Milestone A — COMPLETED:**

- Nhánh đúng `duan-tuan2-dtc245200016`; HEAD/local/origin cùng ở commit `4536737` (`feat(tuan2): implement premium layout and role-based dashboards`). `Tuan2` sạch, không staged; untracked cũ ngoài phạm vi tại `Bai3` và `Tuan1` được giữ nguyên.
- SQL Server được truy vấn read-only sau khi xác nhận đúng `LAPTOP-AQFGUM9Q\\SQLEXPRESS` / `MediQueue`: database ONLINE, chỉ có migration `20261117142359_InitialCreate`; 2 tài khoản gồm 1 Admin và 1 Patient, Doctor/Receptionist bằng 0; Patient profile bằng 1, Doctor/Department/Room bằng 0.
- `AspNetUsers` hiện chưa có trường tên nhân sự. Chốt thêm `StaffFullName` nullable, tối đa 150 ký tự; migration chỉ được sinh để review, không apply và không sửa `InitialCreate`.
- Nguồn tên hiển thị: Patient dùng `Patient.FullName`; Doctor có hồ sơ dùng `Doctor.DisplayName`; Doctor chưa có hồ sơ, Receptionist và Admin dùng `ApplicationUser.StaffFullName`; email alias chỉ là fallback trình bày và không được lưu như tên.
- Module chỉ cho Admin truy cập; tạo mới giới hạn Doctor/Receptionist; không cho xóa, đổi Role/email hoặc tạo Patient/Admin; phân biệt `IsActive` với Identity lockout.
- Không có SQL ghi, seed, reset Admin, database mới, package mới hoặc thay đổi ngoài `Tuan2`.

**Milestone B–F — COMPLETED:**

- Thêm `ApplicationUser.StaffFullName` nullable 150 ký tự; migration `20261119135325_AddStaffFullNameToApplicationUser`, designer, snapshot và SQL idempotent đã sinh. `InitialCreate` không bị sửa; EF xác nhận model không còn pending change.
- Khảo sát read-only sau khi sinh migration xác nhận database thật vẫn chưa có cột mới, Migration history vẫn chỉ có `InitialCreate` và UserCount vẫn bằng 2.
- API `UsersController` có đủ list/detail/create/update/deactivate/activate/unlock, tất cả Admin-only, `ProblemDetails`, no-store và không có DELETE. Service dùng projection, aggregate, stable sort, phân trang, transaction, Identity concurrency, `AuditLogs` và xoay `SecurityStamp`.
- Bảo vệ Admin/self, chỉ tạo Doctor/Receptionist, không đổi email/Role/mật khẩu và không tạo profile Doctor giả. `IsActive` tách biệt lockout; command lặp hợp lệ là idempotent.
- MVC dùng `UsersApiClient` và session JWT phía server; controller Admin-only, command POST + antiforgery. UI gồm stats, filter, table/card responsive, create/edit/details, badge, empty state, modal xác nhận và reduced motion.
- Sidebar/quick action chỉ hiện cho Admin. Hồ sơ Admin/Receptionist và Doctor chưa có profile dùng `StaffFullName`; Patient/Doctor có profile vẫn ưu tiên nguồn nghiệp vụ.

**Milestone G–H — COMPLETED:**

- Restore đạt; full solution build `0` warning, `0` error; `dotnet format --verify-no-changes` đạt.
- Test đạt 26 unit + 76 integration = 102 pass, 1 database read test skip opt-in, 0 fail. Test dùng service/client double, không ghi `MediQueue`.
- NuGet advisory online: 9/9 project không có package vulnerable theo nguồn hiện tại. Không cài package mới.
- HTTP smoke cô lập: API/Web health 200; `/api/users` anonymous 401; `/Users` anonymous 302 về shared login; CSS/JS module 200. Process do lượt kiểm tra tạo đã dừng.
- UTF-8 strict kiểm tra 257 file: 0 lỗi; 20 Markdown: 0 liên kết nội bộ hỏng; `git diff --check` không có whitespace error. Browser storage mới không được thêm; `localStorage` kế thừa chỉ lưu trạng thái sidebar.
- HTTPS **NOT VERIFIED** vì `dotnet dev-certs https --check --trust` báo không có certificate hợp lệ. Visual QA browser **NOT VERIFIED** vì native computer-use pipe không khả dụng; responsive/accessibility đã được code review và integration render test.
- SQL end-to-end **NOT VERIFIED** theo chủ đích vì migration chưa apply. Hướng dẫn review/apply/xác minh chính xác nằm tại `docs/user-management.md`; không tự chuyển Prompt 03.6.

### 2026-11-19 — Prompt 03.4: Premium Shared Layout và Role-based Dashboard

| Milestone | Trạng thái | Kết quả hiện tại | Bước tiếp theo |
|---|---|---|---|
| A — Dữ liệu và Dashboard API | COMPLETED | DTO theo Role, `DashboardService` EF Core read-only, `GET /api/dashboard/me`, `no-store`; build API cô lập đạt 0 warning/0 error | Tích hợp typed client và MVC |
| B — Shared Layout, Sidebar, Header | COMPLETED | Workspace dùng chung, sidebar responsive, header, breadcrumb, account dropdown; Web build 0 warning/0 error | Kiểm thử tương tác trình duyệt |
| C — Admin Dashboard | COMPLETED | Hiển thị aggregate thật, active/inactive, phân bố bốn Role và quick actions hợp lệ | Bổ sung automated test |
| D — Doctor Dashboard | COMPLETED | Chỉ hiện hồ sơ bác sĩ hiện tại; có empty state khi chưa liên kết | Bổ sung automated test |
| E — Receptionist Dashboard | COMPLETED | Chỉ số danh mục đúng phạm vi lễ tân, không có dữ liệu giả | Bổ sung automated test |
| F — Patient Dashboard | COMPLETED | Chỉ hiện hồ sơ bệnh nhân hiện tại; có empty state khi chưa liên kết | Bổ sung automated test |
| G — Responsive, UI/UX, Security | COMPLETED | Collapse/drawer/focus trap/Escape/reduced motion, role accent, no-store và ownership đã review/test | Visual QA trình duyệt tại Milestone H |
| H — Test, tài liệu, final review | COMPLETED | Build 0/0; 23 unit + 71 integration pass, 1 database test skip; format/audit/docs/Git review đạt | Sinh viên review; không tự chuyển Prompt 03.5 |

**Milestone A — COMPLETED:**

- Baseline sạch trên commit `c9dacb6` (`feat(tuan2): implement MVC authentication and polish UI/UX`).
- SQL Server được khảo sát read-only: `MediQueue` ONLINE; 2/2 tài khoản active, gồm 1 Admin và 1 Patient; 1 Patient profile; Doctor/Department/Room đều 0. Không xuất PII và không ghi dữ liệu.
- Tạo hợp đồng Dashboard trong Contracts; response chỉ điền payload của Role hiện tại, không trả password hash, security stamp, JWT hoặc entity persistence.
- API lấy `sub` và `role` từ JWT đã xác minh, không nhận UserId/Role từ query; Role không hợp lệ bị từ chối.
- `DashboardService` dùng `AsNoTracking`, projection, `CountAsync` qua aggregate và truy vấn tuần tự trên một scoped DbContext; không N+1, không `Task.WhenAll`, không ghi database.
- Admin có total/active/inactive và phân bố bốn Role; Doctor/Patient chỉ đọc profile gắn với `UserId`; Receptionist chỉ nhận aggregate danh mục cần thiết.
- File đang xử lý tiếp theo: `DashboardApiClient`, MVC `DashboardController`, `_DashboardLayout` và shared partials.
- Không có lỗi tồn tại ở Milestone A. Build API cô lập đạt `0` warning, `0` error.

**Milestone B — COMPLETED:**

- Tạo `_DashboardLayout`, sidebar, header, breadcrumb và các partial statistic/quick action/state dùng chung; không tạo liên kết tới controller hoặc chức năng chưa tồn tại.
- Sidebar desktop hỗ trợ expanded/collapsed; chỉ lưu khóa giao diện `mediqueue.ui.sidebar-collapsed`, tuyệt đối không lưu JWT hoặc dữ liệu xác thực trong browser storage.
- Mobile drawer có overlay, đóng bằng Escape, focus trap và trả focus về nút mở; header/account dropdown có role label tiếng Việt, logout POST kèm antiforgery.
- `Profile` và `ChangePassword` chuyển sang workspace mới nhưng giữ nguyên controller, ViewModel, validation và luồng xác thực hiện có.
- Web build cô lập sau Milestone B đạt `0` warning, `0` error.

**Milestone C–F — COMPLETED:**

- Admin Dashboard dùng toàn bộ aggregate từ API: tài khoản, bác sĩ, bệnh nhân, chuyên khoa, phòng khám và phân bố Role; không có magic number hoặc biểu đồ giả.
- Doctor/Patient Dashboard chỉ hiển thị profile gắn với tài khoản hiện tại và có empty state rõ ràng nếu profile chưa tồn tại.
- Receptionist Dashboard chỉ hiển thị các aggregate danh mục phù hợp quyền hạn; không truy cập dữ liệu cá nhân ngoài phạm vi.
- Cả bốn Role dùng welcome banner, thông tin tài khoản và quick actions chung; module lịch hẹn/hàng đợi chưa triển khai được mô tả trung thực, không tạo menu giả.
- File đang xử lý tiếp theo: automated test Dashboard API/MVC/client, security/responsive review và tài liệu.

**Milestone G — COMPLETED:**

- Layout đáp ứng desktop/tablet/mobile; sidebar desktop có hai trạng thái, mobile drawer có overlay/Escape/focus trap/body lock và không gây cuộn ngang theo thiết kế CSS.
- Toàn bộ animation dùng motion token và được rút về gần 0 khi `prefers-reduced-motion: reduce`; control có focus-visible và kích thước tương tác phù hợp.
- API `[Authorize]`, chỉ chấp nhận bốn Role, lấy `sub`/`role` từ principal đã xác minh, giới hạn Doctor/Patient theo ownership và trả `no-store`; MVC yêu cầu đúng bốn Role và đối chiếu Role response.
- JWT chỉ nằm trong token store phía server và Bearer header theo từng request. Browser storage chỉ dùng khóa UI `mediqueue.ui.sidebar-collapsed`; không có token trong View/JavaScript/URL.
- Không tạo menu/link/controller placeholder; các module chưa triển khai chỉ có thông báo trung thực, không dùng số liệu giả.

**Milestone H — CHECKPOINT TRƯỚC FINAL GATE:**

- Bổ sung unit test Role presentation: `23/23` pass.
- Bổ sung API integration test bằng authentication/service test double cô lập và MVC/typed client integration test: `66` pass, `1` database read test skip opt-in, `0` fail.
- Regression Account flow tiếp tục pass; Dashboard test bao phủ bốn Role, 401/403/unavailable, chống query ghi đè identity, no-store và không lộ trường nhạy cảm/JWT.
- Đã tạo `docs/dashboard.md` và đồng bộ README, AGENTS, architecture, API, authentication, actors, business rules, use cases, UI design system, setup, roadmap và week plan.
- Lần chạy integration mặc định đầu tiên bị file lock bởi process API có sẵn của người dùng; không dừng process đó. Chạy lại trong artifacts tạm cô lập đạt đầy đủ kết quả trên và đã xóa artifacts tạm.
- Bước tiếp theo: restore/build/full test/format/NuGet audit, HTTPS/smoke/visual QA, link/UTF-8/Git diff review.

**Milestone H — COMPLETED (final gate):**

- `dotnet restore MediQueue.slnx` thành công; full solution build trong artifacts cô lập đạt `0` warning, `0` error. Việc cô lập tránh can thiệp process API có sẵn đang khóa output mặc định.
- Unit test `23/23` pass; integration test `71` pass, `1` database read test skip opt-in, `0` fail. Tổng tự động: `94` pass, `1` skip, `0` fail.
- `dotnet format --verify-no-changes --no-restore` đạt; UTF-8 strict `0` lỗi; liên kết Markdown nội bộ `0` hỏng; `git diff --check` không phát hiện whitespace error.
- NuGet advisory online xác nhận cả 9 project không có package vulnerable theo nguồn hiện tại; không cài package/công cụ mới.
- HTTP smoke cô lập: API/Web health và Home `200`, Dashboard API anonymous `401`, Dashboard/Profile/ChangePassword anonymous `302` về Login; Dashboard CSS/JS/SVG/font `200`. Login/Register không chạy qua HTTP vì antiforgery cookie bắt buộc Secure — đúng thiết kế bảo mật.
- HTTPS **NOT VERIFIED** trong môi trường Codex: `dotnet dev-certs https --check --trust` báo `No valid certificate found`; không tạo/xóa/trust certificate và không bypass TLS. Cần kiểm tra lại `https://localhost:7101/7102` trong phiên Windows của sinh viên.
- Visual QA browser **NOT VERIFIED**: native computer-use pipe không khả dụng. Automated render test xác minh bốn View/layout/menu/assets; code review xác minh breakpoint, focus, Escape và reduced motion. Checklist thủ công có tại `docs/dashboard.md`.
- Không có Migration/schema/package change; database chỉ được truy vấn read-only. Không stage/commit/push/PR và không chuyển Prompt 03.5.

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
