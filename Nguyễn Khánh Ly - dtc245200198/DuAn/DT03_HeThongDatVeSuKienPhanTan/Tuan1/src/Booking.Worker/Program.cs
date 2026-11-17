using EventBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Booking.Worker;

var builder = Host.CreateApplicationBuilder(args);
// Cấu hình kết nối PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
