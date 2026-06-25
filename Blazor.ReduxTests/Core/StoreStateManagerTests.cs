using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core.Materials;

namespace Blazor.ReduxTests.Core;

public class StoreStateManagerTests
{
    [Fact]
    public void GetSliceReturnsNullForUnregisteredType()
    {
        var manager = CreateManager(new Slice { Value = 1 });

        var result = manager.GetSlice<Slice2>();

        Assert.Null(result);
    }

    [Fact]
    public void GetSliceReturnsDeepCopyOfRegisteredSlice()
    {
        var original = new Slice { Value = 42 };
        var manager = CreateManager(original);

        var result = manager.GetSlice<Slice>();

        Assert.Equal(42, result?.Value);
        Assert.NotSame(original, result);
    }

    [Fact]
    public void UpdateSliceReturnsDeepCopyNotSameAsInput()
    {
        var manager = CreateManager(new Slice { Value = 1 });
        var update = new Slice { Value = 99 };

        var result = manager.UpdateSlice(update);

        Assert.Equal(99, result.Value);
        Assert.NotSame(update, result);
    }

    [Fact]
    public void UpdateSliceModifiesInternalState()
    {
        var manager = CreateManager(new Slice { Value = 1 });

        manager.UpdateSlice(new Slice { Value = 50 });

        var retrieved = manager.GetSlice<Slice>();
        Assert.Equal(50, retrieved?.Value);
    }

    [Fact]
    public void UpdateSlicePreservesOtherSlices()
    {
        var manager = CreateManager(
            new Slice { Value = 1 },
            new Slice2 { Value = true, Texts = ["hello"] });

        manager.UpdateSlice(new Slice { Value = 99 });

        Assert.Equal(99, manager.GetSlice<Slice>()?.Value);
        var slice2 = manager.GetSlice<Slice2>();
        Assert.True(slice2?.Value);
        Assert.Equal(["hello"], slice2?.Texts);
    }

    [Fact]
    public void UpdateSliceThrowsOnNullValue()
    {
        var manager = CreateManager(new Slice { Value = 1 });

        Assert.Throws<ArgumentNullException>(() => manager.UpdateSlice<Slice>(null!));
    }

    [Fact]
    public void UpdateSliceAndSnapshotReturnsAtomicResult()
    {
        var manager = CreateManager(new Slice { Value = 1 });

        var (updated, snapshot) = manager.UpdateSliceAndSnapshot(new Slice { Value = 42 }, SnapshotStrategy.DeepCopy);

        Assert.Equal(42, updated.Value);
        var snapshotValue = ((Slice)snapshot.Slices[typeof(Slice)]).Value;
        Assert.Equal(42, snapshotValue);
    }

    [Fact]
    public void UpdateSliceAndSnapshotSnapshotMatchesUpdate()
    {
        var manager = CreateManager(new Slice { Value = 10 });

        var (_, snapshot1) = manager.UpdateSliceAndSnapshot(new Slice { Value = 20 }, SnapshotStrategy.DeepCopy);
        var (_, snapshot2) = manager.UpdateSliceAndSnapshot(new Slice { Value = 30 }, SnapshotStrategy.DeepCopy);

        Assert.Equal(20, ((Slice)snapshot1.Slices[typeof(Slice)]).Value);
        Assert.Equal(30, ((Slice)snapshot2.Slices[typeof(Slice)]).Value);
    }

    [Fact]
    public void CreateSnapshotReturnsAllSlices()
    {
        var manager = CreateManager(
            new Slice { Value = 10 },
            new Slice2 { Value = false, Texts = null });

        var snapshot = manager.CreateSnapshot(SnapshotStrategy.DeepCopy);

        Assert.Equal(2, snapshot.Slices.Count);
        Assert.Contains(typeof(Slice), snapshot.Slices.Keys);
        Assert.Contains(typeof(Slice2), snapshot.Slices.Keys);
    }

    [Fact]
    public void CreateSnapshotWithDeepCopyReturnsIndependentCopy()
    {
        var manager = CreateManager(new Slice { Value = 10 });
        var snapshot = manager.CreateSnapshot(SnapshotStrategy.DeepCopy);

        manager.UpdateSlice(new Slice { Value = 20 });

        var originalValue = ((Slice)snapshot.Slices[typeof(Slice)]).Value;
        Assert.Equal(10, originalValue);
    }

    [Fact]
    public void GetSliceReturnsFreshCopyEachCall()
    {
        var manager = CreateManager(new Slice { Value = 5 });

        var first = manager.GetSlice<Slice>();
        var second = manager.GetSlice<Slice>();

        Assert.NotSame(first, second);
        Assert.Equal(5, first?.Value);
    }

    [Fact]
    public void UpdateSliceThrowsOnUnregisteredType()
    {
        var manager = CreateManager(new Slice { Value = 1 });

        Assert.Throws<InvalidOperationException>(() =>
            manager.UpdateSlice(new Slice2 { Value = true, Texts = null }));
    }

    [Fact]
    public void InitializationWithNoSlicesIsValid()
    {
        var manager = CreateManager();

        Assert.Null(manager.GetSlice<Slice>());
    }

    [Fact]
    public async Task ConcurrentGetAndUpdateDoesNotThrow()
    {
        var manager = CreateManager(new Slice { Value = 0 });

        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => manager.UpdateSlice(new Slice { Value = i })));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public void ValidateAndReplaceAllSlicesReplacesAndReturnsSnapshot()
    {
        var manager = CreateManager(new SnapshotSliceA { Value = 1 });
        var incoming = new RootStateSnapshot(new Dictionary<Type, ISlice>
        {
            { typeof(SnapshotSliceA), new SnapshotSliceA { Value = 99 } }
        });

        var (snapshot, updates) = manager.ValidateAndReplaceAllSlices(incoming, true, SnapshotStrategy.DeepCopy);

        Assert.Equal(99, ((SnapshotSliceA)snapshot.Slices[typeof(SnapshotSliceA)]).Value);
        Assert.Single(updates);
        Assert.Equal(99, manager.GetSlice<SnapshotSliceA>()?.Value);
    }

    [Fact]
    public void ValidateAndReplaceAllSlicesThrowsOnKeyMismatch()
    {
        var manager = CreateManager(new SnapshotSliceA { Value = 1 });
        var incoming = new RootStateSnapshot(new Dictionary<Type, ISlice>
        {
            { typeof(SnapshotSliceA), new SnapshotSliceA { Value = 2 } },
            { typeof(SnapshotSliceB), new SnapshotSliceB { Name = "Two" } }
        });

        Assert.Throws<InvalidOperationException>(() =>
            manager.ValidateAndReplaceAllSlices(incoming, true, SnapshotStrategy.DeepCopy));
    }

    private static StoreStateManager CreateManager(params ISlice[] slices)
    {
        return new StoreStateManager(slices);
    }
}