using Microsoft.JSInterop;

namespace Blazor.Redux.DevTools.Interfaces;

public interface IDevToolsInterop
{
    Task<bool> InitAsync(DotNetObjectReference<ReduxDevTools> dotNetHelper);
    Task SendAsync(object action, object state);
    Task DisconnectAsync();
}