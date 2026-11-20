using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.AspNetCore.SignalR;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddHostedService<NotificationConsumer>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:3000")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var app=builder.Build();
app.UseCors("Frontend");
var signingKey=Environment.GetEnvironmentVariable("AUTH_SIGNING_KEY")??"local-only-signing-key-change-before-deployment-32chars";
app.Use(async(context,next)=>{
 var bearer=context.Request.Headers.Authorization.ToString();
 var token=bearer.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)?bearer[7..]:context.Request.Query["access_token"].ToString();
 if(TryReadToken(token,signingKey,out var user,out var role)){context.Items["user"]=user;context.Items["role"]=role;}
 if(context.Request.Path.StartsWithSegments("/hubs/notifications")&&context.Items["user"] is null){context.Response.StatusCode=401;return;}
 await next();
});
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapGet("/health",()=>Results.Ok(new{status="healthy"}));
app.Run();

static bool TryReadToken(string token,string key,out string user,out string role){
 user=role="";
 try{
  var parts=token.Split('.');if(parts.Length!=2)return false;
  var expected=HMACSHA256.HashData(Encoding.UTF8.GetBytes(key),Encoding.UTF8.GetBytes(parts[0]));
  if(!CryptographicOperations.FixedTimeEquals(expected,Convert.FromBase64String(parts[1])))return false;
  using var doc=JsonDocument.Parse(Convert.FromBase64String(parts[0]));var root=doc.RootElement;
  user=root.GetProperty("User").GetString()??"";role=root.GetProperty("Role").GetString()??"";
  return root.GetProperty("Expires").GetInt64()>DateTimeOffset.UtcNow.ToUnixTimeSeconds()&&user.Length>0;
 }catch{return false;}
}

record TicketNotification(Guid TicketId,string RequesterId,string Status,string? AssignedGroup,string? AssignedAgent,string? ChangedBy,string? ChangeType);
class NotificationsHub:Hub {
 public override async Task OnConnectedAsync(){
  var http=Context.GetHttpContext();
  var user=http?.Items["user"] as string; var role=http?.Items["role"] as string;
  if(user is not null) await Groups.AddToGroupAsync(Context.ConnectionId,"user:"+user);
  if(role is "Agent" or "Admin") await Groups.AddToGroupAsync(Context.ConnectionId,"support");
  await base.OnConnectedAsync();
 }
}
class NotificationConsumer(ILogger<NotificationConsumer> log,IHubContext<NotificationsHub> hub):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken token){
  var factory=new ConnectionFactory{HostName=Environment.GetEnvironmentVariable("RABBITMQ_HOST")??"localhost",UserName=Environment.GetEnvironmentVariable("RABBITMQ_USER")??"guest",Password=Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")??"guest"};
  while(!token.IsCancellationRequested){
   try {
    await using var conn=await factory.CreateConnectionAsync(token); await using var ch=await conn.CreateChannelAsync(cancellationToken:token);
    await ch.ExchangeDeclareAsync("support.events",ExchangeType.Topic,durable:true,cancellationToken:token);
    await ch.QueueDeclareAsync("notification.queue",durable:true,exclusive:false,autoDelete:false,cancellationToken:token);
    await ch.QueueBindAsync("notification.queue","support.events","ticket.assigned",cancellationToken:token);
    await ch.QueueBindAsync("notification.queue","support.events","ticket.status.changed",cancellationToken:token);
    var consumer=new AsyncEventingBasicConsumer(ch);
    consumer.ReceivedAsync+=async(_,ea)=>{
     try{
      var evt=JsonSerializer.Deserialize<TicketNotification>(Encoding.UTF8.GetString(ea.Body.ToArray()));
      if(evt is not null){ await hub.Clients.Group("support").SendAsync("TicketUpdated",evt,token); await hub.Clients.Group("user:"+evt.RequesterId).SendAsync("TicketUpdated",evt,token); }
      await ch.BasicAckAsync(ea.DeliveryTag,false,token);
     }catch(Exception ex){log.LogError(ex,"Notification handling failed");await ch.BasicNackAsync(ea.DeliveryTag,false,true,token);}
    };
    await ch.BasicConsumeAsync("notification.queue",false,consumer,cancellationToken:token);
    log.LogInformation("Notification service consuming queue");
    await Task.Delay(Timeout.Infinite,token);
   }catch(OperationCanceledException) when(token.IsCancellationRequested){break;}
   catch(Exception ex){log.LogWarning(ex,"RabbitMQ unavailable; retrying in 5 seconds");await Task.Delay(5000,token);}
  }
 }
}
