namespace Blazor.Redux.Core.Events;

public interface IStoreEventPublisher
{
    event Action<StoreEvent>? EventOccurred;
    void PublishEvent(StoreEvent storeEvent);
}

public record StoreEvent(
    string EventType,
    string SliceType,
    object? Action = null,
    object? NewState = null,
    object? PreviousState = null,
    DateTime Timestamp = default
)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}