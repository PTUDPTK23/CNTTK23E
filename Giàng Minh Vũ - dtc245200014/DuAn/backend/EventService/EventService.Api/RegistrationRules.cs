public static class RegistrationRules
{
    public static bool HasActiveStatus(string status) => status is "Registered" or "CheckedIn";

    public static string? CheckRegistration(EventEntity e, bool duplicate, DateTimeOffset now)
    {
        if (e.Status != "Open" || e.StartTime <= now) return "Registration is closed.";
        if (duplicate) return "This email already has an active registration.";
        if (e.RegisteredCount >= e.Capacity) return "Event capacity has been reached.";
        return null;
    }

    public static string? CheckCancellation(string status) => status switch
    {
        "CheckedIn" => "A checked-in registration cannot be cancelled.",
        "Cancelled" => "Registration is already cancelled.",
        _ => null
    };

    public static string? CheckCheckIn(string status) => status switch
    {
        "Cancelled" => "Cancelled registration cannot check in.",
        "CheckedIn" => "Registration has already checked in.",
        _ => null
    };
}
