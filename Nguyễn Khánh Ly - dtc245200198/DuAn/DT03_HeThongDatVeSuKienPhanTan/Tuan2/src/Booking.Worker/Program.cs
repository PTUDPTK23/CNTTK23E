using Booking.Worker;
using Booking.Worker.Services;
using EventBooking.Infrastructure.Data;
using EventBooking.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Cấu hình kết nối PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình RabbitMQ Consumer và dịch vụ xử lý đặt vé
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddScoped<IBookingProcessor, BookingProcessor>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
