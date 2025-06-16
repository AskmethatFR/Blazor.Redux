using System.Reactive.Linq;
using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core.Materials;

namespace Blazor.ReduxTests.Core;

public class ObservableStoreTests
{
    private Store _sut;
    private int _changeCount = 0;

    [Fact]
    public void ObservableStoreBeAbleToObserveSliceChanges()
    {
        var slice = new Slice { Value = 0 };
        var expected = new[] { 0, 10, 20, 30 };

        AddSlice(slice);
        var observedValues = ObserveSliceValues();
        UpdateSliceValues(10, 20, 30);

        Verify(expected, observedValues);
    }

    [Fact]
    public void ObservableStoreBeAbleToObserveAnyChange()
    {
        var slice1 = new Slice { Value = 0 };
        var slice2 = new UserSlice { Name = "", IsLoading = false };
        var expectedChangeCount = 3;

        AddSlice(slice1, slice2);
        ObserveAnyChange();
        
        UpdateSlice(new Slice { Value = 42 });
        UpdateSlice(new UserSlice { Name = "John", IsLoading = false });
        UpdateSlice(new Slice { Value = 100 });

        Assert.Equal(expectedChangeCount, _changeCount);
    }

    [Fact]
    public void ObservableStoreBeAbleToSelectSpecificProperty()
    {
        var slice = new UserSlice { Name = "", IsLoading = false };
        var expected = new[] { "", "Alice", "Bob", "Charlie" };

        AddSlice(slice);
        var observedNames = ObserveUserNames();
        
        UpdateSlice(new UserSlice { Name = "Alice", IsLoading = false });
        UpdateSlice(new UserSlice { Name = "Bob", IsLoading = false });
        UpdateSlice(new UserSlice { Name = "Bob", IsLoading = true }); // Même nom, pas d'émission
        UpdateSlice(new UserSlice { Name = "Charlie", IsLoading = false });

        Verify(expected, observedNames);
    }

    [Fact]
    public void ObservableStoreBeAbleToFilterSliceChanges()
    {
        var slice = new Slice { Value = 0 };
        var expected = new[] { 75, 100 };

        AddSlice(slice);
        var highValues = ObserveHighValues();
        UpdateSliceValues(10, 75, 30, 100);

        Verify(expected, highValues);
    }

    [Fact]
    public void ObservableStoreBeAbleToHandleMultipleSubscribers()
    {
        var slice = new Slice { Value = 0 };
        var expectedSubscriber1 = new[] { 0, 5, 10 };
        var expectedSubscriber2 = new[] { 0, 10, 20 };

        AddSlice(slice);
        var (subscriber1Values, subscriber2Values) = ObserveWithMultipleSubscribers();
        UpdateSliceValues(5, 10);

        Verify(expectedSubscriber1, subscriber1Values);
        Verify(expectedSubscriber2, subscriber2Values);
    }

    private void AddSlice(params ISlice[] slices)
    {
        _sut = Store.Init(slices);
    }

    private List<int> ObserveSliceValues()
    {
        var observedValues = new List<int>();
        _sut.ObserveSlice<Slice>()
            .Select(slice => slice.Value)
            .Subscribe(value => observedValues.Add(value));
        return observedValues;
    }

    private void ObserveAnyChange()
    {
        _sut.ObserveAnyChange()
            .Subscribe(_ =>
            {
                _changeCount++;
            });
    }

    private List<string> ObserveUserNames()
    {
        var observedNames = new List<string>();
        _sut.SelectSlice<UserSlice, string>(slice => slice.Name)
            .Subscribe(name => observedNames.Add(name));
        return observedNames;
    }

    private List<int> ObserveHighValues()
    {
        var highValues = new List<int>();
        _sut.WhereSlice<Slice>(slice => slice.Value > 50)
            .Select(slice => slice.Value)
            .Subscribe(value => highValues.Add(value));
        return highValues;
    }

    private (List<int> subscriber1, List<int> subscriber2) ObserveWithMultipleSubscribers()
    {
        var subscriber1Values = new List<int>();
        var subscriber2Values = new List<int>();
        
        _sut.ObserveSlice<Slice>()
            .Select(slice => slice.Value)
            .Subscribe(value => subscriber1Values.Add(value));
            
        _sut.ObserveSlice<Slice>()
            .Select(slice => slice.Value * 2)
            .Subscribe(value => subscriber2Values.Add(value));

        return (subscriber1Values, subscriber2Values);
    }

    private void UpdateSlice<T>(T slice) where T : class, ISlice
    {
        _sut.UpdateSlice(slice);
    }

    private void UpdateSliceValues(params int[] values)
    {
        foreach (var value in values)
        {
            UpdateSlice(new Slice { Value = value });
        }
    }

    private void Verify<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Assert.Equal(expected, actual);
    }
}