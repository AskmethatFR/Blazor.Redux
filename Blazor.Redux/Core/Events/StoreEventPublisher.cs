namespace Blazor.Redux.Core.Events;

/// <summary>
/// Publishes store events for external observation (e.g., logging, dev tools).
/// Events include the action type, slice type, previous state, and new state.
/// </summary>
public interface IStoreEventPublisher
{
    /// <summary>
    /// Raised when a store event occurs (after reducers have been applied).
    /// </summary>
    event Action<StoreEvent>? EventOccurred;

    /// <summary>
    /// Publishes a store event, triggering the <see cref="EventOccurred"/> event.
    /// </summary>
    /// <param name="storeEvent">Event data to publish.</param>
    void PublishEvent(StoreEvent storeEvent);
}

/// <summary>
/// Data record for a store change event, capturing the action and state transition.
/// </summary>
/// <param name="EventType">Name of the action type that triggered the change.</param>
/// <param name="SliceType">Name of the slice type that changed.</param>
/// <param name="Action">Optional reference to the dispatched action.</param>
/// <param name="NewState">Optional reference to the new slice state.</param>
/// <param name="PreviousState">Optional reference to the previous slice state.</param>
/// <param name="Timestamp">UTC timestamp of the event (defaults to <see cref="DateTime.UtcNow"/>).</param>
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
