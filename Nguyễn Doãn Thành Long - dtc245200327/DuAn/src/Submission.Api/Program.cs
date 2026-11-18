using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5433;Database=autograding_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbContext.SeedAsync(db);
}

// 1. GET /assignments : Lấy danh sách bài tập
app.MapGet("/assignments", async (AppDbContext db) =>
{
    var assignments = await db.Assignments
        .OrderBy(a => a.Id)
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(assignments);
});

// 2. POST /assignments/{id}/submissions : Nộp bài cho bài tập có mã {id}, lưu trạng thái Submitted và đẩy vào RabbitMQ
app.MapPost("/assignments/{id:int}/submissions", async (
    int id,
    CreateSubmissionRequest request,
    AppDbContext db,
    IConfiguration config) =>
{
    var content = !string.IsNullOrWhiteSpace(request.Content)
        ? request.Content.Trim()
        : request.RepositoryUrl?.Trim();

    if (string.IsNullOrWhiteSpace(request.StudentId) || string.IsNullOrWhiteSpace(content))
    {
        return Results.BadRequest(new { error = "StudentId và Content không được để trống." });
    }

    var assignmentExists = await db.Assignments.AnyAsync(a => a.Id == id);
    if (!assignmentExists)
    {
        return Results.NotFound(new { error = $"Không tìm thấy bài tập có Id = {id}." });
    }

    var studentId = request.StudentId.Trim();
    var studentExists = await db.Students.AnyAsync(s => s.Id == studentId);
    if (!studentExists)
    {
        return Results.NotFound(new { error = $"Không tìm thấy sinh viên có Id = {studentId}." });
    }

    // Bước 3: Tạo submission với trạng thái Submitted
    var submission = new Submission
    {
        AssignmentId = id,
        StudentId = studentId,
        Content = content,
        SubmittedAt = DateTimeOffset.UtcNow,
        Status = "Submitted",
        Score = null,
        Comment = null
    };

    db.Submissions.Add(submission);
    await db.SaveChangesAsync();

    // Bước 4: Đưa yêu cầu chấm vào RabbitMQ
    var message = new SubmissionGradingMessage(
        submission.Id,
        submission.AssignmentId,
        submission.StudentId,
        submission.Content,
        submission.SubmittedAt);

    await PublishToRabbitMqAsync(config, message);

    Console.WriteLine($"[Submission.Api] Đã lưu Submission #{submission.Id} (Status={submission.Status}) và gửi vào RabbitMQ queue '{RabbitMqQueues.SubmissionGrading}'.");

    return Results.Created($"/submissions/{submission.Id}", submission);
});

// 3. GET /submissions/{id} : Tra cứu chi tiết một bài nộp (bao gồm trạng thái, điểm, nhận xét)
app.MapGet("/submissions/{id:int}", async (int id, AppDbContext db) =>
{
    var submission = await db.Submissions
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.Id == id);

    return submission is null
        ? Results.NotFound(new { error = $"Không tìm thấy bài nộp có Id = {id}." })
        : Results.Ok(submission);
});

// 4. GET /students/{studentId}/submissions : Tra cứu danh sách các bài đã nộp của sinh viên {studentId}
app.MapGet("/students/{studentId}/submissions", async (string studentId, AppDbContext db) =>
{
    var submissions = await db.Submissions
        .AsNoTracking()
        .Where(s => s.StudentId == studentId)
        .OrderByDescending(s => s.SubmittedAt)
        .ToListAsync();

    return Results.Ok(submissions);
});

app.Run();

static async Task PublishToRabbitMqAsync(IConfiguration config, SubmissionGradingMessage message)
{
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMQ:HostName"] ?? "localhost",
        Port = int.TryParse(config["RabbitMQ:Port"], out var p) ? p : 5672,
        UserName = config["RabbitMQ:UserName"] ?? "guest",
        Password = config["RabbitMQ:Password"] ?? "guest"
    };

    await using var connection = await factory.CreateConnectionAsync();
    await using var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync(
        queue: RabbitMqQueues.SubmissionGrading,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var json = JsonSerializer.Serialize(message);
    var body = Encoding.UTF8.GetBytes(json);

    var props = new BasicProperties { Persistent = true };

    await channel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: RabbitMqQueues.SubmissionGrading,
        mandatory: false,
        basicProperties: props,
        body: body);
}

public record CreateSubmissionRequest(string StudentId, string? Content, string? RepositoryUrl = null);
