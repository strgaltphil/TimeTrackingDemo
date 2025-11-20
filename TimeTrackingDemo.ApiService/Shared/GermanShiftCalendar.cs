namespace TimeTrackingDemo.ApiService.Shared;

public static class GermanShiftCalendar
{
    private static readonly TimeZoneInfo GermanZone;

    static GermanShiftCalendar()
    {
        try { GermanZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"); }
        catch { GermanZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }
    }

    public static DateOnly GetShiftDate(DateTimeOffset utcTime)
    {
        var germanTime = TimeZoneInfo.ConvertTime(utcTime, GermanZone);
        var adjustedTime = germanTime.AddHours(-3); 
        return DateOnly.FromDateTime(adjustedTime.Date);
    }

    public static (int Year, int Month) GetShiftMonth(DateTimeOffset utcTime)
    {
        var date = GetShiftDate(utcTime);
        return (date.Year, date.Month);
    }
}