using System.Reflection;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ValidateReducerInterfaces_RejectsDualSyncAndAsyncReducerForSamePair()
    {
        var reducerType = typeof(DualReducer);
        var interfaces = reducerType.GetInterfaces();
        var syncInterfaces = interfaces
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReducer<,>))
            .ToList();
        var asyncInterfaces = interfaces
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncReducer<,>))
            .ToList();

        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "ValidateReducerInterfaces",
            BindingFlags.NonPublic | BindingFlags.Static);

        var exception = Assert.Throws<TargetInvocationException>(() =>
            method!.Invoke(null, [reducerType, syncInterfaces, asyncInterfaces]));

        var inner = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("DualReducer", inner.Message);
        Assert.Contains("Use only one interface", inner.Message);
    }

    private abstract class DualReducer :
        IReducer<DualReducerSlice, DualReducerAction>,
        IAsyncReducer<DualReducerSlice, DualReducerAction>
    {
        public abstract DualReducerSlice Reduce(DualReducerSlice slice, DualReducerAction action);

        public abstract Task<DualReducerSlice> ReduceAsync(DualReducerSlice slice, DualReducerAction action);
    }

    private sealed record DualReducerSlice : ISlice;

    private sealed record DualReducerAction : IAction;
}
