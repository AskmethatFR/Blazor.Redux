namespace Blazor.Redux.DevTools.Interfaces;

public interface IReduxDevTools
{
    Task InitAsync();
    Task SendAsync(object action, object state);
    Task DisconnectAsync();
    bool IsEnabled { get; }

}