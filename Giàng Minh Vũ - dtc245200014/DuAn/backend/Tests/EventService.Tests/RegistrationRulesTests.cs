public class RegistrationRulesTests
{
    private static EventEntity OpenEvent(int capacity = 5, int registered = 0) => new()
    {
        Status = "Open",
        StartTime = DateTimeOffset.UtcNow.AddDays(1),
        Capacity = capacity,
        RegisteredCount = registered
    };

    [Fact]
    public void AllowsRegistrationWhenEventHasSpaceAndEmailIsNew()
    {
        Assert.Null(RegistrationRules.CheckRegistration(OpenEvent(), false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RejectsRegistrationAtCapacity()
    {
        Assert.Equal("Event capacity has been reached.", RegistrationRules.CheckRegistration(OpenEvent(2, 2), false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RejectsDuplicateActiveRegistration()
    {
        Assert.Equal("This email already has an active registration.", RegistrationRules.CheckRegistration(OpenEvent(), true, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RejectsClosedOrStartedEvents()
    {
        var closed = OpenEvent();
        closed.Status = "Closed";
        var started = OpenEvent();
        started.StartTime = DateTimeOffset.UtcNow;
        Assert.Equal("Registration is closed.", RegistrationRules.CheckRegistration(closed, false, DateTimeOffset.UtcNow));
        Assert.Equal("Registration is closed.", RegistrationRules.CheckRegistration(started, false, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("CheckedIn", "A checked-in registration cannot be cancelled.")]
    [InlineData("Cancelled", "Registration is already cancelled.")]
    [InlineData("Registered", null)]
    public void CancellationRulesMatchRegistrationState(string status, string? expected)
    {
        Assert.Equal(expected, RegistrationRules.CheckCancellation(status));
    }

    [Theory]
    [InlineData("Cancelled", "Cancelled registration cannot check in.")]
    [InlineData("CheckedIn", "Registration has already checked in.")]
    [InlineData("Registered", null)]
    public void CheckInRulesRejectCancelledAndRepeatedCheckIns(string status, string? expected)
    {
        Assert.Equal(expected, RegistrationRules.CheckCheckIn(status));
    }
}
