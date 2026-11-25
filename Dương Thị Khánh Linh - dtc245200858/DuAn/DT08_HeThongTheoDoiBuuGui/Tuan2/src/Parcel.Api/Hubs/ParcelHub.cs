
using Microsoft.AspNetCore.SignalR;

public class ParcelHub : Hub
{
    // Khach hang dang ky theo doi mot buu gui
    public async Task WatchParcel(string trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
            return;

        string groupName = $"parcel:{trackingCode.Trim().ToUpperInvariant()}";

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName
        );
    }

    // Khach hang ngung theo doi buu gui
    public async Task StopWatchingParcel(string trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
            return;

        string groupName = $"parcel:{trackingCode.Trim().ToUpperInvariant()}";

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            groupName
        );
    }
}
