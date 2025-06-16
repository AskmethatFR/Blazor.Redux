using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public record Slice : ISlice
{
    public int Value { get; init; }
}