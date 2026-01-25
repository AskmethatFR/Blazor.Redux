using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core;

public class StoreSnapshotTests
{
    [Fact]
    public void ApplySnapshotUpdatesSlices()
    {
        var store = Store.Init(new SnapshotSliceA { Value = 1 }, new SnapshotSliceB { Name = "One" });
        var snapshot = new RootStateSnapshot(new Dictionary<Type, ISlice>
        {
            { typeof(SnapshotSliceA), new SnapshotSliceA { Value = 2 } },
            { typeof(SnapshotSliceB), new SnapshotSliceB { Name = "Two" } }
        });

        store.ApplySnapshot(snapshot, strictValidation: true);

        Assert.Equal(2, store.GetSlice<SnapshotSliceA>()?.Value);
        Assert.Equal("Two", store.GetSlice<SnapshotSliceB>()?.Name);
    }

    [Fact]
    public void ApplySnapshotRejectsUnknownSlicesWhenStrict()
    {
        var store = Store.Init(new SnapshotSliceA { Value = 1 });
        var snapshot = new RootStateSnapshot(new Dictionary<Type, ISlice>
        {
            { typeof(SnapshotSliceA), new SnapshotSliceA { Value = 2 } },
            { typeof(SnapshotSliceB), new SnapshotSliceB { Name = "Two" } }
        });

        Assert.Throws<InvalidOperationException>(() => store.ApplySnapshot(snapshot, strictValidation: true));
    }
}

public record SnapshotSliceA : ISlice
{
    public int Value { get; init; }
}

public record SnapshotSliceB : ISlice
{
    public string Name { get; init; } = string.Empty;
}
