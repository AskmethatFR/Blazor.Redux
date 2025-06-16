using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core.Materials;

namespace Blazor.ReduxTests.Extensions;

/// <summary>
/// Tests des cas limites pour StoreExtensions
/// </summary>
public class StoreExtensionsEdgeCasesTests
{
    private Store _sut;

    [Fact]
    public void StoreExtensionsBeAbleToHandleEmptyStore()
    {
        InitStore();
        
        var result = _sut.GetSliceOrDefault<Slice>();
        
        Assert.NotNull(result);
        Assert.Equal(0, result.Value);
        Assert.IsType<Slice>(result);
    }

    [Fact]
    public void StoreExtensionsBeAbleToWorkWithStoreContainingOtherSlices()
    {
        var userSlice = new UserSlice { Name = "user", IsLoading = false };
        InitStore(userSlice);
        
        var result = _sut.GetSliceOrDefault<Slice>();
        
        Assert.NotNull(result);
        Assert.Equal(0, result.Value);
        Assert.NotSame(userSlice, result);
    }

    [Fact]
    public void StoreExtensionsBeAbleToBeConsistentAcrossMultipleCalls()
    {
        InitStore(new Slice { Value = 100 });
        
        var result1 = _sut.GetSliceOrDefault<Slice>();
        var result2 = _sut.GetSliceOrDefault<Slice>();
        
        Assert.Equivalent(result1, result2);
    }

    [Fact]
    public void StoreExtensionsBeAbleToWorkAfterUpdateSlice()
    {
        InitStore(new Slice { Value = 10 });
        
        _sut.UpdateSlice(new Slice { Value = 50 });
        var result = _sut.GetSliceOrDefault<Slice>();
        
        Assert.Equal(50, result.Value);
    }

    [Fact]
    public void StoreExtensionsBeAbleToPreserveImmutabilityAfterUpdateSlice()
    {
        var originalSlice = new Slice { Value = 42 };
        InitStore(originalSlice);
        
        var beforeUpdate = _sut.GetSliceOrDefault<Slice>();
        _sut.UpdateSlice(new Slice { Value = 100 });
        var afterUpdate = _sut.GetSliceOrDefault<Slice>();
        
        Assert.Equal(42, beforeUpdate.Value);
        Assert.Equal(100, afterUpdate.Value);
        Assert.NotSame(beforeUpdate, afterUpdate);
    }

    [Fact]
    public void StoreExtensionsBeAbleToHandleMultipleUpdateSliceCalls()
    {
        InitStore(new Slice { Value = 0 });
        
        _sut.UpdateSlice(new Slice { Value = 10 });
        VerifySliceValue(10);

        _sut.UpdateSlice(new Slice { Value = 20 });
        VerifySliceValue(20);

        _sut.UpdateSlice(new Slice { Value = 30 });
        VerifySliceValue(30);
    }

    [Fact]
    public void StoreExtensionsBeAbleToHandleGenericConstraints()
    {
        InitStore();
        
        var result = _sut.GetSliceOrDefault<Slice>();
        
        Assert.NotNull(result);
    }

    private void InitStore(params ISlice[] slices)
    {
        _sut = Store.Init(slices);
    }

    private void VerifySliceValue(int expectedValue)
    {
        var result = _sut.GetSliceOrDefault<Slice>();
        Assert.Equal(expectedValue, result.Value);
    }
}