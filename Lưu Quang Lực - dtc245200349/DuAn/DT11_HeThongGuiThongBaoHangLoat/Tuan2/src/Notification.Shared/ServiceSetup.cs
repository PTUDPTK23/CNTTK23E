using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Notification.Shared;

public static class ServiceSetup
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connection = config.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings__Database.");
        services.AddSingleton(NpgsqlDataSource.Create(connection));
        services.AddSingleton<NotificationStore>();
        services.AddSingleton<Broker>();
        return services;
    }
}
