using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

public record MultiCounterAction(int Delta, string Message) : IAction;

public record MultiCounterValueReducer : IReducer<CounterSlice, MultiCounterAction>
{
    public CounterSlice Reduce(CounterSlice slice, MultiCounterAction action)
    {
        return slice with { Value = slice.Value + action.Delta };
    }
}

public record MultiCounterMessageReducer : IReducer<CounterSlice, MultiCounterAction>
{
    public CounterSlice Reduce(CounterSlice slice, MultiCounterAction action)
    {
        return slice with { Message = action.Message };
    }
}

public record MultiAsyncCounterAction(int Delta, string Message) : IAction;

public record MultiAsyncCounterValueReducer : IAsyncReducer<CounterSlice, MultiAsyncCounterAction>
{
    public Task<CounterSlice> ReduceAsync(CounterSlice slice, MultiAsyncCounterAction action)
    {
        return Task.FromResult(slice with { Value = slice.Value + action.Delta });
    }
}

public record MultiAsyncCounterMessageReducer : IAsyncReducer<CounterSlice, MultiAsyncCounterAction>
{
    public Task<CounterSlice> ReduceAsync(CounterSlice slice, MultiAsyncCounterAction action)
    {
        return Task.FromResult(slice with { Message = action.Message });
    }
}
