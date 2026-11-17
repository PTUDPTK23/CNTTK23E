using MediQueue.Domain.Common;
using MediQueue.Domain.Scheduling;

namespace MediQueue.UnitTests.Scheduling;

public sealed class TimeSlotTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 20, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_rejects_end_not_after_start()
    {
        Assert.Throws<DomainRuleException>(() => Create(Start, Start, 1));
    }

    [Fact]
    public void Constructor_rejects_non_positive_capacity()
    {
        Assert.Throws<DomainRuleException>(() => Create(Start, Start.AddMinutes(30), 0));
    }

    [Fact]
    public void Reserve_place_never_exceeds_capacity()
    {
        TimeSlot slot = Create(Start, Start.AddMinutes(30), 1);

        slot.ReservePlace();

        Assert.Equal(1, slot.BookedCount);
        Assert.Throws<DomainRuleException>(slot.ReservePlace);
    }

    [Fact]
    public void Release_place_never_makes_booked_count_negative()
    {
        TimeSlot slot = Create(Start, Start.AddMinutes(30), 1);

        Assert.Throws<DomainRuleException>(slot.ReleasePlace);
    }

    private static TimeSlot Create(DateTimeOffset start, DateTimeOffset end, int capacity) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, end, capacity);
}
