using Marten.Events.Projections;
using TimeTrackingDemo.ApiService.Domain;
using TimeTrackingDemo.ApiService.Shared;

namespace TimeTrackingDemo.ApiService.Infrastructure;

public class MonthlyHoursProjection : MultiStreamProjection<WorkerMonthlyStats, string>
{
    public MonthlyHoursProjection()
    {
        Identity<ShiftStarted>(e => $"{e.WorkerId}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Year}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Month}");
        Identity<BreakStarted>(e => $"{e.WorkerId}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Year}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Month}");
        Identity<BreakEnded>(e => $"{e.WorkerId}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Year}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Month}");
        Identity<ShiftEnded>(e => $"{e.WorkerId}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Year}-{GermanShiftCalendar.GetShiftMonth(e.Timestamp).Month}");
    }

    public void Apply(ShiftStarted e, WorkerMonthlyStats current)
    {
        var (year, month) = GermanShiftCalendar.GetShiftMonth(e.Timestamp);
        current.WorkerId = e.WorkerId;
        current.Year = year;
        current.Month = month;
        current.LastWorkStartTime = e.Timestamp;
    }

    public void Apply(BreakStarted e, WorkerMonthlyStats current)
    {
        if (!current.LastWorkStartTime.HasValue) return;
        current.TotalMinutesWorked += (uint)(e.Timestamp - current.LastWorkStartTime.Value).TotalMinutes;
        current.LastWorkStartTime = null;
    }

    public void Apply(BreakEnded e, WorkerMonthlyStats current)
    {
        current.LastWorkStartTime = e.Timestamp;
    }

    public void Apply(ShiftEnded e, WorkerMonthlyStats current)
    {
        if (!current.LastWorkStartTime.HasValue) return;
        current.TotalMinutesWorked += (uint)(e.Timestamp - current.LastWorkStartTime.Value).TotalMinutes;
        current.LastWorkStartTime = null;
    }
}