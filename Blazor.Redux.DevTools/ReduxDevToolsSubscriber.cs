using Blazor.Redux.Core.Events;
using Blazor.Redux.DevTools.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blazor.Redux.DevTools;

public class ReduxDevToolsSubscriber : IAsyncDisposable
{
    private readonly IStoreEventPublisher _eventPublisher;
    private readonly IReduxDevTools _devTools;
    private readonly ILogger<ReduxDevToolsSubscriber> _logger;

    public ReduxDevToolsSubscriber(IStoreEventPublisher eventPublisher, IReduxDevTools devTools,
        ILogger<ReduxDevToolsSubscriber> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _devTools = devTools ?? throw new ArgumentNullException(nameof(devTools));
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
            var actionData = new
            {
                type = $"{storeEvent.EventType}_{storeEvent.SliceType}",
                payload = storeEvent.Action,
                meta = new
                {
                    sliceType = storeEvent.SliceType,
                    timestamp = storeEvent.Timestamp
                }
            };

            await _devTools.SendAsync(actionData, storeEvent.NewState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event to Redux DevTools");
        }
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