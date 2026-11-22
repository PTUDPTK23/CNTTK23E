using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"[Realtime] {message.SubmissionId} - {message.Status} - {message.UpdatedAt}");
    });

await connection.StartAsync();

Console.WriteLine("Da ket noi den SubmissionHub.");

Console.Write("Nhap SubmissionId can theo doi: ");
var submissionId = Console.ReadLine();

if (!string.IsNullOrWhiteSpace(submissionId))
{
    await Task.Delay(TimeSpan.FromSeconds(4));
    await connection.InvokeAsync("WatchSubmission", submissionId);
    Console.WriteLine($"Dang theo doi: {submissionId}");
}

Console.WriteLine("Nhan Enter de thoat.");
Console.ReadLine();

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);