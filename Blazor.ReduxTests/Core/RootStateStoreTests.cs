using System.Reactive.Linq;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core;

public class RootStateStoreTests
{
    [Fact]
    public void RootStateStoreEmitsOnEveryUpdate()
    {
        var store = Store.Init(new RootSlice { Value = 0 });

        var snapshots = new List<RootStateSnapshot>();
        using var completed = new ManualResetEventSlim();

        using var subscription = store.ObserveState()
            .Take(3)
            .Subscribe(snapshot =>
            {
                snapshots.Add(snapshot);
                if (snapshots.Count == 3)
                {
                    completed.Set();
                }
            });

        store.UpdateSlice(new RootSlice { Value = 1 });
        store.UpdateSlice(new RootSlice { Value = 2 });

        Assert.True(completed.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal(3, snapshots.Count);
        Assert.Equal(0, ((RootSlice)snapshots[0].Slices[typeof(RootSlice)]).Value);
        Assert.Equal(1, ((RootSlice)snapshots[1].Slices[typeof(RootSlice)]).Value);
        Assert.Equal(2, ((RootSlice)snapshots[2].Slices[typeof(RootSlice)]).Value);
    }
}

public record RootSlice : ISlice
{
    public int Value { get; init; }
}
