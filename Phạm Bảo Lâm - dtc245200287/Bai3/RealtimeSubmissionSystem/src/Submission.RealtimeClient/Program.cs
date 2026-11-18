using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/submission")
    .WithAutomaticReconnect()
    .Build();

// Lắng nghe sự kiện gia nhập nhóm
connection.On<string>("JoinedGroup", msg => Console.WriteLine($"[Hub] {msg}"));

// Lắng nghe sự kiện chấm bài (In ra từ khóa 'graded' theo chuẩn rubric)
connection.On<string>("SubmissionGraded", statusMsg => Console.WriteLine($"graded: {statusMsg}"));

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi den Submission.RealtimeApi.");

    // Gọi phương thức tham gia nhóm bài tập (AssignmentId = 1)
    await connection.InvokeAsync("JoinAssignmentGroup", 1);
}
catch (Exception ex)
{
    Console.WriteLine($"Loi ket noi: {ex.Message}");
}

Console.WriteLine("Dang cho su kien tu server... Nhan Enter de thoat.");
Console.ReadLine();