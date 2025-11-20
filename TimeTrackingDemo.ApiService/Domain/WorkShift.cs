namespace TimeTrackingDemo.ApiService.Domain;

public interface IEvent;

public abstract record WorkShiftEvent(
    string StreamId,
    uint WorkerId,
    DateTimeOffset Timestamp
) : IEvent;

public sealed record ShiftStarted(string StreamId, uint WorkerId, DateTimeOffset Timestamp)
    : WorkShiftEvent(StreamId, WorkerId, Timestamp);

public sealed record BreakStarted(string StreamId, uint WorkerId, DateTimeOffset Timestamp)
    : WorkShiftEvent(StreamId, WorkerId, Timestamp);

public sealed record BreakEnded(string StreamId, uint WorkerId, DateTimeOffset Timestamp)
    : WorkShiftEvent(StreamId, WorkerId, Timestamp);

public sealed record ShiftEnded(string StreamId, uint WorkerId, DateTimeOffset Timestamp)
    : WorkShiftEvent(StreamId, WorkerId, Timestamp);

public class WorkShift
{
    public string Id { get; private set; } = string.Empty;
    public uint WorkerId { get; private set; }

    private readonly List<WorkShiftEvent> _uncommittedEvents = [];
    
    private enum ShiftState { NotStarted, Working, OnBreak, Finished }
    private ShiftState _status = ShiftState.NotStarted;
    
    public IReadOnlyCollection<IEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    public void StartShift(string streamId, uint workerId, DateTimeOffset time)
    {
        EnsureState(
            "Shift already started.",
            ShiftState.NotStarted
        );

        Raise(new ShiftStarted(streamId, workerId, time));
    }

    public void StartBreak(DateTimeOffset time)
    {
        EnsureState(
            "Cannot start break. You must be working (not finished or already on break).",
        ShiftState.Working
        );
        
        Raise(new BreakStarted(Id, WorkerId, time));
    }

    public void StopBreak(DateTimeOffset time)
    {
        EnsureState(
            "Cannot stop break. You are not currently on a break.",
            ShiftState.OnBreak
        );

        Raise(new BreakEnded(Id, WorkerId, time));
    }

    public void EndShift(DateTimeOffset time)
    {
        EnsureState(
            "Cannot end shift. You are not currently on a shift.",
            ShiftState.Working
            );

        Raise(new ShiftEnded(Id, WorkerId, time));
    }
    
    private void EnsureState(string message, ShiftState allowedState)
    {
        if (_status != allowedState)
            throw new InvalidOperationException(message);
    }
    
    private void Raise(WorkShiftEvent @event)
    {
        _uncommittedEvents.Add(@event);
        Apply(@event);
    }
    
    private void Apply(WorkShiftEvent @event)
    {
        switch (@event)
        {
            case ShiftStarted e:
                Id = e.StreamId;
                WorkerId = e.WorkerId;
                _status = ShiftState.Working;
                break;

            case BreakStarted:
                _status = ShiftState.OnBreak;
                break;

            case BreakEnded:
                _status = ShiftState.Working;
                break;

            case ShiftEnded:
                _status = ShiftState.Finished;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(@event), @event.GetType().Name, "Unknown event type.");
        }
    }
}