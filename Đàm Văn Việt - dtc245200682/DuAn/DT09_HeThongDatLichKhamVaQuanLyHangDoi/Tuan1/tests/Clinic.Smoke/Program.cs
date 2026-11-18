using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clinic.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Npgsql;

// Real integration tests. Runs against the Compose stack; no in-memory substitutes.
var root=Directory.GetCurrentDirectory();
if(!File.Exists(Path.Combine(root,"docker-compose.yml"))) throw new InvalidOperationException("Run from DuAn/.");
var output=Path.Combine(root,"TestResults");Directory.CreateDirectory(output);
var http=new HttpClient { BaseAddress=new Uri(Environment.GetEnvironmentVariable("TEST_API_URL") ?? "http://localhost:5080"),Timeout=TimeSpan.FromSeconds(15) };
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
    ["RabbitMQ:Uri"]=Environment.GetEnvironmentVariable("TEST_RABBITMQ_URI") ?? "amqp://clinic:clinic-demo-password@localhost:5672/"
}).Build();
await using var source=NpgsqlDataSource.Create(Environment.GetEnvironmentVariable("TEST_DATABASE") ?? "Host=localhost;Database=clinic;Username=clinic;Password=clinic-demo-password");
var db=new Database(source);
var doctors=new List<int>();
var results=new List<object>();
var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);
var runId=Guid.NewGuid().ToString("N")[..8];
var baseDoctor=Random.Shared.Next(10000,100000000);
var failed=false;
var staffPassword=Environment.GetEnvironmentVariable("TEST_STAFF_PASSWORD") ?? "demo-staff-2026";
string? staffToken=null;

void Check(bool condition,string detail) {if(!condition) throw new Exception(detail);}
async Task Until(Func<Task<bool>> condition,string detail,int seconds=25)
{
    var deadline=DateTime.UtcNow.AddSeconds(seconds);
    while(DateTime.UtcNow<deadline) {if(await condition()) return;await Task.Delay(150);}
    throw new Exception("Timeout: "+detail);
}
async Task Compose(params string[] args)
{
    var start=new ProcessStartInfo("docker") { WorkingDirectory=root,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true };
    start.ArgumentList.Add("compose");foreach(var arg in args)start.ArgumentList.Add(arg);
    using var p=Process.Start(start)!;
    var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();var text=await stdout+await stderr;
    if(p.ExitCode!=0)throw new Exception("Compose: "+text);
    Console.WriteLine("compose "+string.Join(' ',args)+": OK");
}
async Task Run(string name,Func<Task<string>> test)
{
    var timer=Stopwatch.StartNew();
    try {var evidence=await test();results.Add(new{name,status="PASS",evidence,seconds=timer.Elapsed.TotalSeconds});Console.WriteLine($"PASS {name}: {evidence}");}
    catch(Exception ex) {failed=true;results.Add(new{name,status="FAIL",evidence=ex.ToString(),seconds=timer.Elapsed.TotalSeconds});Console.WriteLine($"FAIL {name}: {ex}");}
}
async Task<int> Doctor()
{
    var id=baseDoctor+doctors.Count;doctors.Add(id);
    await using var c=await db.Open();
    await Database.Execute(c,"INSERT INTO doctors VALUES($1,$2,'Kiểm thử','TEST')",id,$"TEST {runId} {doctors.Count}");return id;
}
async Task<Guid> Slot(int doctor,int capacity=5,int minute=0,bool future=false)
{
    var id=Guid.NewGuid();var date=ClinicTime.Today;
    var start=ClinicTime.At(date,TimeOnly.MinValue).AddMinutes(minute);
    if(future)start=DateTimeOffset.UtcNow.AddHours(1);
    await using var c=await db.Open();
    await Database.Execute(c,"INSERT INTO slots(id,doctor_id,visit_date,starts_at,ends_at,capacity) VALUES($1,$2,$3,$4,$5,$6)",id,doctor,date,start.ToUniversalTime(),ClinicTime.At(date.AddDays(1),TimeOnly.MinValue).ToUniversalTime(),capacity);return id;
}
async Task<Booking> Book(Guid slot,string name="Bệnh nhân kiểm thử")
{
    using var response=await http.PostAsJsonAsync("/appointments",new BookRequest(slot,name,"0901234567"));
    var body=await response.Content.ReadAsStringAsync();Check(response.StatusCode==HttpStatusCode.Created,"Booking failed: "+body);
    return JsonSerializer.Deserialize<Booking>(body,json)!;
}
async Task<Appointment> Get(Booking b)
{
    using var request=new HttpRequestMessage(HttpMethod.Get,$"/appointments/{b.Id}");request.Headers.Add("X-Appointment-Token",b.AccessToken);
    using var response=await http.SendAsync(request);response.EnsureSuccessStatusCode();return (await response.Content.ReadFromJsonAsync<Appointment>())!;
}
async Task<HttpResponseMessage> RetryBook(BookRequest body,string key)
{
    using var request=new HttpRequestMessage(HttpMethod.Post,"/appointments") { Content=JsonContent.Create(body) };
    request.Headers.Add("Idempotency-Key",key);
    return await http.SendAsync(request);
}
async Task Cancel(Booking b)
{
    using var request=new HttpRequestMessage(HttpMethod.Delete,$"/appointments/{b.Id}");request.Headers.Add("X-Appointment-Token",b.AccessToken);
    using var response=await http.SendAsync(request);response.EnsureSuccessStatusCode();
}
async Task<QueueView> Advance(int doctor,string action)
{
    using var request=new HttpRequestMessage(HttpMethod.Post,$"/internal/queues/{doctor}/{action}");request.Headers.Authorization=new("Bearer",staffToken);
    using var response=await http.SendAsync(request);
    Check(response.IsSuccessStatusCode,"Advance failed: "+await response.Content.ReadAsStringAsync());
    return (await response.Content.ReadFromJsonAsync<QueueView>())!;
}

