using Microsoft.JSInterop;
using System.Text.Json;
using Blazor.Redux.Core;
using Blazor.Redux.DevTools.Interfaces;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blazor.Redux.DevTools;

public class ReduxDevTools : IReduxDevTools, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ReduxDevTools> _logger;
    private readonly IRootStateStore _rootStateStore;
    private readonly IStateSnapshotApplier _snapshotApplier;
    private readonly IReduxSerializer _serializer;
    private DotNetObjectReference<ReduxDevTools>? _dotNetRef;
    private IDevToolsInterop? _devToolsInterop;
    private bool _isInitialized;
    private RootStateSnapshot? _committedSnapshot;

    public bool IsEnabled { get; private set; }

    public ReduxDevTools(
        IJSRuntime jsRuntime,
        ILogger<ReduxDevTools> logger,
        IRootStateStore rootStateStore,
        IStateSnapshotApplier snapshotApplier,
        IReduxSerializer serializer)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rootStateStore = rootStateStore ?? throw new ArgumentNullException(nameof(rootStateStore));
        _snapshotApplier = snapshotApplier ?? throw new ArgumentNullException(nameof(snapshotApplier));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public async Task InitAsync()
    {
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _devToolsInterop = new DevToolsInterop(_jsRuntime);
            
            IsEnabled = await _devToolsInterop.InitAsync(_dotNetRef);
            _isInitialized = true;
            
            if (IsEnabled)
            {
                _committedSnapshot = _rootStateStore.Current;
                _logger.LogInformation("Redux DevTools connected successfully");
            }
            else
            {
                _logger.LogWarning("Redux DevTools extension not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Redux DevTools");
            IsEnabled = false;
        }
    }

    public async Task SendAsync(object actionData, object? state)
    {
        if (!IsEnabled || !_isInitialized || _devToolsInterop == null)
            return;

        try
        {
            await _devToolsInterop.SendAsync(actionData, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send action to Redux DevTools");
        }
    }

    [JSInvokable]
    public async Task OnDevToolsMessage(JsonElement message)
    {
        try
        {
            _logger.LogDebug("DevTools message received: {Message}", message);
            HandleDevToolsMessage(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling DevTools message");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_devToolsInterop != null && _isInitialized)
        {
            await _devToolsInterop.DisconnectAsync();
        }
        IsEnabled = false;
        _isInitialized = false;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _dotNetRef?.Dispose();
    }

    private void HandleDevToolsMessage(JsonElement message)
    {
        if (!message.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        var messageType = typeElement.GetString();
        if (!string.Equals(messageType, "DISPATCH", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!message.TryGetProperty("payload", out var payloadElement))
        {
            return;
        }

        if (!payloadElement.TryGetProperty("type", out var payloadTypeElement))
        {
            return;
        }

        var payloadType = payloadTypeElement.GetString();
        switch (payloadType)
        {
            case "JUMP_TO_STATE":
            case "JUMP_TO_ACTION":
                ApplyStateFromMessage(message);
                break;
            case "RESET":
                if (_committedSnapshot != null)
                {
                    _snapshotApplier.ApplySnapshot(_committedSnapshot, strictValidation: true);
                }
                break;
            case "ROLLBACK":
                if (_committedSnapshot != null)
                {
                    _snapshotApplier.ApplySnapshot(_committedSnapshot, strictValidation: true);
                }
                break;
            case "COMMIT":
                _committedSnapshot = _rootStateStore.Current;
                break;
            default:
                break;
        }
    }

    private void ApplyStateFromMessage(JsonElement message)
    {
        if (!message.TryGetProperty("state", out var stateElement))
        {
            return;
        }

        var stateJson = stateElement.GetString();
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }

        var snapshot = _serializer.DeserializeState(stateJson);
        _snapshotApplier.ApplySnapshot(snapshot, strictValidation: true);
    }
}
