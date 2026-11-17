using FileProcessing.Core.Interfaces;
using FileProcessing.Infrastructure.Messaging;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Processing;
using FileProcessing.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileProcessing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in configuration.");

        services.AddDbContext<FileProcessingDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(FileProcessingDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        // Storage Service
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Image Processing Service
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();

        // Messaging Configuration & Services
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IJobQueueChannel, InMemoryJobQueue>();
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        // Outbox Publisher Background Service
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}
