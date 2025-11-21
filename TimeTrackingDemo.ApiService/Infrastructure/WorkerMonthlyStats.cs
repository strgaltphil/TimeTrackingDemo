namespace TimeTrackingDemo.ApiService.Infrastructure;

public class WorkerMonthlyStats
{
    public string Id { get; set; } = string.Empty; // "WorkerId-YYYY-MM"
    public uint WorkerId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public uint TotalMinutesWorked { get; set; }
    
    public DateTimeOffset? LastWorkStartTime { get; set; }
}