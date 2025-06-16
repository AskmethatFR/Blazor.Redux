using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core.Materials;

public record UserProfileSlice : ISlice
{
    public UserInfo User { get; init; } = new();
    public bool IsLoading { get; init; }
}