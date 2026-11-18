using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "Support token", Description = "Nhập token nhận được từ POST /auth/login."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddDbContext<TicketDb>(o => o.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Database=supportdb;Username=supportuser;Password=local_demo_password_change"));
builder.Services.AddSingleton<RabbitPublisher>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:3000")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors("Frontend");
app.UseSwagger();
app.UseSwaggerUI();

var signingKey = Environment.GetEnvironmentVariable("AUTH_SIGNING_KEY") ?? "local-only-signing-key-change-before-deployment-32chars";
app.Use(async (context, next) =>
{
    var auth = context.Request.Headers.Authorization.ToString();
    if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
        TryReadToken(auth[7..], signingKey, out var user, out var role))
    {
        context.Items["user"] = user;
        context.Items["role"] = role;
    }
    await next();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketDb>();
    var retries = 20;
    while (retries-- > 0)
    {
        try { await db.Database.EnsureCreatedAsync(); break; }
        catch when (retries > 0) { await Task.Delay(2000); }
    }
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "OutboxMessages" (
            "Id" uuid PRIMARY KEY,
            "RoutingKey" text NOT NULL,
            "Payload" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "PublishedAt" timestamp with time zone NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_PublishedAt_CreatedAt"
            ON "OutboxMessages" ("PublishedAt", "CreatedAt");
        """);
}

app.MapPost("/auth/login", (LoginRequest input) =>
{
    var accounts = new[]
    {
        (User: Environment.GetEnvironmentVariable("SUPPORT_REQUESTER_USER") ?? "customer", Password: Environment.GetEnvironmentVariable("SUPPORT_REQUESTER_PASSWORD") ?? "customer-demo-change", Role: "Requester"),
        (User: Environment.GetEnvironmentVariable("SUPPORT_AGENT_USER") ?? "agent", Password: Environment.GetEnvironmentVariable("SUPPORT_AGENT_PASSWORD") ?? "agent-demo-change", Role: "Agent"),
        (User: Environment.GetEnvironmentVariable("SUPPORT_ADMIN_USER") ?? "admin", Password: Environment.GetEnvironmentVariable("SUPPORT_ADMIN_PASSWORD") ?? "admin-demo-change", Role: "Admin")
    };
    var account = accounts.FirstOrDefault(x => FixedEquals(x.User, input.Username) && FixedEquals(x.Password, input.Password));
    return account.User is null ? Results.Unauthorized() : Results.Ok(new { token = CreateToken(account.User, account.Role, signingKey), username = account.User, role = account.Role });
}).WithTags("Authentication");

app.MapPost("/tickets", async (HttpContext ctx, CreateTicket input, TicketDb db) =>
{
    var user = GetUser(ctx); var role = GetRole(ctx);
    if (user is null) return Results.Unauthorized();
    if (role is not ("Requester" or "Agent" or "Admin")) return Results.Forbid();
    if (role == "Requester" && !string.IsNullOrWhiteSpace(input.RequesterId) && !string.Equals(input.RequesterId.Trim(), user, StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
    var requesterId = role == "Requester" ? user : input.RequesterId?.Trim();
    if (string.IsNullOrWhiteSpace(requesterId) || requesterId.Length > 120) return Results.BadRequest("RequesterId is required and must be at most 120 characters.");
    if (string.IsNullOrWhiteSpace(input.Content) || input.Content.Length > 5000) return Results.BadRequest("Content is required and must be at most 5000 characters.");
    var priority = string.IsNullOrWhiteSpace(input.Priority) ? "Normal" : input.Priority.Trim();
    if (priority is not ("Normal" or "High")) return Results.BadRequest("Priority must be Normal or High.");
    var ticket = new Ticket { RequesterId = requesterId, Content = input.Content.Trim(), Priority = priority, Status = "New", CreatedAt = DateTimeOffset.UtcNow };
    db.Tickets.Add(ticket);
    db.History.Add(new TicketHistory { TicketId = ticket.Id, ChangedBy = user, Change = "Created", NewValue = "New", At = DateTimeOffset.UtcNow });
    var createdAt = DateTimeOffset.UtcNow;
    db.OutboxMessages.Add(new OutboxMessage { RoutingKey = "ticket.created", Payload = JsonSerializer.Serialize(new TicketCreated(ticket.Id, ticket.RequesterId, ticket.Priority)), CreatedAt = createdAt });
    await db.SaveChangesAsync();
    return Results.Created($"/api/tickets/{ticket.Id}", ticket);
}).WithTags("Tickets");

app.MapGet("/tickets", async (HttpContext ctx, TicketDb db, string? search, string? status) =>
{
    if (GetUser(ctx) is null) return Results.Unauthorized();
    var query = db.Tickets.AsQueryable();
    var user = GetUser(ctx);
    if (GetRole(ctx) == "Requester") query = query.Where(t => t.RequesterId == user);
    if (!string.IsNullOrWhiteSpace(status))
    {
        var statuses = new[] { "New", "Assigned", "InProgress", "Resolved" };
        if (!statuses.Contains(status)) return Results.BadRequest("Status must be New, Assigned, InProgress, or Resolved.");
        query = query.Where(t => t.Status == status);
    }
    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        var lowered = term.ToLowerInvariant();
        if (Guid.TryParse(term, out var ticketId)) query = query.Where(t => t.Id == ticketId || t.RequesterId.ToLower().Contains(lowered) || t.Content.ToLower().Contains(lowered));
        else query = query.Where(t => t.RequesterId.ToLower().Contains(lowered) || t.Content.ToLower().Contains(lowered));
    }
    return Results.Ok(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
}).WithTags("Tickets");

app.MapGet("/tickets/{id:guid}", async (Guid id, HttpContext ctx, TicketDb db) =>
{
    if (GetUser(ctx) is null) return Results.Unauthorized();
    var t = await db.Tickets.FindAsync(id);
    if (t is null) return Results.NotFound();
    if (GetRole(ctx) == "Requester" && t.RequesterId != GetUser(ctx)) return Results.Forbid();
    return Results.Ok(t);
}).WithTags("Tickets");

app.MapPatch("/tickets/{id:guid}/status", async (Guid id, HttpContext ctx, UpdateStatus input, TicketDb db) =>
{
    if (GetUser(ctx) is null) return Results.Unauthorized();
    if (GetRole(ctx) == "Requester") return Results.Forbid();
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket is null) return Results.NotFound();
    var transitions = new Dictionary<string, string[]> { ["New"] = ["InProgress", "Resolved"], ["Assigned"] = ["InProgress", "Resolved"], ["InProgress"] = ["Resolved"], ["Resolved"] = [] };
    if (!transitions.TryGetValue(ticket.Status, out var allowed) || !allowed.Contains(input.Status)) return Results.BadRequest("Invalid status transition.");
    var old = ticket.Status; ticket.Status = input.Status; ticket.UpdatedAt = DateTimeOffset.UtcNow;
    var changedBy = GetUser(ctx)!;
    db.History.Add(new TicketHistory { TicketId = id, ChangedBy = changedBy, Change = "Status", OldValue = old, NewValue = input.Status, At = DateTimeOffset.UtcNow });
    db.OutboxMessages.Add(new OutboxMessage
    {
        RoutingKey = "ticket.status.changed",
        Payload = JsonSerializer.Serialize(new TicketChanged(ticket.Id, ticket.RequesterId, ticket.Status, ticket.AssignedGroup, ticket.AssignedAgent, changedBy, "Status")),
        CreatedAt = DateTimeOffset.UtcNow
    });
    await db.SaveChangesAsync();
    return Results.Ok(ticket);
}).WithTags("Tickets");

app.MapGet("/users/{userId}/tickets", async (string userId, HttpContext ctx, TicketDb db) =>
{
    if (GetUser(ctx) is null) return Results.Unauthorized();
    if (GetRole(ctx) == "Requester" && userId != GetUser(ctx)) return Results.Forbid();
    return Results.Ok(await db.Tickets.Where(t => t.RequesterId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync());
}).WithTags("Tickets");

app.MapGet("/tickets/{id:guid}/history", async (Guid id, HttpContext ctx, TicketDb db) =>
{
    if (GetUser(ctx) is null) return Results.Unauthorized();
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket is null) return Results.NotFound();
    if (GetRole(ctx) == "Requester" && ticket.RequesterId != GetUser(ctx)) return Results.Forbid();
    return Results.Ok(await db.History.Where(h => h.TicketId == id).OrderBy(h => h.At).ToListAsync());
}).WithTags("Tickets");

app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Health");
app.Run();

static string? GetUser(HttpContext ctx) => ctx.Items["user"] as string;
static string? GetRole(HttpContext ctx) => ctx.Items["role"] as string;
static bool FixedEquals(string expected, string? actual) => actual is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
static string CreateToken(string user, string role, string key)
{
    var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TokenPayload(user, role, DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds()))));
    var signature = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(payload)));
    return $"{payload}.{signature}";
}
static bool TryReadToken(string token, string key, out string user, out string role)
{
    user = role = "";
    try
    {
        var parts = token.Split('.'); if (parts.Length != 2) return false;
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(parts[0]));
        if (!CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(parts[1]))) return false;
        var payload = JsonSerializer.Deserialize<TokenPayload>(Convert.FromBase64String(parts[0]));
        if (payload is null || payload.Expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return false;
        user = payload.User; role = payload.Role; return true;
    }
    catch { return false; }
}

record LoginRequest(string Username, string Password);
record TokenPayload(string User, string Role, long Expires);
record CreateTicket(string? RequesterId, string Content, string? Priority);
record UpdateStatus(string Status);
record TicketCreated(Guid TicketId, string RequesterId, string Priority);
record TicketChanged(Guid TicketId, string RequesterId, string Status, string? AssignedGroup, string? AssignedAgent, string ChangedBy, string ChangeType);
class Ticket { public Guid Id { get; set; } = Guid.NewGuid(); public string RequesterId { get; set; } = ""; public string Content { get; set; } = ""; public string Priority { get; set; } = "Normal"; public string Status { get; set; } = "New"; public string? AssignedGroup { get; set; } public string? AssignedAgent { get; set; } public DateTimeOffset CreatedAt { get; set; } public DateTimeOffset? UpdatedAt { get; set; } }
class TicketHistory { public long Id { get; set; } public Guid TicketId { get; set; } public string ChangedBy { get; set; } = "system"; public string Change { get; set; } = ""; public string? OldValue { get; set; } public string? NewValue { get; set; } public DateTimeOffset At { get; set; } }
class TicketDb(DbContextOptions<TicketDb> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>(); public DbSet<TicketHistory> History => Set<TicketHistory>(); public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder b) { b.Entity<Ticket>().HasKey(x => x.Id); b.Entity<TicketHistory>().HasKey(x => x.Id); b.Entity<TicketHistory>().HasIndex(x => x.TicketId); b.Entity<OutboxMessage>().HasKey(x => x.Id); b.Entity<OutboxMessage>().HasIndex(x => new { x.PublishedAt, x.CreatedAt }); }
}
class OutboxMessage { public Guid Id { get; set; } = Guid.NewGuid(); public string RoutingKey { get; set; } = ""; public string Payload { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } public DateTimeOffset? PublishedAt { get; set; } }
class OutboxPublisher(ILogger<OutboxPublisher> log, IServiceScopeFactory scopes, RabbitPublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var foundMessages = false;
            var retrying = false;
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TicketDb>();
                var pending = await db.OutboxMessages.Where(m => m.PublishedAt == null).OrderBy(m => m.CreatedAt).Take(25).ToListAsync(stoppingToken);
                foundMessages = pending.Count > 0;
                foreach (var message in pending)
                {
                    try
                    {
                        await publisher.PublishSerializedAsync(message.RoutingKey, message.Payload);
                        message.PublishedAt = DateTimeOffset.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex) { retrying = true; log.LogWarning(ex, "Outbox publish failed; will retry message {MessageId}", message.Id); break; }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogWarning(ex, "Outbox polling failed; retrying"); }
            await Task.Delay(retrying ? TimeSpan.FromSeconds(5) : foundMessages ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
class RabbitPublisher : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection; private IChannel? _channel;
    private async Task Ensure()
    {
        if (_channel is not null && _channel.IsOpen) return;
        await ResetAsync();
        var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost", UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest", Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest" };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        await _channel.ExchangeDeclareAsync("support.events", ExchangeType.Topic, durable: true);
    }
    public async Task PublishSerializedAsync(string key, string json)
    {
        await _gate.WaitAsync();
        try { await Ensure(); await _channel!.BasicPublishAsync("support.events", key, Encoding.UTF8.GetBytes(json)); }
        catch { await ResetAsync(); throw; }
        finally { _gate.Release(); }
    }
    private async Task ResetAsync()
    {
        var channel = _channel; var connection = _connection; _channel = null; _connection = null;
        if (channel is not null) try { await channel.DisposeAsync(); } catch { }
        if (connection is not null) try { await connection.DisposeAsync(); } catch { }
    }
    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try { await ResetAsync(); }
        finally { _gate.Release(); _gate.Dispose(); }
    }
}
class NotificationsHub : Microsoft.AspNetCore.SignalR.Hub { }
