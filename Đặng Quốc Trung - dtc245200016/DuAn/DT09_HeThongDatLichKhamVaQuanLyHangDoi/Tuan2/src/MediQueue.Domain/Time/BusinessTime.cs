namespace MediQueue.Domain.Time;

public static class BusinessTime
{
    public const string IanaTimeZoneId = "Asia/Ho_Chi_Minh";

    public const string WindowsTimeZoneId = "SE Asia Standard Time";

    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static DateOnly GetVietnamServiceDate(DateTimeOffset instant)
    {
        DateTimeOffset vietnamTime = TimeZoneInfo.ConvertTime(instant, VietnamTimeZone);
        return DateOnly.FromDateTime(vietnamTime.DateTime);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (string id in new[] { IanaTimeZoneId, WindowsTimeZoneId })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        throw new TimeZoneNotFoundException("Vietnam time zone is not available on this operating system.");
    }
}
