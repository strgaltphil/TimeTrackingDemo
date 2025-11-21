using Marten;
using TimeTrackingDemo.ApiService.Domain;
using TimeTrackingDemo.ApiService.Infrastructure;
using TimeTrackingDemo.ApiService.Shared;

namespace TimeTrackingDemo.ApiService.Features;

public record WorkerRequest(uint WorkerId, DateTimeOffset Timestamp) : IEvent;

public static class ShiftEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("time-tracking");
        
        group.MapPost("/shifts/start", async (WorkerRequest req, IDocumentSession session) =>
        {
            var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);
            
            var existing = await session.Events.AggregateStreamAsync<WorkShift>(streamKey);
            if (existing != null) return Results.Conflict("Shift already started for this workday.");

            try
            {
                var shift = new WorkShift();
                shift.StartShift(streamKey, req.WorkerId, req.Timestamp);
                session.Events.StartStream<WorkShift>(streamKey, shift.GetUncommittedEvents());
                await session.SaveChangesAsync();
                return Results.Ok(new { Id = streamKey, Status = "Started" });
            }
            catch (Exception ex) { return Results.BadRequest(ex.Message); }
        });
        
        group.MapPost("/shifts/end", async (WorkerRequest req, IDocumentSession session) =>
        {
            var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);
            
            var shift = await session.Events.AggregateStreamAsync<WorkShift>(streamKey);
            if (shift == null) return Results.NotFound("Shift not found.");

            try
            {
                shift.EndShift(req.Timestamp);
                session.Events.Append(streamKey, shift.GetUncommittedEvents());
                await session.SaveChangesAsync();
                return Results.Ok(new { Id = streamKey, Status = "Ended" });
            }
            catch (Exception ex) { return Results.BadRequest(ex.Message); }
        });
        
        group.MapPost("/breaks/start", async (WorkerRequest req, IDocumentSession session) =>
        {
            var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);

            var shift = await session.Events.AggregateStreamAsync<WorkShift>(streamKey);
            if (shift == null) return Results.NotFound("Shift not found.");

            try
            {
                shift.StartBreak(req.Timestamp);
                session.Events.Append(streamKey, shift.GetUncommittedEvents());
                await session.SaveChangesAsync();
                return Results.Ok("Break started");
            }
            catch (Exception ex) { return Results.BadRequest(ex.Message); }
        });
        
        group.MapPost("/breaks/end", async (WorkerRequest req, IDocumentSession session) =>
        {
            var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);

            var shift = await session.Events.AggregateStreamAsync<WorkShift>(streamKey);
            if (shift == null) return Results.NotFound("Shift not found.");

            try
            {
                shift.StopBreak(req.Timestamp);

                session.Events.Append(streamKey, shift.GetUncommittedEvents());
                await session.SaveChangesAsync();
                return Results.Ok("Break ended");
            }
            catch (Exception ex) { return Results.BadRequest(ex.Message); }
        });
        
        group.MapGet("stats/{workerId}/{year}/{month}", async (uint workerId, int year, int month, IQuerySession session) =>
        {
            var stats = await session.Query<WorkerMonthlyStats>()
                .Where(x => x.WorkerId == workerId && x.Year == year && x.Month == month)
                .FirstOrDefaultAsync();

            return stats is not null ? Results.Ok(stats) : Results.NotFound("No data.");
        });
        
        group.MapGet("stats/{year}/{month}", async (int year, int month, IQuerySession session) =>
        {
            var stats = await session.Query<WorkerMonthlyStats>()
                .Where(x => x.Year == year && x.Month == month)
                .ToListAsync();

            return Results.Ok(stats);
        });
    }
}