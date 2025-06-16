using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core.Materials;

namespace Blazor.ReduxTests.Core;

/// <summary>
/// Tests pour AsyncReducer
/// 
/// AsyncReducer => Modifie un slice de manière asynchrone
/// Action Async => Déclenche une modification asynchrone du state
/// Opérations async => Simulent des appels API, base de données, etc.
/// </summary>
public class AsyncReducerTests
{
    private Store _sut;

    [Fact]
    public async Task AsyncReducerBeAbleToUpdateSliceAfterAsyncOperation()
    {
        var slice = new AsyncCounterSlice { Value = 0 };
        var action = new IncrementAsyncAction(10);
        var expected = new AsyncCounterSlice { Value = 10, IsLoading = false };

        AddSlice(slice);
        await ReduceAsync<AsyncCounterSlice, IncrementAsyncAction>(action);
        
        Verify(expected);
    }

    [Fact]
    public async Task AsyncReducerBeAbleToHandleSequentialCalls()
    {
        var slice = new AsyncCounterSlice { Value = 0 };
        var action1 = new IncrementAsyncAction(5);
        var action2 = new IncrementAsyncAction(7);
        var action3 = new IncrementAsyncAction(3);
        var expected = new AsyncCounterSlice { Value = 15, IsLoading = false }; // 5 + 7 + 3

        AddSlice(slice);
        await ReduceAsync<AsyncCounterSlice, IncrementAsyncAction>(action1);
        await ReduceAsync<AsyncCounterSlice, IncrementAsyncAction>(action2);
        await ReduceAsync<AsyncCounterSlice, IncrementAsyncAction>(action3);

        Verify(expected);
    }

    [Fact]
    public async Task AsyncReducerBeAbleToHandleConcurrentCalls()
    {
        var slice = new AsyncCounterSlice { Value = 0 };
        var action1 = new SlowIncrementAsyncAction(2, TimeSpan.FromMilliseconds(50));
        var action2 = new SlowIncrementAsyncAction(3, TimeSpan.FromMilliseconds(30));
        var action3 = new SlowIncrementAsyncAction(5, TimeSpan.FromMilliseconds(20));
        var expected = new AsyncCounterSlice { Value = 5, IsLoading = false }; // Le plus rapide gagne

        AddSlice(slice);
        
        var currentSlice = _sut.GetSlice<AsyncCounterSlice>()!;
        var reducer = new SlowIncrementAsyncReducer();
        
        var task1 = reducer.ReduceAsync(currentSlice, action1);
        var task2 = reducer.ReduceAsync(currentSlice, action2);
        var task3 = reducer.ReduceAsync(currentSlice, action3);
        
        var results = await Task.WhenAll(task1, task2, task3);
        _sut.UpdateSlice(results[2]); // Le plus rapide gagne

        Verify(expected);
    }

    [Fact]
    public async Task AsyncReducerBeAbleToHandleDifferentSliceTypes()
    {
        var counterSlice = new AsyncCounterSlice { Value = 0 };
        var dataSlice = new AsyncDataSlice { Data = "" };
        var counterAction = new IncrementAsyncAction(42);
        var dataAction = new FetchDataAsyncAction("test-data");
        var expectedCounter = new AsyncCounterSlice { Value = 42, IsLoading = false };
        var expectedData = new AsyncDataSlice { Data = "test-data", IsLoading = false };

        AddSlice(counterSlice, dataSlice);
        await ReduceAsync<AsyncCounterSlice, IncrementAsyncAction>(counterAction);
        await ReduceAsync<AsyncDataSlice, FetchDataAsyncAction>(dataAction);

        Verify(expectedCounter);
        Verify(expectedData);
    }

    [Fact]
    public async Task AsyncReducerWithCancellationBeAbleToRespectCancellationToken()
    {
        var slice = new AsyncCounterSlice { Value = 0 };
        var action = new SlowIncrementAsyncAction(10, TimeSpan.FromMilliseconds(500));

        AddSlice(slice);
        
        using var cts = new CancellationTokenSource();
        var currentSlice = _sut.GetSlice<AsyncCounterSlice>()!;
        var reducer = new CancellableAsyncReducer();
        var task = reducer.ReduceAsync(currentSlice, action, cts.Token);

        await Task.Delay(50);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(100)]
    public async Task AsyncReducerWithDifferentValuesBeAbleToUpdateCorrectly(int value)
    {
        var slice = new AsyncCounterSlice { Value = 0 };
        var action = new IncrementAsyncAction(value);
        var expected = new AsyncCounterSlice { Value = value, IsLoading = false };

        AddSlice(slice);
        await ReduceAsync<AsyncCounterSlice, IncrementAsyncAction>(action);

        Verify(expected);
    }

