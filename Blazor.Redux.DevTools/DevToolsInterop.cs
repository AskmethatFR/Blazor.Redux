using Blazor.Redux.DevTools.Interfaces;
using Microsoft.JSInterop;

namespace Blazor.Redux.DevTools;

internal class DevToolsInterop : IDevToolsInterop
{
    private readonly IJSRuntime _jsRuntime;

    public DevToolsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> InitAsync(DotNetObjectReference<ReduxDevTools> dotNetHelper)
    {
        return await _jsRuntime.InvokeAsync<bool>("ReduxDevTools.init", dotNetHelper);
    }

    public async Task SendAsync(object action, object state)
    {
        await _jsRuntime.InvokeVoidAsync("ReduxDevTools.send", action, state);
    }

    public async Task DisconnectAsync()
    {
        await _jsRuntime.InvokeVoidAsync("ReduxDevTools.disconnect");
    }
}