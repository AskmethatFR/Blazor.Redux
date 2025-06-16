using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

#region Test Materials - Actions

#endregion

#region Test Materials - Reducers

public record SlowCounterAsyncReducer : IAsyncReducer<CounterSlice, SlowCounterAction>
{
    public async Task<CounterSlice> ReduceAsync(CounterSlice slice, SlowCounterAction action)
    {
        await Task.Delay(50);
        return slice with { Value = 25, IsLoading = false };
    }
}

#endregion