using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

public record FetchCounterAsyncReducer : IAsyncReducer<CounterSlice, FetchCounterAction>
{
    public async Task<CounterSlice> ReduceAsync(CounterSlice slice, FetchCounterAction action)
    {
        await Task.Delay(20);
        return slice with { Value = 42, IsLoading = false };
    }
}