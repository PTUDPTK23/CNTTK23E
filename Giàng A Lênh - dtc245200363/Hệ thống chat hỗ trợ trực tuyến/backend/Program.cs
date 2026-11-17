using ChatSupport.Backend;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5251", "https://localhost:7141")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection. Hãy cấu hình backend/appsettings.Development.local.json.");

builder.Services.AddDbContext<ChatSupportDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<AdminAccountService>();
builder.Services.AddSingleton<AdminSessionService>();

var app = builder.Build();
app.UseCors("Frontend");
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/admin"))
    {
        var sessions = context.RequestServices.GetRequiredService<AdminSessionService>();
        var header = context.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : null;
        if (!sessions.IsValid(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Phiên Admin không hợp lệ hoặc đã hết hạn." });
            return;
        }
    }

    await next();
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatSupportDbContext>();
    await DatabaseInitializer.InitializeAsync(db, app.Environment.ContentRootPath);
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "ChatSupport.Backend",
    message = "API hệ thống chat hỗ trợ trực tuyến đang hoạt động."
}));

app.MapPost("/api/auth/login", async (LoginRequest request, AdminAccountService accounts, AdminSessionService sessions, ChatSupportDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu." });

    var account = await accounts.AuthenticateAsync(db, request.Username, request.Password);
    if (account is null)
        return Results.Json(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." }, statusCode: StatusCodes.Status401Unauthorized);

    return Results.Ok(new LoginResponse("Đăng nhập thành công.", account.DisplayName, account.Username, account.Role, sessions.Create()));
});

app.MapPost("/api/auth/logout", (HttpContext context, AdminSessionService sessions) =>
{
    var header = context.Request.Headers.Authorization.ToString();
    sessions.Revoke(header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : null);
    return Results.Ok(new { message = "Đã đăng xuất." });
});

app.MapGet("/api/admin/summary", async (ChatSupportDbContext db) => Results.Ok(new AdminSummary(
    await db.Customers.CountAsync(),
    await db.StaffMembers.CountAsync(),
    await db.StaffMembers.CountAsync(item => item.Status == "Đang hoạt động"),
    await db.Conversations.CountAsync(item => item.Status == "Đang xử lý"),
    await db.Messages.CountAsync())));

app.MapGet("/api/admin/customers", async (ChatSupportDbContext db) => Results.Ok(await db.Customers.OrderByDescending(item => item.CreatedAt).ToListAsync()));
app.MapPost("/api/admin/customers", async (CustomerInput input, ChatSupportDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Email))
        return Results.BadRequest(new { message = "Cần nhập họ tên và email khách hàng." });
    var customer = new Customer { Id = Guid.NewGuid(), FullName = input.FullName.Trim(), Email = input.Email.Trim(), Phone = input.Phone?.Trim() ?? "", CreatedAt = DateTimeOffset.UtcNow, IsActive = true };
    db.Customers.Add(customer);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/customers/{customer.Id}", customer);
});
app.MapPut("/api/admin/customers/{id:guid}", async (Guid id, CustomerInput input, ChatSupportDbContext db) =>
{
    var customer = await db.Customers.FindAsync(id);
    if (customer is null) return Results.NotFound();
    customer.FullName = input.FullName.Trim();
    customer.Email = input.Email.Trim();
    customer.Phone = input.Phone?.Trim() ?? "";
    customer.IsActive = input.IsActive;
    await db.SaveChangesAsync();
    return Results.Ok(customer);
});
app.MapDelete("/api/admin/customers/{id:guid}", async (Guid id, ChatSupportDbContext db) =>
{
    var customer = await db.Customers.FindAsync(id);
    if (customer is null) return Results.NotFound();
    db.Customers.Remove(customer);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/admin/staff", async (ChatSupportDbContext db) => Results.Ok(await db.StaffMembers.OrderBy(item => item.FullName).ToListAsync()));
app.MapPost("/api/admin/staff", async (StaffInput input, ChatSupportDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Email))
        return Results.BadRequest(new { message = "Cần nhập họ tên và email nhân viên." });
    var staff = new StaffMember { Id = Guid.NewGuid(), FullName = input.FullName.Trim(), Email = input.Email.Trim(), Role = string.IsNullOrWhiteSpace(input.Role) ? "Nhân viên hỗ trợ" : input.Role.Trim(), Status = input.Status };
    db.StaffMembers.Add(staff);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/staff/{staff.Id}", staff);
});
app.MapPut("/api/admin/staff/{id:guid}", async (Guid id, StaffInput input, ChatSupportDbContext db) =>
{
    var staff = await db.StaffMembers.FindAsync(id);
    if (staff is null) return Results.NotFound();
    staff.FullName = input.FullName.Trim();
    staff.Email = input.Email.Trim();
    staff.Role = input.Role.Trim();
    staff.Status = input.Status;
    await db.SaveChangesAsync();
    return Results.Ok(staff);
});
app.MapDelete("/api/admin/staff/{id:guid}", async (Guid id, ChatSupportDbContext db) =>
{
    var staff = await db.StaffMembers.FindAsync(id);
    if (staff is null) return Results.NotFound();
    db.StaffMembers.Remove(staff);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/admin/conversations", async (ChatSupportDbContext db) => Results.Ok(await db.Conversations.OrderByDescending(item => item.UpdatedAt).ToListAsync()));
app.MapGet("/api/admin/messages", async (ChatSupportDbContext db) => Results.Ok(await db.Messages.OrderByDescending(item => item.SentAt).ToListAsync()));
app.MapGet("/api/admin/performance", async (ChatSupportDbContext db) =>
{
    var staff = await db.StaffMembers.ToListAsync();
    var conversations = await db.Conversations.ToListAsync();
    var messages = await db.Messages.ToListAsync();
    return Results.Ok(staff.Select(item => new StaffPerformance(
        item.Id, item.FullName, item.Role,
        conversations.Count(conversation => conversation.AssignedStaffId == item.Id),
        messages.Count(message => message.SenderId == item.Id), item.Status)));
});
app.MapGet("/api/admin/settings", async (ChatSupportDbContext db) => Results.Ok(await db.SystemSettings.FirstAsync()));
app.MapPut("/api/admin/settings", async (SystemSettingsInput input, ChatSupportDbContext db) =>
{
    var settings = await db.SystemSettings.FirstAsync();
    settings.SystemName = input.SystemName.Trim();
    settings.SupportEmail = input.SupportEmail.Trim();
    settings.AutoReplyEnabled = input.AutoReplyEnabled;
    settings.WelcomeMessage = input.WelcomeMessage.Trim();
    await db.SaveChangesAsync();
    return Results.Ok(settings);
});

app.Run();

internal sealed record LoginRequest(string Username, string Password, bool RememberMe = false);
internal sealed record LoginResponse(string Message, string DisplayName, string Username, string Role, string AccessToken);
internal sealed record SystemSettingsInput(string SystemName, string SupportEmail, bool AutoReplyEnabled, string WelcomeMessage);
