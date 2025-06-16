using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Sample.Client.Pages;

// Action pour incrémenter de façon asynchrone avec délai
public record IncrementAsyncAction(int DelayMs = 1000) : IAction;

// Actions internes pour gérer les états loading/success/error
public record SetLoadingAction(bool IsLoading, string? Message = null) : IAction;

public record SetErrorAction(string Error) : IAction;

public record SetSuccessAction(string? Message = null) : IAction;

public record IncrementAsyncReducer : IAsyncReducer<CounterSlice, IncrementAsyncAction>
{
    public async Task<CounterSlice> ReduceAsync(CounterSlice slice, IncrementAsyncAction action)
    {
        // Simuler une opération asynchrone
        await Task.Delay(action.DelayMs);

        return slice with
        {
            Value = slice.Value + 1,
            IsLoading = false,
            Message = $"Incrémenté après {action.DelayMs}ms de délai",
            Error = null
        };
    }
}

public record SetMessageAction(string NextMessage) : IAction;

public record SetMessageReducer : IReducer<CounterSlice, SetMessageAction>
{
    public CounterSlice Reduce(CounterSlice slice, SetMessageAction action)
    {
        return slice with
        {
            Message = action.NextMessage
        };
    }
}

public record SetLoadingReducer : IReducer<CounterSlice, SetLoadingAction>
{
    public CounterSlice Reduce(CounterSlice slice, SetLoadingAction action)
    {
        return slice with
        {
            IsLoading = action.IsLoading,
            Message = action.Message,
            Error = null
        };
    }
}

public record SetErrorReducer : IReducer<CounterSlice, SetErrorAction>
{
    public CounterSlice Reduce(CounterSlice slice, SetErrorAction action)
    {
        return slice with
        {
            IsLoading = false,
            Error = action.Error,
            Message = null
        };
    }
}

public record SetSuccessReducer : IReducer<CounterSlice, SetSuccessAction>
{
    public CounterSlice Reduce(CounterSlice slice, SetSuccessAction action)
    {
        return slice with
        {
            IsLoading = false,
            Message = action.Message,
            Error = null
        };
    }
}
