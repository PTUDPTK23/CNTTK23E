using MediQueue.Domain.Common;
using MediQueue.Domain.Queues;

namespace MediQueue.UnitTests.Queues;

public sealed class QueueInvariantTests
{
    private static readonly DateOnly ServiceDate = new(2026, 11, 20);

    [Fact]
    public void Queue_ticket_rejects_non_positive_number()
    {
        Assert.Throws<DomainRuleException>(() => CreateTicket(0));
    }

    [Fact]
    public void Queue_ticket_formats_number_with_at_least_three_digits()
    {
        QueueTicket ticket = CreateTicket(5);

        Assert.Equal("005", ticket.DisplayNumber);
    }

    [Fact]
    public void Queue_ticket_rejects_invalid_transition()
    {
        QueueTicket ticket = CreateTicket(1);

        Assert.Throws<DomainRuleException>(() => ticket.Complete(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Daily_queue_counter_only_increases()
    {
        var queue = new DailyQueue(Guid.NewGuid(), Guid.NewGuid(), ServiceDate);

        int first = queue.IssueNextNumber();
        int second = queue.IssueNextNumber();

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, queue.LastIssuedNumber);
    }

    private static QueueTicket CreateTicket(int queueNumber) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ServiceDate,
            Guid.NewGuid(),
            queueNumber,
            DateTimeOffset.UtcNow);
}
