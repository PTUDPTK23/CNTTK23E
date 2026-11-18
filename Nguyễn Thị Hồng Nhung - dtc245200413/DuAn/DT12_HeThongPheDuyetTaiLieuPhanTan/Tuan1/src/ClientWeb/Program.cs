using System.Text;
using ClientWeb.Services;

var builder = WebApplication.CreateBuilder(args);

var documentApiUrl = (builder.Configuration["Services:DocumentApiUrl"] ?? "http://localhost:5001").TrimEnd('/');
var notificationServiceUrl = (builder.Configuration["Services:NotificationServiceUrl"] ?? "http://localhost:5002").TrimEnd('/');

builder.Services.AddHttpClient("DocumentApi", client =>
{
    client.BaseAddress = new Uri(documentApiUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("NotificationService", client =>
{
    client.BaseAddress = new Uri(notificationServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<NotificationListenerWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NotificationListenerWorker>());
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    service = "ClientWeb",
    status = "Healthy",
    documentApiUrl,
    notificationServiceUrl,
    timestamp = DateTime.UtcNow
}));

app.MapGet("/api/live-events", (NotificationListenerWorker listener) =>
    Results.Ok(listener.GetRecentLiveEvents()));

static async Task ForwardRequestAsync(HttpContext ctx, HttpClient client, string targetPath)
{
    try
    {
        var method = new HttpMethod(ctx.Request.Method);
        using var reqMsg = new HttpRequestMessage(method, targetPath + ctx.Request.QueryString);

        var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N")[..12];
        reqMsg.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        if (ctx.Request.ContentLength > 0 || HttpMethods.IsPost(ctx.Request.Method) || HttpMethods.IsPut(ctx.Request.Method))
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                reqMsg.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
        }

        using var resp = await client.SendAsync(reqMsg);
        var respContent = await resp.Content.ReadAsStringAsync();

        ctx.Response.StatusCode = (int)resp.StatusCode;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        if (resp.Headers.TryGetValues("X-Correlation-Id", out var respCorr))
        {
            ctx.Response.Headers["X-Correlation-Id"] = respCorr.FirstOrDefault();
        }
        await ctx.Response.WriteAsync(respContent);
    }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = 503;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            message = $"Service tạm thời không khả dụng: {ex.Message}"
        }));
    }
}

// Proxy routes to DocumentApi via Docker service name
app.Map("/api/documents", async (HttpContext ctx, IHttpClientFactory factory) =>
    await ForwardRequestAsync(ctx, factory.CreateClient("DocumentApi"), "/documents"));

app.Map("/api/documents/{**rest}", async (HttpContext ctx, string rest, IHttpClientFactory factory) =>
    await ForwardRequestAsync(ctx, factory.CreateClient("DocumentApi"), $"/documents/{rest}"));

app.MapGet("/api/outbox/stats", async (HttpContext ctx, IHttpClientFactory factory) =>
    await ForwardRequestAsync(ctx, factory.CreateClient("DocumentApi"), "/outbox/stats"));

// Proxy routes to NotificationService via Docker service name
app.Map("/api/notifications", async (HttpContext ctx, IHttpClientFactory factory) =>
    await ForwardRequestAsync(ctx, factory.CreateClient("NotificationService"), "/notifications"));

app.Map("/api/notifications/{**rest}", async (HttpContext ctx, string rest, IHttpClientFactory factory) =>
    await ForwardRequestAsync(ctx, factory.CreateClient("NotificationService"), $"/notifications/{rest}"));

app.Run();
