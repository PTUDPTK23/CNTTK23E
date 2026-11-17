using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddPolicy("FrontendDev", policy => policy
    .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:5501", "http://127.0.0.1:5501")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection trong appsettings.json.");

var dataSource = NpgsqlDataSource.Create(connectionString);
var app = builder.Build();
var frontendPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "frontend"));
app.UseCors("FrontendDev");

// Khởi tạo bảng khi chạy lần đầu. Chỉ Cha123 được giữ vai trò administrator.
await using (var connection = await dataSource.OpenConnectionAsync())
{
    await using (var command = new NpgsqlCommand("""
        CREATE TABLE IF NOT EXISTS app_users (
            username TEXT PRIMARY KEY,
            email TEXT UNIQUE,
            actor_id TEXT NOT NULL CHECK (actor_id IN ('administrator', 'monitoring_user', 'data_simulator')),
            password_salt TEXT NOT NULL,
            password_hash TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """, connection))
        await command.ExecuteNonQueryAsync();

    await using (var normalizeAdmins = new NpgsqlCommand(
        "UPDATE app_users SET actor_id = 'monitoring_user' WHERE actor_id = 'administrator' AND username <> 'Cha123';", connection))
        await normalizeAdmins.ExecuteNonQueryAsync();

    await using (var emailIndex = new NpgsqlCommand(
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_app_users_email_case_insensitive ON app_users (LOWER(email)) WHERE email IS NOT NULL;", connection))
        await emailIndex.ExecuteNonQueryAsync();

    var adminSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    var adminHash = HashPassword("Cha123456", adminSalt);
    await using (var seed = new NpgsqlCommand("""
        INSERT INTO app_users (username, email, actor_id, password_salt, password_hash)
        VALUES ('Cha123', NULL, 'administrator', $1, $2)
        ON CONFLICT (username) DO UPDATE SET
            email = NULL, actor_id = 'administrator', password_salt = EXCLUDED.password_salt,
            password_hash = EXCLUDED.password_hash;
        """, connection))
    {
        seed.Parameters.AddWithValue(adminSalt);
        seed.Parameters.AddWithValue(adminHash);
        await seed.ExecuteNonQueryAsync();
    }

    await using var index = new NpgsqlCommand("""
        CREATE UNIQUE INDEX IF NOT EXISTS ux_app_users_single_administrator
        ON app_users (actor_id) WHERE actor_id = 'administrator';
        """, connection);
    await index.ExecuteNonQueryAsync();
}

var actors = new[]
{
    new { id = "administrator", name = "Quản trị viên", type = "user" },
    new { id = "monitoring_user", name = "Người giám sát", type = "user" },
    new { id = "data_simulator", name = "Người mô phỏng dữ liệu", type = "user" }
};

app.MapGet("/", () => Results.File(Path.Combine(frontendPath, "index.html"), "text/html; charset=utf-8"));
app.MapGet("/styles.css", () => Results.File(Path.Combine(frontendPath, "styles.css"), "text/css; charset=utf-8"));
app.MapGet("/app.js", () => Results.File(Path.Combine(frontendPath, "app.js"), "text/javascript; charset=utf-8"));
app.MapGet("/api/actors", () => Results.Ok(new { actors }));

app.MapPost("/api/auth/login", async (LoginRequest request) =>
{
    var login = request.Email?.Trim() ?? ""; // Nhận tên đăng nhập hoặc email.
    if (login.Contains('@')) login = login.ToLowerInvariant();
    if (login.Length == 0 || request.Password is null)
        return Results.BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu." });

    await using var command = dataSource.CreateCommand("""
        SELECT username, email, actor_id, password_salt, password_hash
        FROM app_users WHERE username = $1 OR email = $1 LIMIT 1;
        """);
    command.Parameters.AddWithValue(login);
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.Json(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." }, statusCode: 401);

    var user = new User(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4));
    await reader.DisposeAsync();
    var actual = Convert.FromBase64String(HashPassword(request.Password, user.Salt));
    var expected = Convert.FromBase64String(user.PasswordHash);
    if (!CryptographicOperations.FixedTimeEquals(actual, expected))
        return Results.Json(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." }, statusCode: 401);

    var actorName = actors.First(actor => actor.id == user.ActorId).name;
    return Results.Ok(new { message = "Đăng nhập thành công!", user = new { username = user.Username, email = user.Email, actorId = user.ActorId, actor = actorName } });
});

app.MapPost("/api/auth/register", async (RegisterRequest request) =>
{
    var email = request.Email?.Trim().ToLowerInvariant() ?? "";
    if (!Regex.IsMatch(email, @"^\S+@\S+\.\S+$"))
        return Results.BadRequest(new { message = "Vui lòng nhập email hợp lệ." });
    if (request.Password is null || request.Password.Length < 8)
        return Results.BadRequest(new { message = "Mật khẩu cần có ít nhất 8 ký tự." });

    var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    await using var command = dataSource.CreateCommand("""
        INSERT INTO app_users (username, email, actor_id, password_salt, password_hash)
        VALUES ($1, $1, 'monitoring_user', $2, $3);
        """);
    command.Parameters.AddWithValue(email);
    command.Parameters.AddWithValue(salt);
    command.Parameters.AddWithValue(HashPassword(request.Password, salt));
    try
    {
        await command.ExecuteNonQueryAsync();
        return Results.Json(new { message = "Tạo tài khoản thành công. Bạn có thể đăng nhập." }, statusCode: 201);
    }
    catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.Json(new { message = "Email này đã được đăng ký." }, statusCode: 409);
    }
});

app.MapPost("/api/auth/forgot-password", (EmailRequest request) =>
{
    if (!Regex.IsMatch(request.Email?.Trim() ?? "", @"^\S+@\S+\.\S+$"))
        return Results.BadRequest(new { message = "Vui lòng nhập email hợp lệ." });
    return Results.Ok(new { message = "Nếu email đã đăng ký, hướng dẫn khôi phục sẽ được gửi đến hộp thư." });
});

app.Lifetime.ApplicationStopped.Register(dataSource.Dispose);
app.Run();

static string HashPassword(string password, string salt) => Convert.ToBase64String(
    Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), 120_000, HashAlgorithmName.SHA256, 32));

record User(string Username, string? Email, string ActorId, string Salt, string PasswordHash);
record LoginRequest(string? Email, string? Password);
record RegisterRequest(string? Email, string? Password);
record EmailRequest(string? Email);
