using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

public record CounterReducer : IReducer<CounterSlice, FetchCounterAction>
{
    public CounterSlice Reduce(CounterSlice slice, FetchCounterAction action)
    {
        return slice with { IsLoading = true };
    }
}