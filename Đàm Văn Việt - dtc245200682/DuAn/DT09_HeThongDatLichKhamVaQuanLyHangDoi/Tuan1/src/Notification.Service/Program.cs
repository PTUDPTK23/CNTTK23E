using Clinic.Core;
using Microsoft.AspNetCore.SignalR;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["Urls"]!);
builder.Services.AddClinic(builder.Configuration);
builder.Services.AddSignalR(o=> { o.MaximumReceiveMessageSize=4096; o.EnableDetailedErrors=false; });
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ?? ["http://localhost:5080"]).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddHostedService<NotificationConsumer>();
var app=builder.Build();
app.UseCors();
app.MapHub<QueueHub>("/hubs/queue");
app.MapGet("/health",()=>new { status="ready",service="Notification Service" });
await app.Services.InitializeClinic(app.Lifetime.ApplicationStopping);
await app.RunAsync();

public sealed class QueueHub(Database db) : Hub
{
    public static string QueueGroup(int doctor,DateOnly date)=>$"queue:{doctor}:{date:yyyy-MM-dd}";
    public static string AppointmentGroup(Guid id)=>$"appointment:{id}";
    public Task JoinQueue(int doctorId,string date)
    {
        if(doctorId<=0 || !DateOnly.TryParseExact(date,"yyyy-MM-dd",out var parsed)) throw new HubException("Hàng đợi không hợp lệ.");
        return Groups.AddToGroupAsync(Context.ConnectionId,QueueGroup(doctorId,parsed));
    }
    public Task LeaveQueue(int doctorId,string date)=>Groups.RemoveFromGroupAsync(Context.ConnectionId,$"queue:{doctorId}:{date}");
    public async Task JoinAppointment(string id,string accessToken)
    {
        if(!Guid.TryParse(id,out var parsed) || !await db.CanView(parsed,accessToken)) throw new HubException("Không có quyền xem lịch hẹn.");
        await Groups.AddToGroupAsync(Context.ConnectionId,AppointmentGroup(parsed));
    }
    public Task LeaveAppointment(string id)=>Groups.RemoveFromGroupAsync(Context.ConnectionId,$"appointment:{id}");
}
sealed class NotificationConsumer(IHubContext<QueueHub> hub,IConfiguration config,ILogger<NotificationConsumer> log) : RabbitConsumer(config,log,"clinic.notifications")
{
    protected override async Task Handle(BusMessage message,CancellationToken ct)
    {
        // Invalidation only: clients fetch authoritative state, so duplicate or reordered events are harmless.
        await hub.Clients.Group(QueueHub.QueueGroup(message.DoctorId,message.Date)).SendAsync("QueueChanged",new {doctorId=message.DoctorId,date=message.Date},ct);
        await hub.Clients.Group(QueueHub.AppointmentGroup(message.AppointmentId)).SendAsync("AppointmentChanged",new {id=message.AppointmentId},ct);
    }
}
