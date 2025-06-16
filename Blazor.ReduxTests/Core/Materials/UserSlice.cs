using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public record UserSlice : ISlice
{
    public string Name { get; init; } = string.Empty;
    public bool IsLoading { get; init; }
    public bool IsActive { get; set; }
    public bool IsLoggedIn { get; set; }
}