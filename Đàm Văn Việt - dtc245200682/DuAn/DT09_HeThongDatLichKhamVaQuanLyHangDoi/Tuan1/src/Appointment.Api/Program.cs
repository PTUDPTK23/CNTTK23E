using System.Threading.RateLimiting;
using Clinic.Core;
using Microsoft.AspNetCore.RateLimiting;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["Urls"]!);
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=16_384);
builder.Services.AddClinic(builder.Configuration).AddStaffAuth(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddRateLimiter(o=>
{
    o.RejectionStatusCode=429;
    o.AddPolicy("login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local",_=>new FixedWindowRateLimiterOptions { PermitLimit=10,Window=TimeSpan.FromMinutes(1) }));
});
var app=builder.Build();
app.Use(async(context,next)=>
{
    context.Response.Headers.CacheControl="no-store";
    context.Response.Headers.XContentTypeOptions="nosniff";
    context.Response.Headers["Referrer-Policy"]="no-referrer";
    try { await next(); }
    catch(RuleException ex) { context.Response.StatusCode=ex.StatusCode; await context.Response.WriteAsJsonAsync(new { error=ex.Message }); }
    catch(Exception ex) when(ex is not OperationCanceledException)
    {
        app.Logger.LogError(ex,"Request {Method} {Path} failed",context.Request.Method,context.Request.Path);
        context.Response.StatusCode=500; await context.Response.WriteAsJsonAsync(new {error="Hệ thống tạm thời chưa xử lý được. Vui lòng thử lại."});
    }
});
app.UseDefaultFiles(); app.UseStaticFiles(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/client-config",()=>new { notificationUrl=app.Configuration["Notification:PublicUrl"],today=ClinicTime.Today });
app.MapGet("/health",async(Database db)=>
{
    await using var c=await db.Open();
    return Results.Ok(new {status=await Database.Scalar<int>(c,"SELECT 1")==1?"ready":"unready",service="Appointment API"});
});
app.MapGet("/doctors",(Database db)=>db.Doctors());
app.MapGet("/doctors/{id:int}/slots",(int id,DateOnly? date,Database db)=>db.Slots(id,date ?? ClinicTime.Today));
app.MapPost("/appointments",async(BookRequest request,Database db,HttpContext ctx,IConfiguration config)=>
{
    var key=ctx.Request.Headers.ContainsKey("Idempotency-Key") ? ctx.Request.Headers["Idempotency-Key"].ToString() : null;
    var (id,token)=await db.Book(request,key,config["Auth:SigningKey"]);
    var appointment=await db.GetAppointment(id);
    return Results.Created($"/appointments/{id}",new {id,accessToken=token,status=appointment.Status,statusLabel=appointment.Status switch {
        "Pending"=>"Chờ cấp số", "Waiting"=>"Đang chờ khám", "Serving"=>"Đến lượt khám của bạn",
        "Completed"=>"Đã hoàn thành", "Cancelled"=>"Đã hủy", _=>"Đã hết hạn" }});
});
app.MapGet("/appointments/{id:guid}",async(Guid id,HttpContext ctx,Database db)=>
{
    if(!ctx.User.IsInRole("Staff") && !await db.CanView(id,ctx.Request.Headers["X-Appointment-Token"])) return Results.StatusCode(403);
    return Results.Ok(await db.GetAppointment(id));
});
app.MapDelete("/appointments/{id:guid}",async(Guid id,HttpContext ctx,Database db)=>
{
    if(!ctx.User.IsInRole("Staff") && !await db.CanView(id,ctx.Request.Headers["X-Appointment-Token"])) return Results.StatusCode(403);
    await db.Cancel(id); return Results.NoContent();
});
app.MapGet("/queues/{doctorId:int}",(int doctorId,DateOnly? date,Database db)=>db.Queue(doctorId,date ?? ClinicTime.Today));
app.MapPost("/staff/login",(LoginRequest request,IConfiguration config)=>
    Secrets.Equal(request.Password ?? "",config["Auth:StaffPassword"]!) ? Results.Ok(new {token=StaffAuth.Issue(config),expiresIn=14400}) : Results.Unauthorized()).RequireRateLimiting("login");
app.MapPost("/internal/queues/{doctorId:int}/next",(int doctorId,DateOnly? date,Database db)=>db.Advance(doctorId,date ?? ClinicTime.Today,false)).RequireAuthorization("Staff");
app.MapPost("/internal/queues/{doctorId:int}/complete",(int doctorId,DateOnly? date,Database db)=>db.Advance(doctorId,date ?? ClinicTime.Today,true)).RequireAuthorization("Staff");
await app.Services.InitializeClinic(app.Lifetime.ApplicationStopping);
await app.RunAsync();
record LoginRequest(string? Password);
