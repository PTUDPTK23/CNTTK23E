using Campaign.Api;
using Microsoft.AspNetCore.Diagnostics;
using Notification.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.Services.AddProblemDetails();
AdminAccess.Register(builder.Services, builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<RetryScheduler>();
var app = builder.Build();
await app.Services.GetRequiredService<NotificationStore>().InitializeAsync(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError("Request failed TraceId={TraceId} ErrorType={ErrorType}", context.TraceIdentifier, error?.GetType().Name);
    await Results.Problem(statusCode: 503, title: "Dịch vụ tạm thời không sẵn sàng. Vui lòng thử lại.")
        .ExecuteAsync(context);
}));
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; frame-ancestors 'none'";
    if (context.Request.Path.StartsWithSegments("/campaigns") || context.Request.Path.StartsWithSegments("/auth")
        || context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    // Cross-origin forms cannot add this header. CORS is intentionally not enabled.
    if (HttpMethods.IsPost(context.Request.Method) && context.Request.Headers["X-NotifyLab-Request"] != "1")
    {
        await Results.Problem(statusCode: 403, title: "Yêu cầu thiếu X-NotifyLab-Request.").ExecuteAsync(context);
        return;
    }
    await next(context);
});
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
    {
        // A tab with an old dashboard script still gets the login page on its next navigation.
        if ((path == "/" || path == "/index.html") && !context.User.IsInRole(AdminAccess.Role))
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Redirect("/login.html");
            return;
        }
    }
    await next(context);
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = file => file.Context.Response.Headers.CacheControl =
        string.Equals(Path.GetExtension(file.File.Name), ".html", StringComparison.OrdinalIgnoreCase)
            ? "no-store"
            : "no-cache"
});
app.UseAuthorization();
app.UseRateLimiter();
AdminAccess.MapEndpoints(app);

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" })).AllowAnonymous();
app.MapGet("/health/ready", async (NotificationStore store, CancellationToken ct) =>
    await store.IsHealthyAsync(ct) ? Results.Ok(new { status = "ready", dependency = "postgres" }) : Results.StatusCode(503))
    .AllowAnonymous();
app.MapGet("/api/info", (IConfiguration config) => Results.Ok(new
{
    maxRecipients = CampaignRules.MaxRecipients,
    demoFailures = config.GetValue("Delivery:EnableDemoFailures", false),
    externalSmtp = config.GetValue("Delivery:ExternalSmtp", false),
    maxAttempts = Math.Clamp(config.GetValue("Delivery:MaxAttempts", 3), 1, 10)
}));

app.MapPost("/campaigns", async (CreateCampaign input, NotificationStore store, CancellationToken ct) =>
{
    var (campaign, error) = CampaignRules.Validate(input);
    if (error is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["campaign"] = [error] });
    var id = await store.CreateAsync(campaign!, ct);
    return Results.Created($"/campaigns/{id}", new { id, recipientCount = campaign!.Recipients.Length });
});
app.MapGet("/campaigns", async (int? page, int? pageSize, NotificationStore store, CancellationToken ct) =>
{
    if (!ValidPage(page, pageSize)) return Results.BadRequest(new { error = "page >= 1, pageSize từ 1 đến 100." });
    return Results.Ok(await store.ListAsync(page ?? 1, pageSize ?? 20, ct));
});
app.MapGet("/campaigns/{id:guid}", async (Guid id, NotificationStore store, CancellationToken ct) =>
    await store.GetAsync(id, ct) is { } campaign ? Results.Ok(campaign) : Results.NotFound());
app.MapPost("/campaigns/{id:guid}/start", async (Guid id, NotificationStore store, CancellationToken ct) =>
    await store.StartAsync(id, ct) ? Results.Accepted($"/campaigns/{id}", new { id }) : Results.NotFound());
app.MapPost("/campaigns/{id:guid}/retry-failed", async (Guid id, NotificationStore store, CancellationToken ct) =>
{
    var retried = await store.RetryFailedAsync(id, ct);
    return retried is null ? Results.NotFound() : Results.Accepted($"/campaigns/{id}", new { id, retried });
});
app.MapGet("/campaigns/{id:guid}/deliveries", async (Guid id, string? status, int? page, int? pageSize,
    NotificationStore store, CancellationToken ct) =>
{
    if (!ValidPage(page, pageSize) || status is not (null or "" or "Pending" or "Sent" or "Failed"))
        return Results.BadRequest(new { error = "Bộ lọc: Pending, Sent, Failed; page >= 1, pageSize từ 1 đến 100." });
    if (await store.GetAsync(id, ct) is null) return Results.NotFound();
    return Results.Ok(await store.DeliveriesAsync(id, status, page ?? 1, pageSize ?? 20, ct));
});
app.MapGet("/campaigns/{id:guid}/deliveries/{deliveryId:guid}/attempts", async (Guid id, Guid deliveryId,
    NotificationStore store, CancellationToken ct) => Results.Ok(await store.AttemptsAsync(id, deliveryId, ct)));
app.MapGet("/campaigns/{id:guid}/audit", async (Guid id, int? page, int? pageSize, NotificationStore store, CancellationToken ct) =>
{
    if (!ValidPage(page, pageSize)) return Results.BadRequest(new { error = "page >= 1, pageSize từ 1 đến 100." });
    if (await store.GetAsync(id, ct) is null) return Results.NotFound();
    return Results.Ok(await store.AuditAsync(id, page ?? 1, pageSize ?? 20, ct));
});

await app.RunAsync();

static bool ValidPage(int? page, int? size) => (page ?? 1) >= 1 && (size ?? 20) is >= 1 and <= 100;
