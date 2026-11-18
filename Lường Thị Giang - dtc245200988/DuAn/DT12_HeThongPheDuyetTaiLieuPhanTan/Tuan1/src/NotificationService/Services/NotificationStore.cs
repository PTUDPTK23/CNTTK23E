using System.Collections.Concurrent;

namespace NotificationService.Services;

public record UserNotification(
    Guid Id,
    Guid EventId,
    Guid DocumentId,
    string DocumentTitle,
    string Creator,
    string Approver,
    string NewStatus,
    string ActionBy,
    string Headline,
    string DetailMessage,
    string CorrelationId,
    DateTime CreatedAtUtc
);

public class NotificationStore
{
    private readonly ConcurrentQueue<UserNotification> _notifications = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _processedEventIds = new();

    public bool IsAlreadyProcessed(Guid eventId) => _processedEventIds.ContainsKey(eventId);

    public bool TryMarkProcessed(Guid eventId) => _processedEventIds.TryAdd(eventId, DateTime.UtcNow);

    public void AddNotification(UserNotification notification)
    {
        _notifications.Enqueue(notification);
        while (_notifications.Count > 100)
        {
            _notifications.TryDequeue(out _);
        }
    }

    public IReadOnlyList<UserNotification> GetRecent(int take = 50)
    {
        return _notifications
            .Reverse()
            .Take(take)
            .ToList();
    }

    public int ProcessedCount => _processedEventIds.Count;
}
