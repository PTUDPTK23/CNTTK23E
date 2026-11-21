using Microsoft.EntityFrameworkCore;
using Notification.Worker;
using Shared.Contracts.Data;

var builder = Host.CreateApplicationBuilder(args);

// 1. Cấu hình kết nối PostgreSQL từ file appsettings.json hoặc biến môi trường (ConnectionStrings__PostgreSql)
var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    ?? throw new InvalidOperationException("PostgreSQL Connection String not found in appsettings.json or environment variables.");

// 2. Đăng ký Entity Framework Core với PostgreSQL
builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// 3. Đăng ký cấu hình Worker
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));

// 4. Đăng ký Background Worker Service
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
host.Run();
