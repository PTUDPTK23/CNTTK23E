using MediQueue.Contracts.Health;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".MediQueue.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAuthenticationSessionStore, InMemoryAuthenticationSessionStore>();
builder.Services.AddScoped<IMvcAuthenticationService, MvcAuthenticationService>();
builder.Services.AddScoped<MediQueueCookieEvents>();

builder.Services.AddOptions<BackendApiOptions>()
    .BindConfiguration(BackendApiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<BackendApiOptions>, BackendApiOptionsValidator>();
builder.Services.AddHttpClient<IAccountApiClient, AccountApiClient>((services, client) =>
{
    BackendApiOptions options = services.GetRequiredService<IOptions<BackendApiOptions>>().Value;
    client.BaseAddress = options.BaseAddress;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/problem+json");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient<IDashboardApiClient, DashboardApiClient>((services, client) =>
{
    BackendApiOptions options = services.GetRequiredService<IOptions<BackendApiOptions>>().Value;
    client.BaseAddress = options.BaseAddress;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/problem+json");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddAuthentication(MediQueueAuthenticationDefaults.Scheme)
    .AddCookie(MediQueueAuthenticationDefaults.Scheme, options =>
    {
        options.Cookie.Name = MediQueueAuthenticationDefaults.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/access-denied";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
        options.EventsType = typeof(MediQueueCookieEvents);
    });
builder.Services.AddAuthorization();
ConfigureDataProtection(builder);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/health", () =>
    Results.Ok(new ServiceStatusResponse("MediQueue.Web", "Healthy")));
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();

static void ConfigureDataProtection(WebApplicationBuilder builder)
{
    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("MediQueue.Web");
    if (builder.Environment.IsDevelopment())
    {
        dataProtection.UseEphemeralDataProtectionProvider();
        return;
    }

    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, ".data-protection")));
    if (OperatingSystem.IsWindows())
    {
        dataProtection.ProtectKeysWithDpapi();
    }
}
