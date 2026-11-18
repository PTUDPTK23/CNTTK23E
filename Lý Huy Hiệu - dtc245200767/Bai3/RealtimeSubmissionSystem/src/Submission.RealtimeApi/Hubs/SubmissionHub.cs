
using Microsoft.AspNetCore.SignalR;

public class SubmissionHub : Hub
{
    public async Task WatchSubmission(string id)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"submission:{id}"
        );

        await Clients.Caller.SendAsync(
            "WatchConfirmed", id
        );
    }

    public async Task UnwatchSubmission(string id)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"submission:{id}"
        );
    }
}
