using Library.Shared;
using Library.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Library")));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<ReservationConsumer>();
await builder.Build().RunAsync();
