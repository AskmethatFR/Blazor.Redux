using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

public interface IDispatchMiddleware
{
    Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction;
}
