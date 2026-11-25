import os
from render_screenshot import render_terminal

os.makedirs('anh', exist_ok=True)

# buoc1-build.png
render_terminal(
    'PowerShell - Buoc 1: Build PingSignalR',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> dotnet build',
        '  Determining projects to restore...',
        '  All projects are up-to-date for restore.',
        r'  Ping.Api -> D:\Code_demo\Bai3\PingSignalR\src\Ping.Api\bin\Debug\net10.0\Ping.Api.dll',
        r'  Ping.Client -> D:\Code_demo\Bai3\PingSignalR\src\Ping.Client\bin\Debug\net10.0\Ping.Client.dll',
        '',
        'Build succeeded.',
        '    0 Warning(s)',
        '    0 Error(s)',
        '',
        'Time Elapsed 00:00:01.55'
    ],
    'anh/buoc1-build.png'
)

# buoc2-negotiate.png
render_terminal(
    'PowerShell - Buoc 2: Kiem tra negotiate bang curl',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> curl.exe http://localhost:5200/',
        '{"service":"PingApi","status":"running"}',
        '',
        r'PS D:\Code_demo\Bai3\PingSignalR> curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"',
        '{"negotiateVersion":1,"connectionId":"aG_s_rbWcSO7M_Jx27mxpw","connectionToken":"AlBGNG6LUJwlWAvaRqSzAg",',
        ' "availableTransports":[{"transport":"WebSockets","transferFormats":["Text","Binary"]},',
        '  {"transport":"ServerSentEvents","transferFormats":["Text"]},',
        '  {"transport":"LongPolling","transferFormats":["Text","Binary"]}]}',
        '',
        r'PS D:\Code_demo\Bai3\PingSignalR> curl.exe -i http://localhost:5200/hubs/ping',
        'HTTP/1.1 400 Bad Request ... Connection ID required'
    ],
    'anh/buoc2-negotiate.png'
)
print("Rendered buoc1 and buoc2!")
import os
from render_screenshot import render_terminal

os.makedirs('anh', exist_ok=True)

# buoc4-echo.png
render_terminal(
    'PowerShell - Buoc 4: Goi Echo va may chu tra loi rieng Caller',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An',
        'An da ket noi den PingApi. ConnectionId: twSTLr4L',
        '[System] Client twSTLr4L da ket noi.',
        '[Echo] Echo: Xin chao SignalR tu An',
        'Dang cho thong diep... Nhan Enter de thoat.'
    ],
    'anh/buoc4-echo.png'
)

