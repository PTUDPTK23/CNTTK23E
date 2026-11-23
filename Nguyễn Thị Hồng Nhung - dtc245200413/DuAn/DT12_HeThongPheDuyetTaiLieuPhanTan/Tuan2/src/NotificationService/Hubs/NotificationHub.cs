using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinRecipientGroup(string recipient)
    {
        if (!string.IsNullOrWhiteSpace(recipient))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, recipient.Trim().ToLowerInvariant());
        }
    }
}
