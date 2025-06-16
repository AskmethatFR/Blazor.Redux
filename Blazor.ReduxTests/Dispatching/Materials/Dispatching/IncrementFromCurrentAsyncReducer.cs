using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

public record IncrementFromCurrentAsyncReducer : IAsyncReducer<CounterSlice, IncrementFromCurrentAction>
{
    public async Task<CounterSlice> ReduceAsync(CounterSlice slice, IncrementFromCurrentAction action)
    {
        await Task.Delay(5);
        return slice with { Value = slice.Value + 50 };
    }
}