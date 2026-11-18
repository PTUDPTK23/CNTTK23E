using Microsoft.AspNetCore.SignalR;

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
            throw new HubException("Ma luot nop khong hop le.");
        var group = SubmissionGroups.ForSubmission(id);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id:D}";
}

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
