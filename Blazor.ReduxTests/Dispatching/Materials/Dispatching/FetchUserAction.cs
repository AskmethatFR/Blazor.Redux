using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Dispatching.Materials.Dispatching;

public record FetchUserAction(int UserId) : IAction;