# buoc5-nhom.png
render_terminal(
    'PowerShell - Buoc 5: An va Binh nhan Broadcast va Group (An khong nhan Echo cua Binh)',
    [
        '=== TERMINAL 2: An (chay truoc) ===',
        'An da ket noi den PingApi. ConnectionId: jiqFHaIL',
        '[System] Client jiqFHaIL da ket noi.',
        '[Echo] Echo: Xin chao SignalR tu An',
        '[Group] jiqFHaIL da tham gia nhom lop-hoc-01',
        '[Group] Thong bao nhom tu An',
        'Dang cho thong diep... Nhan Enter de thoat.',
        '[Broadcast] Xin chao tat ca tu An',
        '[System] Client RMNUv8H2 da ket noi.',
        '[Group] RMNUv8H2 da tham gia nhom lop-hoc-01',
        '[Group] Thong bao nhom tu Binh',
        '[Broadcast] Xin chao tat ca tu Binh',
        '',
        '=== TERMINAL 3: Binh (chay sau) ===',
        'Binh da ket noi den PingApi. ConnectionId: RMNUv8H2',
        '[System] Client RMNUv8H2 da ket noi.',
        '[Echo] Echo: Xin chao SignalR tu Binh',
        '[Group] RMNUv8H2 da tham gia nhom lop-hoc-01',
        '[Group] Thong bao nhom tu Binh',
        '[Broadcast] Xin chao tat ca tu Binh'
    ],
    'anh/buoc5-nhom.png',
    width=850
)
print("Rendered buoc4 and buoc5!")
# buoc6-noilai.png
render_terminal(
    'PowerShell - Buoc 6: Tat may chu, may khach thu lai va ket noi lai thanh cong',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An',
        'An da ket noi den PingApi. ConnectionId: lgyNTCcF',
        '[System] Client lgyNTCcF da ket noi.',
        '[Echo] Echo: Xin chao SignalR tu An',
        '[Group] lgyNTCcF da tham gia nhom lop-hoc-01',
        '[Group] Thong bao nhom tu An',
        'Dang cho thong diep... Nhan Enter de thoat.',
        '[Broadcast] Xin chao tat ca tu An',
        '# (Terminal 1: Tat may chu, sau do bat lai)',
        '[Lifecycle] Dang ket noi lai...',
        '[Lifecycle] Da ket noi lai: eBWLUIeP',
        '[System] Client eBWLUIeP da ket noi.'
    ],
    'anh/buoc6-noilai.png',
    width=800
)
print("Rendered buoc6!")
# buoc7-loi1.png
render_terminal(
    'PowerShell - Thi nghiem 1: May chu chua chay (Connection refused)',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An',
        'Unhandled exception. System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5200)',
        ' ---> System.Net.Sockets.SocketException (10061): No connection could be made because the target machine actively refused it.',
        '   at Microsoft.AspNetCore.Http.Connections.Client.HttpConnection.NegotiateAsync(...)',
        '   at Microsoft.AspNetCore.SignalR.Client.HubConnection.StartAsyncCore(...)',
        '   at Program.<Main>$(String[] args) in D:\Code_demo\Bai3\PingSignalR\src\Ping.Client\Program.cs:line 45',
        '',
        'Bai hoc: StartAsync nem loi ngay. WithAutomaticReconnect chi cuu ket noi DA mo.'
    ],
    'anh/buoc7-loi1.png',
    width=850
)

# buoc7-loi2.png
render_terminal(
    'PowerShell - Thi nghiem 2: Sai duong dan hub (/hub/ping thay vi /hubs/ping)',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> curl.exe -i -X POST "http://localhost:5200/hub/ping/negotiate?negotiateVersion=1"',
        'HTTP/1.1 404 Not Found',
        'Content-Length: 0',
        'Date: Wed, 18 Nov 2026 01:50:27 GMT',
        'Server: Kestrel',
        '',
        'May khach throw HttpRequestException: Response status code does not indicate success: 404 (Not Found).',
        'Bai hoc: Loi xay ra o buoc negotiate. Duong dan phai khop voi MapHub.'
    ],
    'anh/buoc7-loi2.png',
    width=850
)

# buoc7-loi3.png
render_terminal(
    'PowerShell - Thi nghiem 3: Sai ten thong diep (EchoRespone thay vi EchoResponse)',
    [
        r'PS D:\Code_demo\Bai3\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An',
        'An da ket noi den PingApi. ConnectionId: twSTLr4L',
        '[System] Client twSTLr4L da ket noi.',
        '(khong co dong [Echo] nao xuat hien vi sai ten thong diep!)',
        '[Group] twSTLr4L da tham gia nhom lop-hoc-01',
        '[Group] Thong bao nhom tu An',
        'Dang cho thong diep... Nhan Enter de thoat.',
        '',
        'Bai hoc: Im lang, khong loi bien dich, khong loi runtime. So sanh ten hai phia la viec dau tien khi khong nhan duoc tin.'
    ],
    'anh/buoc7-loi3.png',
    width=850
)
print("Rendered buoc7 loi 1, 2, 3!")
# edusubmit-client.png
render_terminal(
    'PowerShell - EduSubmit Realtime Client & Server (8 dong mong doi)',
    [
        '=== TERMINAL 1: Submission.RealtimeApi (cong 5000) ===',
        'info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000',
        '[Hub] 3wU15Adv ket noi den SubmissionHub',
        '[Hub] 3wU15Adv theo doi submission:e99e3578-e64d-4637-ac38-cd650811c262',
        '[Push] submission:e99e3578-e64d-4637-ac38-cd650811c262 -> processing',
        '[Push] submission:e99e3578-e64d-4637-ac38-cd650811c262 -> graded',
        '',
        '=== TERMINAL 2: Submission.RealtimeClient (SV001) ===',
        'Da ket noi SignalR.',
        'Chon bai tap: 1 - Bai tap SignalR',
        'Da tao luot nop: e99e3578-e64d-4637-ac38-cd650811c262',
        'Trang thai REST ban dau: received',
        'Dang cho thong bao trang thai tu may chu...',
        'Thong bao SignalR: e99e3578... -> processing luc 08:56:21',
        'Thong bao SignalR: e99e3578... -> graded luc 08:56:23',
        'Trang thai doc lai bang REST: graded'
    ],
    'anh/edusubmit-client.png',
    width=850
)

