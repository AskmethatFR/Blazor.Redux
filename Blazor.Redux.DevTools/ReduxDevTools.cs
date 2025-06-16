using Microsoft.JSInterop;
using System.Text.Json;
using Blazor.Redux.DevTools.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blazor.Redux.DevTools;

public class ReduxDevTools : IReduxDevTools, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ReduxDevTools> _logger;
    private DotNetObjectReference<ReduxDevTools>? _dotNetRef;
    private IDevToolsInterop? _devToolsInterop;
    private bool _isInitialized;

    public bool IsEnabled { get; private set; }

    public ReduxDevTools(IJSRuntime jsRuntime, ILogger<ReduxDevTools> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
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
            // Implement time travel functionality here if needed
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
}