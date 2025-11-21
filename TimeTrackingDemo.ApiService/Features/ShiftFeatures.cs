using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using TimeTrackingDemo.ApiService.Domain;
using TimeTrackingDemo.ApiService.Infrastructure;
using TimeTrackingDemo.ApiService.Shared;

namespace TimeTrackingDemo.ApiService.Features;

public record WorkerRequest(uint WorkerId, DateTimeOffset Timestamp) : IEvent;

public record WorkingTimeResponse(string Id, string Status);

public static class ShiftEndpoints
{
    private static BadRequest<string>? ValidateYearAndMonth(int year, int month)
    {
        if (month is < 1 or > 12)
            return TypedResults.BadRequest("Month must be between 1 and 12.");

        if (year < 1900 || year > 2100)
            return TypedResults.BadRequest("Year must be between 1900 and 2100.");

        return null;
    }

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("time-tracking");

        group.MapPost("/shifts/start",
            async Task<Results<Ok<WorkingTimeResponse>, Conflict<string>, BadRequest<string>>> (WorkerRequest req,
                IDocumentSession session, CancellationToken ct) =>
            {
                var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);

                var existing = await session.Events.AggregateStreamAsync<WorkShift>(streamKey, token: ct);
                if (existing != null) return TypedResults.Conflict("Shift already started for this workday.");

                try
                {
                    var shift = new WorkShift();
                    shift.StartShift(streamKey, req.WorkerId, req.Timestamp);
                    session.Events.StartStream<WorkShift>(streamKey, shift.GetUncommittedEvents());
                    await session.SaveChangesAsync(ct);
                    return TypedResults.Ok(new WorkingTimeResponse(streamKey, "Started"));
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            });

        group.MapPost("/shifts/end",
            async Task<Results<Ok<WorkingTimeResponse>, NotFound<string>, BadRequest<string>>> (WorkerRequest req,
                IDocumentSession session, CancellationToken ct) =>
            {
                var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);

                var shift = await session.Events.AggregateStreamAsync<WorkShift>(streamKey, token: ct);
                if (shift == null) return TypedResults.NotFound("Shift not found.");

                try
                {
                    shift.EndShift(req.Timestamp);
                    session.Events.Append(streamKey, shift.GetUncommittedEvents());
                    await session.SaveChangesAsync(ct);
                    return TypedResults.Ok(new WorkingTimeResponse(streamKey, "Ended"));
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            });

        group.MapPost("/breaks/start",
            async Task<Results<Ok<string>, NotFound<string>, BadRequest<string>>> (WorkerRequest req,
                IDocumentSession session, CancellationToken ct) =>
            {
                var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);

                var shift = await session.Events.AggregateStreamAsync<WorkShift>(streamKey, token: ct);
                if (shift == null) return TypedResults.NotFound("Shift not found.");

                try
                {
                    shift.StartBreak(req.Timestamp);
                    session.Events.Append(streamKey, shift.GetUncommittedEvents());
                    await session.SaveChangesAsync(ct);
                    return TypedResults.Ok("Break started");
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            });

        group.MapPost("/breaks/end",
            async Task<Results<Ok<string>, NotFound<string>, BadRequest<string>>> (WorkerRequest req,
                IDocumentSession session, CancellationToken ct) =>
            {
                var streamKey = ShiftKeyGenerator.Generate(req.WorkerId, req.Timestamp);

                var shift = await session.Events.AggregateStreamAsync<WorkShift>(streamKey, token: ct);
                if (shift == null) return TypedResults.NotFound("Shift not found.");

                try
                {
                    shift.StopBreak(req.Timestamp);

                    session.Events.Append(streamKey, shift.GetUncommittedEvents());
                    await session.SaveChangesAsync(ct);
                    return TypedResults.Ok("Break ended");
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            });

        group.MapGet("stats/{workerId}/{year:int}/{month:int}",
            async Task<Results<Ok<WorkerMonthlyStats>, NotFound<string>, BadRequest<string>>> (uint workerId, int year,
                int month, IQuerySession session, CancellationToken ct) =>
            {
                var validationResult = ValidateYearAndMonth(year, month);
                if (validationResult is not null)
                    return validationResult;

                var stats = await session.Query<WorkerMonthlyStats>()
                    .Where(x => x.WorkerId == workerId && x.Year == year && x.Month == month)
                    .FirstOrDefaultAsync(token: ct);

                return stats is not null ? TypedResults.Ok(stats) : TypedResults.NotFound("No data.");
            });

        group.MapGet("stats/{year:int}/{month:int}",
            async Task<Results<Ok<IReadOnlyList<WorkerMonthlyStats>>, BadRequest<string>>> (int year, int month,
                IQuerySession session, CancellationToken ct) =>
            {
                var validationResult = ValidateYearAndMonth(year, month);
                if (validationResult is not null)
                    return validationResult;

                var stats = await session.Query<WorkerMonthlyStats>()
                    .Where(x => x.Year == year && x.Month == month)
                    .ToListAsync(token: ct);

                return TypedResults.Ok(stats);
            });

        group.MapPost("maintenance/rebuild-projections", async (IDocumentStore store, CancellationToken ct) =>
        {
            using var daemon = await store.BuildProjectionDaemonAsync();

            await daemon.RebuildProjectionAsync<MonthlyHoursProjection>(ct);

            return TypedResults.Ok("Rebuild started.");
        });
    }
}