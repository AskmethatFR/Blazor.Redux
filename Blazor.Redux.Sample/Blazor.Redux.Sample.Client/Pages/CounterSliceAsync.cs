
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Sample.Client.Pages;

public record CounterSlice : ISlice
{
    public int Value { get; init; } = 0;
    public bool IsLoading { get; init; } = false;
    public string? Message { get; init; } = null;
    public string? Error { get; init; } = null;

}

public record IncrementAction : IAction;

public record CounterReducer : IReducer<CounterSlice, IncrementAction>
{
    public CounterSlice Reduce(CounterSlice slice, IncrementAction action)
    {
        return new CounterSlice { Value = slice.Value + 1 };
    }
}


