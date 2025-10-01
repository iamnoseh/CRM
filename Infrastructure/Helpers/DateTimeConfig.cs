namespace Infrastructure.Helpers;

public static class DateTimeConfig
{
    private static readonly TimeZoneInfo DushanbeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dushanbe");

    public static DateTimeOffset ToUtc(this DateTimeOffset localTime)
    {
        return localTime.ToUniversalTime();
    }

    public static DateTimeOffset ToUtc(this DateTime localTime)
    {
        if (localTime.Kind == DateTimeKind.Utc)
            return new DateTimeOffset(localTime);

        var localTimeOffset = TimeZoneInfo.ConvertTimeToUtc(localTime, DushanbeTimeZone);
        return new DateTimeOffset(localTimeOffset);
    }

    public static DateTimeOffset ToDushanbeTime(this DateTimeOffset utcTime)
    {
        return TimeZoneInfo.ConvertTime(utcTime, DushanbeTimeZone);
    }

    public static DateTime ToDushanbeTime(this DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTime(utcTime, DushanbeTimeZone);
    }

    public static DateTime NowDushanbe()
    {
        return TimeZoneInfo.ConvertTime(DateTime.UtcNow, DushanbeTimeZone);
    }
}
