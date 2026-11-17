using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediQueue.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMediQueueInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("MediQueue")
            ?? throw new InvalidOperationException(
                "Connection string 'MediQueue' is required. Configure it with User Secrets or an environment variable.");

        services.AddDbContext<MediQueueDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(MediQueueDbContext).Assembly.FullName)));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MediQueueDbContext>();

        return services;
    }
}
