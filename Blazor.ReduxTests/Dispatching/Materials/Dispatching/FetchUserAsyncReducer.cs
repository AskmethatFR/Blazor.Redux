using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core.Materials;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

public record FetchUserAsyncReducer : IAsyncReducer<UserSlice, FetchUserAction>
{
    public async Task<UserSlice> ReduceAsync(UserSlice slice, FetchUserAction action)
    {
        await Task.Delay(15);
        return slice with { Name = $"User-{action.UserId}", IsLoading = false };
    }
}