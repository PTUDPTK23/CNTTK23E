using Microsoft.AspNetCore.SignalR;

sealed class SubmissionHub(ILogger<SubmissionHub> logger) : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id) || id == Guid.Empty)
            throw new HubException("Ma luot nop khong hop le.");

        var group = GroupName(id);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        logger.LogInformation("[Hub] {ConnectionId} theo doi {Group}", Context.ConnectionId, group);
    }

    internal static string GroupName(Guid id) => $"submission:{id:D}";
}
