using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public class Slice3 : ISlice
{
    public Slice Value { get; init; }
    public List<string> Texts { get; init; }
}