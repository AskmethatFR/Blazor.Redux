using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public record AsyncDataSlice : ISlice
{
    public string Data { get; init; } = string.Empty;
    public bool IsLoading { get; init; }
}