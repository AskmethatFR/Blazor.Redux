using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public record Slice2 : ISlice
{
    public bool Value { get; init; }
    public List<string> Texts { get; init; }
}