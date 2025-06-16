using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core;

/// <summary>
/// Tests pour Reducer
/// 
/// Reducer => Applique une transformation pure sur un slice en réponse à une action
/// Actions => Déclenchent des modifications de state
/// Slices => Parties immutables du state
/// </summary>
public class ReducerTests
{
    [Fact]
    public void ReducerBeAbleToIncrementCounterSlice()
    {
        var slice = new CounterSlice { Value = 0 };
        var action = new CounterSliceAction(2);
        var expected = new CounterSlice { Value = 2 };

        var actual = ReduceCounter(slice, action);

        Verify(expected, actual);
    }

    [Fact]
    public void ReducerBeAbleToAddToExistingCounterValue()
    {
        var slice = new CounterSlice { Value = 5 };
        var action = new CounterSliceAction(3);
        var expected = new CounterSlice { Value = 8 };

        var actual = ReduceCounter(slice, action);

        Verify(expected, actual);
    }

    [Fact]
    public void ReducerBeAbleToUpdateNameSlice()
    {
        var slice = new NameSlice { Name = "" };
        var action = new NameAction("Toto");
        var expected = new NameSlice { Name = "Toto" };

        var actual = ReduceName(slice, action);

        Verify(expected, actual);
    }

    [Fact]
    public void ReducerBeAbleToReplaceExistingName()
    {
        var slice = new NameSlice { Name = "OldName" };
        var action = new NameAction("NewName");
        var expected = new NameSlice { Name = "NewName" };

        var actual = ReduceName(slice, action);

        Verify(expected, actual);
    }

    [Fact]
    public void ReducerBeAbleToPreserveImmutability()
    {
        var originalSlice = new CounterSlice { Value = 10 };
        var action = new CounterSliceAction(5);

        var newSlice = ReduceCounter(originalSlice, action);

        Assert.Equal(10, originalSlice.Value); // Original inchangé
        Assert.Equal(15, newSlice.Value);      // Nouveau slice modifié
        Assert.NotSame(originalSlice, newSlice); // Instances différentes
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(10, 3, 13)]
    [InlineData(-5, 8, 3)]
    public void ReducerBeAbleToHandleDifferentCounterValues(int initial, int increment, int expected)
    {
        var slice = new CounterSlice { Value = initial };
        var action = new CounterSliceAction(increment);
        var expectedSlice = new CounterSlice { Value = expected };

        var actual = ReduceCounter(slice, action);

        Verify(expectedSlice, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Test")]
    [InlineData("Very Long Name With Spaces")]
    public void ReducerBeAbleToHandleDifferentNameValues(string name)
    {
        var slice = new NameSlice { Name = "" };
        var action = new NameAction(name);
        var expected = new NameSlice { Name = name };

        var actual = ReduceName(slice, action);

        Verify(expected, actual);
    }

    private CounterSlice ReduceCounter(CounterSlice slice, CounterSliceAction action)
    {
        var reducer = new ReduceCounterSlice();
        return reducer.Reduce(slice, action);
    }

    private NameSlice ReduceName(NameSlice slice, NameAction action)
    {
        var reducer = new ReduceName();
        return reducer.Reduce(slice, action);
    }

    private void Verify<T>(T? expected, T? actual) where T : class, ISlice
    {
        Assert.Equivalent(expected, actual);
        
        if (actual is not null)
        {
            Assert.NotSame(expected, actual);
        }
    }
}

#region Test Materials - Slices

public record CounterSlice : ISlice
{
    public int Value { get; init; }
    public bool IsLoading { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record NameSlice : ISlice
{
    public string Name { get; init; } = string.Empty;
}

#endregion

#region Test Materials - Actions

public record CounterSliceAction(int Value) : IAction;
public record NameAction(string Name) : IAction;

#endregion

#region Test Materials - Reducers

public record ReduceCounterSlice : IReducer<CounterSlice, CounterSliceAction>
{
    public CounterSlice Reduce(CounterSlice slice, CounterSliceAction action)
    {
        return slice with { Value = slice.Value + action.Value };
    }
}

public record ReduceName : IReducer<NameSlice, NameAction>
{
    public NameSlice Reduce(NameSlice slice, NameAction action)
    {
        return slice with { Name = action.Name };
    }
}

#endregion