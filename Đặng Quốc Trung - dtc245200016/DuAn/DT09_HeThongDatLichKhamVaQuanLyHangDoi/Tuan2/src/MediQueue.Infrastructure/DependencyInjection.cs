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
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 3;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MediQueueDbContext>();

        services.AddScoped<IIdentityBootstrapper, IdentityBootstrapper>();

        return services;
    }
}
