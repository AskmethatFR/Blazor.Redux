namespace Blazor.Redux.Interfaces;

public interface IAsyncDispatcher
{
    Task DispatchAsync<TSlice, TAction>(TAction action, CancellationToken cancellationToken = default)
        where TSlice : class, ISlice
        where TAction : class, IAction;

    
}