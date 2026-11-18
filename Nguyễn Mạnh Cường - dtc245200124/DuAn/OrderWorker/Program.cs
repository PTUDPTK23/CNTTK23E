using Microsoft.EntityFrameworkCore;
using OrderWorker;
using OrderWorker.Data;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? "Host=localhost;Port=5432;Database=distributed_orders;Username=orders;Password=orders";
builder.Services.AddDbContext<WorkerDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
