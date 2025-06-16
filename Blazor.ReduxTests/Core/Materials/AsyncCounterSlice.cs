using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public record AsyncCounterSlice : ISlice
{
    public int Value { get; init; }
    public bool IsLoading { get; init; }
}