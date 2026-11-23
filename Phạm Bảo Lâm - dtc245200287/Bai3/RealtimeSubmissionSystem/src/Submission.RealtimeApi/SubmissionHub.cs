using Microsoft.AspNetCore.SignalR;

namespace Submission.RealtimeApi;

public class SubmissionHub : Hub
{
    // Cho phép client tham gia nhóm theo mã bài tập (AssignmentId)
    public async Task JoinAssignmentGroup(int assignmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"assignment-{assignmentId}");
        await Clients.Caller.SendAsync("JoinedGroup", $"Đã gia nhập nhóm assignment-{assignmentId}");
    }
}