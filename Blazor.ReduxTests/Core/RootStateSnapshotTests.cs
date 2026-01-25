using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core;

public class RootStateSnapshotTests
{
    [Fact]
    public void SnapshotUsesDeepCopyByDefault()
    {
        var store = Store.Init(new MutableSlice { Value = 1 });

        var snapshot = store.Current;
        var snapshotSlice = (MutableSlice)snapshot.Slices[typeof(MutableSlice)];
        snapshotSlice.Value = 42;

        Assert.Equal(1, store.GetSlice<MutableSlice>()?.Value);
    }

    [Fact]
    public void SnapshotCanUseReferencesWhenConfigured()
    {
        var store = Store.Init(SnapshotStrategy.Reference, new MutableSlice { Value = 1 });

        var snapshot = store.Current;
        var snapshotSlice = (MutableSlice)snapshot.Slices[typeof(MutableSlice)];
        snapshotSlice.Value = 42;

        Assert.Equal(42, store.GetSlice<MutableSlice>()?.Value);
    }
}

public sealed class MutableSlice : ISlice
{
    public int Value { get; set; }
}
