using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core.Materials;

namespace Blazor.ReduxTests.Extensions;

/// <summary>
/// Tests pour StoreExtensions
/// 
/// StoreExtensions => Extension methods pour faciliter l'utilisation du Store
/// GetSliceOrDefault => Récupère un slice ou retourne une instance par défaut
/// </summary>
public class StoreExtensionsTests
{
    private Store _sut;

    [Fact]
    public void StoreExtensionsBeAbleToGetExistingSlice()
    {
        var slice = new Slice { Value = 42 };
        var expected = new Slice { Value = 42 };

        AddSlice(slice);
        var actual = GetSliceOrDefault<Slice>();

        Verify(expected, actual);
    }

    [Fact]
    public void StoreExtensionsBeAbleToReturnDefaultWhenSliceNotFound()
    {
        var slice = new Slice { Value = 10 };
        var expected = new Slice2(); // Instance par défaut

        AddSlice(slice);
        var actual = GetSliceOrDefault<Slice2>();

        Assert.NotNull(actual);
        Assert.IsType<Slice2>(actual);
        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void StoreExtensionsBeAbleToReturnDefaultWithProperValues()
    {
        var expected = new Slice { Value = 0 }; // Valeur par défaut

        AddSlice(); // Store vide
        var actual = GetSliceOrDefault<Slice>();

        Verify(expected, actual);
    }

    [Fact]
    public void StoreExtensionsBeAbleToWorkWithComplexSlices()
    {
        var slice3 = new Slice3 
        { 
            Value = new Slice { Value = 100 },
            Texts = ["item1", "item2"]
        };

        AddSlice(slice3);
        var actual = GetSliceOrDefault<Slice3>();

        Verify(slice3, actual);
    }

    [Fact]
    public void StoreExtensionsBeAbleToReturnDefaultForMissingComplexSlice()
    {
        var slice = new Slice { Value = 5 };
        var expected = new Slice3(); // Instance par défaut avec propriétés null

        AddSlice(slice);
        var actual = GetSliceOrDefault<Slice3>();

        Assert.NotNull(actual);
        Assert.Null(actual.Value);
        Assert.Null(actual.Texts);
    }

    [Fact]
    public void StoreExtensionsBeAbleToHandleMultipleSlices()
    {
        var slice = new Slice { Value = 10 };
        var slice2 = new Slice2 { Value = true, Texts = ["test"] };
        var expectedSlice = new Slice { Value = 10 };
        var expectedSlice2 = new Slice2 { Value = true, Texts = ["test"] };
        var expectedMissing = new Slice3();

        AddSlice(slice, slice2);
        var actualSlice = GetSliceOrDefault<Slice>();
        var actualSlice2 = GetSliceOrDefault<Slice2>();
        var actualMissing = GetSliceOrDefault<Slice3>();

        Verify(expectedSlice, actualSlice);
        Verify(expectedSlice2, actualSlice2);
        Assert.NotNull(actualMissing);
        Assert.Equivalent(expectedMissing, actualMissing);
    }

    [Fact]
    public void StoreExtensionsBeAbleToPreserveTypeInformation()
    {
        var slice = new Slice { Value = 25 };
        var expected = new Slice { Value = 25 };

        AddSlice(slice);
        var actual = GetSliceOrDefault<Slice>();

        Assert.IsType<Slice>(actual);
        Assert.IsAssignableFrom<ISlice>(actual);
        Verify(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-10)]
    [InlineData(int.MaxValue)]
    public void StoreExtensionsBeAbleToPreserveAllValues(int value)
    {
        var slice = new Slice { Value = value };
        var expected = new Slice { Value = value };

        AddSlice(slice);
        var actual = GetSliceOrDefault<Slice>();

        Verify(expected, actual);
    }

    [Fact]
    public void StoreExtensionsBeAbleToWorkWithUpdateSlice()
    {
        var initialSlice = new Slice { Value = 10 };
        var updatedSlice = new Slice { Value = 20 };
        var expected = new Slice { Value = 20 };

        AddSlice(initialSlice);
        UpdateSlice(updatedSlice);
        var actual = GetSliceOrDefault<Slice>();

        Verify(expected, actual);
        Assert.NotSame(initialSlice, actual);
    }

    [Fact]
    public void StoreExtensionsBeAbleToHandleUserSlices()
    {
        var userSlice = new UserSlice 
        { 
            Name = "test",
            IsLoading = false
        };
        var expected = new UserSlice 
        { 
            Name = "test",
            IsLoading = false
        };

        AddSlice(userSlice);
        var actual = GetSliceOrDefault<UserSlice>();

        Verify(expected, actual);
    }

    private void AddSlice(params ISlice[] slices)
    {
        _sut = Store.Init(slices);
    }

    private T GetSliceOrDefault<T>() where T : class, ISlice, new()
    {
        return _sut.GetSliceOrDefault<T>();
    }

    private void UpdateSlice<T>(T slice) where T : class, ISlice
    {
        _sut.UpdateSlice(slice);
    }

    private void Verify<T>(T? expected, T? actual) where T : class, ISlice
    {
        Assert.NotNull(actual);
        Assert.Equivalent(expected, actual);
        
        if (actual is not null && expected is not null)
        {
            Assert.NotSame(expected, actual);
        }
    }
}