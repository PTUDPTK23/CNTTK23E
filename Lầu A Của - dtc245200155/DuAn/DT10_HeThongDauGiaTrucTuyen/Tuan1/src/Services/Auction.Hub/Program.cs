using Auction.Hub.Consumers;
using Auction.Hub.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configure port
var port = builder.Configuration.GetValue<int>("PORT", 5003);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

// Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClient", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddHostedService<RabbitMqHubConsumer>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("AllowWebClient");

app.MapHub<AuctionHub>("/hubs/auction");
app.MapHealthChecks("/health");

app.MapGet("/", () => "Auction SignalR Hub is running at /hubs/auction");

app.Run();
