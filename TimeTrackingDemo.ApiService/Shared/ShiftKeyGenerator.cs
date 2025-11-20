namespace TimeTrackingDemo.ApiService.Shared;

public static class ShiftKeyGenerator
{
    public static string Generate(uint workerId, DateTimeOffset timestamp)
    {
        var shiftDate = GermanShiftCalendar.GetShiftDate(timestamp);
        return $"{workerId}_{shiftDate:yyyy-MM-dd}";
    }
}