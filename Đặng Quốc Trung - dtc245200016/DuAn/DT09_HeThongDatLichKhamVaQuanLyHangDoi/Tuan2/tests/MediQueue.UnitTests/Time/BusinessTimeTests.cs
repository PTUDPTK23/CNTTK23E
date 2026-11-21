using MediQueue.Domain.Time;

namespace MediQueue.UnitTests.Time;

public sealed class BusinessTimeTests
{
    [Fact]
    public void Service_date_uses_vietnam_time_instead_of_utc_date()
    {
        var instant = new DateTimeOffset(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);

        DateOnly serviceDate = BusinessTime.GetVietnamServiceDate(instant);

        Assert.Equal(new DateOnly(2026, 11, 18), serviceDate);
    }
}
