using Blazor.Redux.Core.Events;
using Blazor.Redux.DevTools.Interfaces;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blazor.Redux.DevTools;

public class ReduxDevToolsSubscriber : IAsyncDisposable
{
    private readonly IStoreEventPublisher _eventPublisher;
    private readonly IReduxDevTools _devTools;
    private readonly IRootStateStore _rootStateStore;
    private readonly IReduxSerializer _serializer;
    private readonly ILogger<ReduxDevToolsSubscriber> _logger;

    public ReduxDevToolsSubscriber(
        IStoreEventPublisher eventPublisher,
        IReduxDevTools devTools,
        IRootStateStore rootStateStore,
        IReduxSerializer serializer,
        ILogger<ReduxDevToolsSubscriber> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _devTools = devTools ?? throw new ArgumentNullException(nameof(devTools));
        _rootStateStore = rootStateStore ?? throw new ArgumentNullException(nameof(rootStateStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync()
    {
        await _devTools.InitAsync();

        if (_devTools.IsEnabled)
        {
            _eventPublisher.EventOccurred += OnStoreEvent;
            _logger.LogInformation("Redux DevTools subscriber initialized");
        }
    }

    private async void OnStoreEvent(StoreEvent storeEvent)
    {
        try
        {
            var actionData = BuildActionPayload(storeEvent);
            var state = _serializer.SerializeState(_rootStateStore.Current);

            await _devTools.SendAsync(actionData, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event to Redux DevTools");
        }
    }

    private object BuildActionPayload(StoreEvent storeEvent)
    {
        if (storeEvent.Action is IAction action)
        {
            var serializedAction = _serializer.SerializeAction(action);
            return new
            {
                type = $"{storeEvent.EventType}_{storeEvent.SliceType}",
                payload = serializedAction,
                meta = new
                {
                    sliceType = storeEvent.SliceType,
                    timestamp = storeEvent.Timestamp
                }
            };
        }

        return new
        {
            type = $"{storeEvent.EventType}_{storeEvent.SliceType}",
            payload = storeEvent.Action,
            meta = new
            {
                sliceType = storeEvent.SliceType,
                timestamp = storeEvent.Timestamp
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_devTools.IsEnabled)
        {
            _eventPublisher.EventOccurred -= OnStoreEvent;
        }

        await _devTools.DisconnectAsync();
    }
}