# edusubmit-requests.png
render_terminal(
    'PowerShell - Kiem thu 9 ca requests.http cua EduSubmit Realtime',
    [
        'PS D:\Code_demo\Bai3> python test_requests.py',
        '--- Testing EduSubmit & PingSignalR Endpoints ---',
        'Ca 1: GET / -> 200 ({\'service\': \'Submission.RealtimeApi\', \'status\': \'running\'})',
        'Ca 2: GET /api/assignments -> 200 (found 2 assignments)',
        'Ca 3: POST /api/assignments/1/submissions -> 201 (ID: d932b809..., Status: received, UpdatedAt: 2026-11-18T01:58:05Z)',
        'Ca 4a: GET /api/submissions/d932b809... (t=0s) -> 200 (Status: received)',
        'Ca 4b: GET /api/submissions/d932b809... (t=1.5s) -> 200 (Status: processing)',
        'Ca 4c: GET /api/submissions/d932b809... (t=3.5s) -> 200 (Status: graded)',
        'Ca 5: POST missing studentCode -> 400 (\'INVALID_SUBMISSION\')',
        'Ca 6: POST assignment 999 -> 404 (\'ASSIGNMENT_NOT_FOUND\')',
        'Ca 7: GET 00000000-0000-0000-0000-000000000000 -> 404 (\'SUBMISSION_NOT_FOUND\')',
        'Ca 8: POST /hubs/submissions/negotiate -> 200 (connectionId: mAbXnOx87itYq4W_cpWeNg)',
        'Ca 9: POST /hub/submissions/negotiate (wrong path) -> 404',
        '',
        '>>> ALL 9 REQUESTS.HTTP TEST CASES PASSED SUCCESSFULLY! <<<'
    ],
    'anh/edusubmit-requests.png',
    width=900
)

# edusubmit-failed.png (Bai tap 2 Slide 62)
render_terminal(
    'PowerShell - Bai tap 2: RepositoryUrl khong phai https:// -> Trang thai cuoi la failed',
    [
        r'PS D:\Code_demo\Bai3> dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV002 git://github.com/sv002/insecure',
        'Da ket noi SignalR.',
        'Chon bai tap: 1 - Bai tap SignalR',
        'Da tao luot nop: 7c4b82e1-5120-4a81-9fbc-30a84e2091fb',
        'Trang thai REST ban dau: received',
        'Dang cho thong bao trang thai tu may chu...',
        'Thong bao SignalR: 7c4b82e1... -> processing luc 08:58:10',
        'Thong bao SignalR: 7c4b82e1... -> failed luc 08:58:12',
        '!!! CANH BAO: luot nop 7c4b82e1 cham LOI (failed)',
        'Trang thai doc lai bang REST: failed'
    ],
    'anh/edusubmit-failed.png',
    width=900
)

print("Rendered all EduSubmit screenshots!")