try
{
    await Until(async()=>{try{return (await http.GetAsync("/health")).IsSuccessStatusCode;}catch{return false;}},"API ready",60);
    var login=await http.PostAsJsonAsync("/staff/login",new{password=staffPassword});login.EnsureSuccessStatusCode();
    staffToken=(await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
    await Run("HTTP retry đồng thời, restart API và nội dung khác",async()=>
    {
        var doctor=await Doctor();var slot=await Slot(doctor,1);
        var body=new BookRequest(slot,"Kiểm thử retry","0901234567");var key=Secrets.NewToken();
        var responses=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>RetryBook(body,key)));
        var bookings=new List<Booking>();
        foreach(var response in responses)
        {
            using(response) { Check(response.StatusCode==HttpStatusCode.Created,await response.Content.ReadAsStringAsync());bookings.Add((await response.Content.ReadFromJsonAsync<Booking>())!); }
        }
        Check(bookings.All(b=>b.Id==bookings[0].Id && b.AccessToken==bookings[0].AccessToken),"Retries must return the same appointment and capability");
        await using var c=await db.Open();
        Check(await Database.Scalar<long>(c,"SELECT count(*) FROM appointments WHERE slot_id=$1",slot)==1,"Only one appointment");
        Check(await Database.Scalar<int>(c,"SELECT reserved FROM slots WHERE id=$1",slot)==1,"Only one reserved seat");
        Check(await Database.Scalar<long>(c,"SELECT count(*) FROM outbox WHERE kind='number.requested' AND body::jsonb->>'AppointmentId'=$1",bookings[0].Id.ToString())==1,"Only one number request");
        using var conflict=await RetryBook(body with {PatientName="Nội dung khác"},key);
        Check(conflict.StatusCode==HttpStatusCode.Conflict,"Same key with changed content must fail");
        using var invalid=await RetryBook(body,"invalid");Check(invalid.StatusCode==HttpStatusCode.BadRequest,"Invalid key must fail");
        await Compose("restart","api");
        await Until(async()=>{try{return (await http.GetAsync("/health")).IsSuccessStatusCode;}catch{return false;}},"API restart for retry",30);
        using var retry=await RetryBook(body,key);retry.EnsureSuccessStatusCode();
        var replay=(await retry.Content.ReadFromJsonAsync<Booking>())!;
        Check(replay.Id==bookings[0].Id && replay.AccessToken==bookings[0].AccessToken,"Retry must survive API restart");
        await Cancel(replay);
        using var cancelled=await RetryBook(body,key);
        Check((await cancelled.Content.ReadFromJsonAsync<Booking>())?.Status=="Cancelled","Retry of cancelled booking must not recreate it");
        Check(await Database.Scalar<int>(c,"SELECT reserved FROM slots WHERE id=$1",slot)==0,"Cancelled replay must not reserve again");
        return "8 POST cùng khóa: 1 lịch/1 chỗ/1 yêu cầu cấp số; khác nội dung 409, khóa sai 400; restart và hủy vẫn trả lịch cũ.";
    });
    await Run("1. Tranh chấp chỗ cuối",async()=>
    {
        var doctor=await Doctor();var slot=await Slot(doctor,1);
        var responses=await Task.WhenAll(http.PostAsJsonAsync("/appointments",new BookRequest(slot,"Người A","0901")),http.PostAsJsonAsync("/appointments",new BookRequest(slot,"Người B","0902")));
        Check(responses.Count(r=>r.StatusCode==HttpStatusCode.Created)==1 && responses.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1,"Expected exactly one 201 and one 409");
        await using var c=await db.Open();Check(await Database.Scalar<int>(c,"SELECT reserved FROM slots WHERE id=$1",slot)==1,"Reserved must be 1");
        Check(await Database.Scalar<long>(c,"SELECT count(*) FROM appointments WHERE slot_id=$1",slot)==1,"One appointment only");
        foreach(var response in responses)response.Dispose();return "Hai POST đồng thời: một 201, một 409; DB có đúng 1 lịch, reserved=1.";
    });
    await Run("2. Dừng và bật lại Queue Worker",async()=>
    {
        await Compose("stop","worker");var doctor=await Doctor();var b=await Book(await Slot(doctor));
        await Task.Delay(1200);var pending=await Get(b);Check(pending.Status=="Pending" && pending.Number is null,"Must remain Pending without Worker");
        await Compose("start","worker");await Until(async()=>(await Get(b)).Status=="Waiting","number after restart");
        Check((await Get(b)).Number==1,"First number must be 1");return "Worker dừng: Pending, number=null; bật lại: Waiting, number=1.";
    });
    await Run("3. RabbitMQ redelivery và yêu cầu trùng",async()=>
    {
        await Compose("stop","worker");var doctor=await Doctor();var b=await Book(await Slot(doctor));
        await using var broker=new Broker(config);BusMessage? body=null;
        await Until(async()=>
        {
            var ch=await broker.Channel(CancellationToken.None);var delivery=await ch.BasicGetAsync("clinic.numbers",false);
            if(delivery is null)return false;
            body=JsonSerializer.Deserialize<BusMessage>(delivery.Body.Span);
            Check(body?.AppointmentId==b.Id,"Unexpected pending delivery; use a dedicated demo stack");
            return true;
        },"outbox published");
        await broker.Reset(); // Close without ACK: RabbitMQ puts this delivery back with redelivered=true.
        var inspect=await broker.Channel(CancellationToken.None);var redelivery=await inspect.BasicGetAsync("clinic.numbers",false);
        Check(redelivery?.Redelivered==true,"Broker must mark actual redelivery");
        await broker.Reset();await Compose("start","worker");
        await Until(async()=>(await Get(b)).Number==1,"redelivery processed");
        var duplicate=JsonSerializer.Serialize(body);
        await broker.Publish("number.requested",duplicate,body!.Id,CancellationToken.None);
        await broker.Publish("number.requested",duplicate,body.Id,CancellationToken.None);
        await Task.Delay(1200);Check((await Get(b)).Number==1,"Duplicate must preserve number");
        await using var c=await db.Open();Check(await Database.Scalar<int>(c,"SELECT last_number FROM doctor_queues WHERE doctor_id=$1",doctor)==1,"Duplicate must not increment sequence");
        return "Đã quan sát Redelivered=true sau đóng channel chưa ACK; phát lại thêm 2 lần: số vẫn 1, last_number=1.";
    });
    await Run("4. Hủy trước khi Worker xử lý",async()=>
    {
        await Compose("stop","worker");var doctor=await Doctor();var slot=await Slot(doctor,1);var b=await Book(slot);
        await Cancel(b);await Cancel(b);
        await using var c=await db.Open();Check(await Database.Scalar<int>(c,"SELECT reserved FROM slots WHERE id=$1",slot)==0,"Repeated cancellation must release once");
        await Compose("start","worker");await Task.Delay(1800);
        var a=await Get(b);Check(a.Status=="Cancelled" && a.Number is null,"Cancelled must never get number");
        var replacement=await Book(slot);await Until(async()=>(await Get(replacement)).Number==1,"replacement takes released seat");
        return "Hủy 2 lần: reserved=0; Worker chạy lại: Cancelled, number=null; đặt lại được chỗ và nhận số 1.";
    });
    await Run("Phân quyền REST và chống truy cập lịch người khác",async()=>
    {
        var doctor=await Doctor();var b=await Book(await Slot(doctor));
        Check((await http.GetAsync($"/appointments/{b.Id}")).StatusCode==HttpStatusCode.Forbidden,"Anonymous GET must fail");
        Check((await http.DeleteAsync($"/appointments/{b.Id}")).StatusCode==HttpStatusCode.Forbidden,"Anonymous DELETE must fail");
        Check((await http.PostAsync($"/internal/queues/{doctor}/next",null)).StatusCode==HttpStatusCode.Unauthorized,"Anonymous staff action must fail");
        return "GET/DELETE cá nhân không có khóa trả 403; gọi lượt không đăng nhập trả 401.";
    });
    await Run("Thứ tự gọi theo khung giờ và chặn lượt chưa đến",async()=>
    {
        var doctor=await Doctor();var late=await Slot(doctor,5,2);var early=await Slot(doctor,5,1);var future=await Slot(doctor,5,future:true);
        var a=await Book(late);await Until(async()=>(await Get(a)).Number!=null,"late numbered");
        var b=await Book(early);var f=await Book(future);await Until(async()=>(await Get(f)).Number!=null,"future numbered");
        var second=(await Get(b)).Number;Check((await Advance(doctor,"next")).CurrentNumber==second,"Earlier slot must be called before lower number in later slot");
        await Advance(doctor,"complete");Check((await Advance(doctor,"next")).CurrentNumber==(await Get(a)).Number,"Later eligible slot next");await Advance(doctor,"complete");
        using var request=new HttpRequestMessage(HttpMethod.Post,$"/internal/queues/{doctor}/next");request.Headers.Authorization=new("Bearer",staffToken);
        Check((await http.SendAsync(request)).StatusCode==HttpStatusCode.Conflict,"Future slot cannot be called");
        return "Khung sớm được gọi trước dù số lớn hơn; khung tương lai không được gọi (409).";
    });

    await Run("Outbox bền vững khi RabbitMQ ngừng và API restart",async()=>
    {
        await Compose("stop","rabbitmq");
        var doctor=await Doctor();var b=await Book(await Slot(doctor));
        Check((await Get(b)).Status=="Pending","Broker down booking remains Pending");
        await using(var c=await db.Open())
            Check(await Database.Scalar<long>(c,"SELECT count(*) FROM outbox WHERE kind='number.requested' AND sent_at IS NULL AND body::jsonb->>'AppointmentId'=$1",b.Id.ToString())==1,"Unsent outbox must persist");
        await Compose("restart","api");
        await Until(async()=>{try{return (await http.GetAsync("/health")).IsSuccessStatusCode;}catch{return false;}},"API restarted",30);
        Check((await Get(b)).Status=="Pending","Appointment must persist through API restart");
        await Compose("start","rabbitmq");
        await Until(async()=>(await Get(b)).Status=="Waiting","outbox drains after broker returns",60);
        Check((await Get(b)).Number==1,"Exactly one number after recovery");
        return "Đặt thành công khi broker dừng; quan sát outbox chưa gửi, restart API vẫn còn Pending; bật broker tự cấp số 1.";
    });

    var localBrowsers=Path.Combine(root,".local","browsers");
    if(Directory.Exists(localBrowsers) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")))
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH",localBrowsers);
    using var playwright=await Playwright.CreateAsync();
    var browserPath=Environment.GetEnvironmentVariable("TEST_BROWSER") ?? (Directory.Exists(localBrowsers)?null:@"C:\Program Files\Google\Chrome\Application\chrome.exe");
    await using var browser=await playwright.Chromium.LaunchAsync(new() { ExecutablePath=File.Exists(browserPath)?browserPath:null,Headless=true,Args=["--no-sandbox"] });
    await using var patientContext=await browser.NewContextAsync(new() { ViewportSize=new(){Width=1440,Height=1050} });
    await using var staffContext=await browser.NewContextAsync(new() { ViewportSize=new(){Width=1440,Height=1050} });
    var patient=await patientContext.NewPageAsync();var staffPage=await staffContext.NewPageAsync();
    var pageErrors=new List<string>();patient.PageError+=(_,e)=>pageErrors.Add(e);staffPage.PageError+=(_,e)=>pageErrors.Add(e);
    var doc=await Doctor();await Slot(doc);
    var patientNavigations=0;patient.FrameNavigated+=(_,f)=>{if(f==patient.MainFrame)patientNavigations++;};
    await Run("5. Hai cửa sổ bệnh nhân/nhân viên cập nhật SignalR",async()=>
    {
        await patient.GotoAsync(http.BaseAddress!.ToString());await staffPage.GotoAsync(new Uri(http.BaseAddress!,"/staff.html").ToString());
        await patient.Locator("#doctor").SelectOptionAsync(doc.ToString());await staffPage.Locator("#doctor").SelectOptionAsync(doc.ToString());
        await Assertions.Expect(patient.Locator("#connection")).ToHaveAttributeAsync("data-state","connected",new(){Timeout=20000});
        await patient.Locator(".slot").First.ClickAsync();await patient.Locator("#patientName").FillAsync("Nguyễn Demo SignalR");await patient.Locator("#phone").FillAsync("0901234567");await patient.Locator("#book").ClickAsync();
        await Assertions.Expect(patient.Locator("#myStatus")).ToHaveTextAsync("Đang chờ khám",new(){Timeout=20000});
        await staffPage.Locator("#staffPassword").FillAsync(staffPassword);await staffPage.Locator("#login button").ClickAsync();await staffPage.Locator("#next").ClickAsync();
        await Assertions.Expect(patient.Locator("#current")).ToHaveTextAsync("1",new(){Timeout=15000});
        await Assertions.Expect(patient.Locator("#myStatus")).ToHaveTextAsync("Đến lượt khám của bạn");
        Check(patientNavigations==1,"Patient must not reload");Check(pageErrors.Count==0,string.Join('\n',pageErrors));
        await patient.ScreenshotAsync(new(){Path=Path.Combine(output,"patient-live.png"),FullPage=true});await staffPage.ScreenshotAsync(new(){Path=Path.Combine(output,"staff-live.png"),FullPage=true});
        return "Chrome thật: đặt lịch qua form, nhân viên gọi số 1, bệnh nhân tự cập nhật lượt và trạng thái; chỉ 1 navigation, không lỗi JS.";
    });
    await Run("6. Client ngắt mạng, kết nối lại và đồng bộ API",async()=>
    {
        var gets=0;patient.Request+=(_,r)=>{if(r.Url.Contains($"/queues/{doc}"))gets++;};
        await patientContext.SetOfflineAsync(true);
        await Assertions.Expect(patient.Locator("#connection")).Not.ToHaveAttributeAsync("data-state","connected",new(){Timeout=45000});
        await staffPage.Locator("#complete").ClickAsync();
        await Assertions.Expect(staffPage.Locator("#current")).ToHaveTextAsync("—");
        Check(await patient.Locator("#current").InnerTextAsync()=="1","Offline patient should still display last known number");
        await patientContext.SetOfflineAsync(false);
        await Assertions.Expect(patient.Locator("#connection")).ToHaveAttributeAsync("data-state","connected",new(){Timeout=30000});
        await Assertions.Expect(patient.Locator("#current")).ToHaveTextAsync("—",new(){Timeout=10000});
        await Assertions.Expect(patient.Locator("#myStatus")).ToHaveTextAsync("Đã hoàn thành");
        Check(gets>0,"Reconnect must fetch current queue via API");Check(patientNavigations==1,"Reconnect must not reload");
        return $"Ngắt mạng context bệnh nhân, hoàn thành lượt khi offline; nối lại tự GET API ({gets} request) và hiển thị đúng, không reload.";
    });
    await Run("SignalR từ chối nhóm lịch riêng nếu khóa sai",async()=>
    {
        var result=await patient.EvaluateAsync<bool>("""
        async () => {
          const cfg=await (await fetch('/client-config')).json();
          const c=new signalR.HubConnectionBuilder().withUrl(cfg.notificationUrl+'/hubs/queue').build();
          await c.start();
          try { await c.invoke('JoinAppointment',document.querySelector('#appointmentId').value,'wrong-token');return false; }
          catch { return true; }
          finally { await c.stop(); }
        }
        """);
        Check(result,"Unauthorized private group must reject");return "Hub JoinAppointment với khóa sai bị từ chối trong trình duyệt thật.";
    });
    await patientContext.SetOfflineAsync(false);
    await Run("Chỗ trống cập nhật khi Worker dừng và reconnect",async()=>
    {
        await Compose("stop","worker");
        try
        {
            var doctor=await Doctor();var slot=await Slot(doctor,2);
            await using var context=await browser.NewContextAsync();var page=await context.NewPageAsync();
            await page.GotoAsync(http.BaseAddress!.ToString());await page.Locator("#doctor").SelectOptionAsync(doctor.ToString());
            await Assertions.Expect(page.Locator("#connection")).ToHaveAttributeAsync("data-state","connected");
            var button=page.Locator($"[data-slot-id='{slot}']");await button.ClickAsync();
            var b=await Book(slot);
            await Assertions.Expect(button.Locator("small")).ToHaveTextAsync("Còn 1/2 chỗ",new(){Timeout=15000});
            await Assertions.Expect(button).ToHaveClassAsync("slot selected");
            Check((await Get(b)).Status=="Pending","Availability must update before number assignment");
            await context.SetOfflineAsync(true);
            await Assertions.Expect(page.Locator("#connection")).Not.ToHaveAttributeAsync("data-state","connected",new(){Timeout=45000});
            await Cancel(b);await context.SetOfflineAsync(false);
            await Assertions.Expect(page.Locator("#connection")).ToHaveAttributeAsync("data-state","connected",new(){Timeout=30000});
            await Assertions.Expect(button.Locator("small")).ToHaveTextAsync("Còn 2/2 chỗ");
            b=await Book(slot);await Assertions.Expect(button.Locator("small")).ToHaveTextAsync("Còn 1/2 chỗ",new(){Timeout=15000});
            await Cancel(b);await Assertions.Expect(button.Locator("small")).ToHaveTextAsync("Còn 2/2 chỗ",new(){Timeout=15000});
            return "Worker dừng: tab khác đặt/hủy cập nhật chỗ; giữ lựa chọn khung giờ, reconnect lấy lại chỗ mới nhất.";
        }
        finally { await Compose("start","worker"); }
    });
    await Run("Khôi phục đúng bác sĩ và ngày sau reload",async()=>
    {
        var doctor=await Doctor();var tomorrow=ClinicTime.Today.AddDays(1);var slot=Guid.NewGuid();
        await using(var c=await db.Open())
            await Database.Execute(c,"INSERT INTO slots(id,doctor_id,visit_date,starts_at,ends_at,capacity) VALUES($1,$2,$3,$4,$5,2)",slot,doctor,tomorrow,ClinicTime.At(tomorrow,TimeOnly.MinValue).ToUniversalTime(),ClinicTime.At(tomorrow.AddDays(1),TimeOnly.MinValue).ToUniversalTime());
        var b=await Book(slot);await using var context=await browser.NewContextAsync();var page=await context.NewPageAsync();
        await page.GotoAsync(http.BaseAddress!.ToString());
        await page.EvaluateAsync("saved => sessionStorage.setItem('appointment',JSON.stringify(saved))",new{id=b.Id,token=b.AccessToken});
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator("#doctor")).ToHaveValueAsync(doctor.ToString());
        await Assertions.Expect(page.Locator("#date")).ToHaveValueAsync(tomorrow.ToString("yyyy-MM-dd"));
        await Assertions.Expect(page.Locator("#myName")).ToHaveTextAsync("Bệnh nhân kiểm thử");
        await Assertions.Expect(page.Locator("#connection")).ToHaveAttributeAsync("data-state","connected");
        return "Reload lịch ngày mai của bác sĩ khác: bộ chọn bác sĩ/ngày và lịch cá nhân khớp nhau.";
    });
    await Run("Trình duyệt mất phản hồi đặt lịch rồi reload và thử lại",async()=>
    {
        var doctor=await Doctor();var slot=await Slot(doctor,1);
        await using var context=await browser.NewContextAsync();var page=await context.NewPageAsync();
        await page.GotoAsync(http.BaseAddress!.ToString());await page.Locator("#doctor").SelectOptionAsync(doctor.ToString());
        await page.Locator($"[data-slot-id='{slot}']").ClickAsync();
        await page.Locator("#patientName").FillAsync("Mất phản hồi");await page.Locator("#phone").FillAsync("0901234567");
        await page.RouteAsync("**/appointments",async route=>
        {
            if(route.Request.Method=="POST") { await route.FetchAsync();await route.AbortAsync("failed"); }
            else await route.ContinueAsync();
        });
        await page.Locator("#book").ClickAsync();await Assertions.Expect(page.Locator("#retryBooking")).ToBeVisibleAsync();
        await page.UnrouteAsync("**/appointments");await page.ReloadAsync();
        await Assertions.Expect(page.Locator("#retryBooking")).ToBeVisibleAsync();await page.Locator("#retryBooking").ClickAsync();
        await Assertions.Expect(page.Locator("#myName")).ToHaveTextAsync("Mất phản hồi",new(){Timeout=15000});
        await Assertions.Expect(page.Locator("#doctor")).ToHaveValueAsync(doctor.ToString());
        await Assertions.Expect(page.Locator("#retryBooking")).ToBeHiddenAsync();
        await using var c=await db.Open();
        Check(await Database.Scalar<long>(c,"SELECT count(*) FROM appointments WHERE slot_id=$1",slot)==1,"Lost HTTP response must not duplicate booking");
        Check(await Database.Scalar<int>(c,"SELECT reserved FROM slots WHERE id=$1",slot)==1,"Lost HTTP response must reserve once");
        return "Server lưu nhưng phản hồi bị chặn; reload và thử lại trả lịch cũ, DB chỉ có 1 lịch và reserved=1.";
    });
}
catch(Exception ex) {failed=true;results.Add(new{name="Test infrastructure",status="FAIL",evidence=ex.ToString()});Console.WriteLine(ex);}
finally
{
    try {await Compose("start","rabbitmq","worker");}catch(Exception ex){Console.WriteLine(ex.Message);}
    // Remove only fixtures created by this run; never reset shared volumes or purge queues.
    try
    {
        await Task.Delay(500);await using var c=await db.Open();
        foreach(var doctor in doctors)
        {
            await using var tx=await c.BeginTransactionAsync();
            var patients=await Database.Read(c,"SELECT patient_id FROM appointments WHERE doctor_id=$1",r=>r.GetGuid(0),doctor);
            await Database.Execute(c,"DELETE FROM outbox WHERE (body::jsonb->>'DoctorId')::int=$1",doctor);
            await Database.Execute(c,"DELETE FROM appointments WHERE doctor_id=$1",doctor);await Database.Execute(c,"DELETE FROM slots WHERE doctor_id=$1",doctor);
            await Database.Execute(c,"DELETE FROM doctor_queues WHERE doctor_id=$1",doctor);await Database.Execute(c,"DELETE FROM doctors WHERE id=$1",doctor);
            foreach(var patient in patients)await Database.Execute(c,"DELETE FROM patients WHERE id=$1",patient);
            await tx.CommitAsync();
        }
    }
    catch(Exception ex){Console.WriteLine("Fixture cleanup: "+ex.Message);}
    await File.WriteAllTextAsync(Path.Combine(output,"smoke-results.json"),JsonSerializer.Serialize(new{runId,finishedAt=DateTimeOffset.UtcNow,failed,results},new JsonSerializerOptions{WriteIndented=true}));
}
return failed?1:0;
record Booking(Guid Id,string AccessToken,string Status);