    [Fact]
    public async Task AsyncReducerBeAbleToPreserveImmutability()
    {
        var slice = new AsyncCounterSlice { Value = 10 };
        var action = new IncrementAsyncAction(5);

        AddSlice(slice);
        
        var currentSlice = _sut.GetSlice<AsyncCounterSlice>()!;
        var reducer = new IncrementAsyncReducer();
        var newSlice = await reducer.ReduceAsync(currentSlice, action);

        // L'original ne doit pas être modifié
        Assert.Equal(10, currentSlice.Value);
        Assert.Equal(15, newSlice.Value);
        Assert.NotSame(currentSlice, newSlice);
    }

    private void AddSlice(params ISlice[] slices)
    {
        _sut = Store.Init(slices);
    }

    private async Task ReduceAsync<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var currentSlice = _sut.GetSlice<TSlice>()!;
        var reducer = CreateReducer<TSlice, TAction>();
        var newSlice = await reducer.ReduceAsync(currentSlice, action);
        _sut.UpdateSlice(newSlice);
    }

    private static IAsyncReducer<TSlice, TAction> CreateReducer<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        return (typeof(TSlice), typeof(TAction)) switch
        {
            (Type s, Type a) when s == typeof(AsyncCounterSlice) && a == typeof(IncrementAsyncAction) 
                => (IAsyncReducer<TSlice, TAction>)new IncrementAsyncReducer(),
            (Type s, Type a) when s == typeof(AsyncDataSlice) && a == typeof(FetchDataAsyncAction) 
                => (IAsyncReducer<TSlice, TAction>)new FetchDataAsyncReducer(),
            _ => throw new NotSupportedException($"Reducer non supporté pour {typeof(TSlice).Name} et {typeof(TAction).Name}")
        };
    }

    private void Verify<T>(T? expected) where T : class, ISlice
    {
        var actual = _sut.GetSlice<T>();
        Assert.Equivalent(expected, actual);
        
        if (actual is not null)
        {
            Assert.NotSame(expected, actual);
        }
    }
}

#region Test Materials - Actions

public record IncrementAsyncAction(int Value) : IAction;
public record FetchDataAsyncAction(string Data) : IAction;
public record SlowIncrementAsyncAction(int Value, TimeSpan Delay) : IAction;

#endregion

#region Test Materials - AsyncReducers

public record IncrementAsyncReducer : IAsyncReducer<AsyncCounterSlice, IncrementAsyncAction>
{
    public async Task<AsyncCounterSlice> ReduceAsync(AsyncCounterSlice slice, IncrementAsyncAction action)
    {
        await Task.Delay(10);
        return slice with { Value = slice.Value + action.Value, IsLoading = false };
    }
}

public record FetchDataAsyncReducer : IAsyncReducer<AsyncDataSlice, FetchDataAsyncAction>
{
    public async Task<AsyncDataSlice> ReduceAsync(AsyncDataSlice slice, FetchDataAsyncAction action)
    {
        await Task.Delay(25);
        return slice with { Data = action.Data, IsLoading = false };
    }
}

public record SlowIncrementAsyncReducer : IAsyncReducer<AsyncCounterSlice, SlowIncrementAsyncAction>
{
    public async Task<AsyncCounterSlice> ReduceAsync(AsyncCounterSlice slice, SlowIncrementAsyncAction action)
    {
        await Task.Delay(action.Delay);
        return slice with { Value = slice.Value + action.Value, IsLoading = false };
    }
}

public record CancellableAsyncReducer : IAsyncReducer<AsyncCounterSlice, SlowIncrementAsyncAction>
{
    public async Task<AsyncCounterSlice> ReduceAsync(AsyncCounterSlice slice, SlowIncrementAsyncAction action, CancellationToken cancellationToken = default)
    {
        await Task.Delay(action.Delay, cancellationToken);
        return slice with { Value = slice.Value + action.Value, IsLoading = false };
    }

    public Task<AsyncCounterSlice> ReduceAsync(AsyncCounterSlice slice, SlowIncrementAsyncAction action)
    {
        return ReduceAsync(slice, action, CancellationToken.None);
    }
}

#endregion