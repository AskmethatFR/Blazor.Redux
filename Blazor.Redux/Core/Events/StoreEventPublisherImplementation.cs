namespace Blazor.Redux.Core.Events;

public class StoreEventPublisher : IStoreEventPublisher
{
    public event Action<StoreEvent>? EventOccurred;

    public void PublishEvent(StoreEvent storeEvent)
    {
        EventOccurred?.Invoke(storeEvent);
    }
}