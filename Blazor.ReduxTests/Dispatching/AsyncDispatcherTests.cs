using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;
using Blazor.ReduxTests.Core.Materials;
using Blazor.ReduxTests.Dispatching.Materials.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.ReduxTests.Dispatching;

public class AsyncDispatcherTests
{
    private IAsyncDispatcher _sut;
    private Store _store;
    private SpyStoreEventPublisher _eventPublisher = new SpyStoreEventPublisher();


    [Fact]
    public async Task AsyncDispatcherShouldExecuteSuccessfully()
    {
        var action = new FetchCounterAction();
        var expectedSlice = new CounterSlice { Value = 42, IsLoading = false };

        SetupAsyncDispatcher();
        await DispatchAsync<CounterSlice, FetchCounterAction>(action);

        Verify(expectedSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldExecuteWithPayload()
    {
        var action = new FetchUserAction(123);
        var expectedSlice = new UserSlice { Name = "User-123", IsLoading = false };

        SetupAsyncDispatcher();
        await DispatchAsync<UserSlice, FetchUserAction>(action);

        Verify(expectedSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldAccessCurrentState()
    {
        var initialSlice = new CounterSlice { Value = 100, IsLoading = false };
        var expectedSlice = new CounterSlice { Value = 150, IsLoading = false };
        var action = new IncrementFromCurrentAction();

        SetupAsyncDispatcher();
        UpdateSlice(initialSlice);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(action);

        Verify(expectedSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldCompleteSlowOperations()
    {
        var action = new SlowCounterAction();
        var expectedSlice = new CounterSlice { Value = 25, IsLoading = false };

        SetupAsyncDispatcher();
        await DispatchAsync<CounterSlice, SlowCounterAction>(action);

        Verify(expectedSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldPreserveStateImmutability()
    {
        var initialSlice = new CounterSlice { Value = 10, IsLoading = false };
        var expectedFinalSlice = new CounterSlice { Value = 60, IsLoading = false };
        var action = new IncrementFromCurrentAction();

        SetupAsyncDispatcher();
        UpdateSlice(initialSlice);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(action);

        // L'état original n'est pas modifié
        var unchangedInitialSlice = new CounterSlice { Value = 10, IsLoading = false };
        Assert.Equivalent(unchangedInitialSlice, initialSlice);

        // Le store contient le nouvel état
        Verify(expectedFinalSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldHandleSequentialDispatches()
    {
        var action = new IncrementFromCurrentAction();
        var expectedSlice = new CounterSlice { Value = 150, IsLoading = false }; // 0 + 50 + 50 + 50

        SetupAsyncDispatcher();
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(action);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(action);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(action);

        Verify(expectedSlice);
    }

    [Theory]
    [InlineData(5, 55)] // 5 + 50 = 55
    [InlineData(10, 60)] // 10 + 50 = 60  
    [InlineData(25, 75)] // 25 + 50 = 75
    public async Task AsyncDispatcherShouldHandleVariousIncrements(int initialValue, int expectedFinalValue)
    {
        var initialSlice = new CounterSlice { Value = initialValue, IsLoading = false };
        var expectedSlice = new CounterSlice { Value = expectedFinalValue, IsLoading = false };
        var action = new IncrementFromCurrentAction();

        SetupAsyncDispatcher();
        UpdateSlice(initialSlice);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(action);

        Verify(expectedSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldHandleConcurrentSliceTypes()
    {
        var counterAction = new FetchCounterAction();
        var userAction = new FetchUserAction(456);
        var expectedCounterSlice = new CounterSlice { Value = 42, IsLoading = false };
        var expectedUserSlice = new UserSlice { Name = "User-456", IsLoading = false };

        SetupAsyncDispatcher();

        var counterTask = DispatchAsync<CounterSlice, FetchCounterAction>(counterAction);
        var userTask = DispatchAsync<UserSlice, FetchUserAction>(userAction);
        await Task.WhenAll(counterTask, userTask);

        Verify(expectedCounterSlice);
        Verify(expectedUserSlice);
    }

    [Fact]
    public async Task AsyncDispatcherShouldMaintainStateConsistency()
    {
        var fetchAction = new FetchCounterAction();
        var incrementAction1 = new IncrementFromCurrentAction();
        var incrementAction2 = new IncrementFromCurrentAction();
        var expectedSlice = new CounterSlice { Value = 142, IsLoading = false }; // 42 + 50 + 50

        SetupAsyncDispatcher();
        await DispatchAsync<CounterSlice, FetchCounterAction>(fetchAction);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(incrementAction1);
        await DispatchAsync<CounterSlice, IncrementFromCurrentAction>(incrementAction2);

        Verify(expectedSlice);
    }

    [Fact]
    public Task AsyncDispatcherShouldTriggerSyncReducerFirst()
    {
        var action = new FetchCounterAction();
        var expectedSlice = new CounterSlice { Value = 0, IsLoading = true };

        SetupAsyncDispatcher();
        _sut.DispatchAsync<CounterSlice, FetchCounterAction>(action);

        Verify(expectedSlice);
        return Task.CompletedTask;
    }

    [Fact]
    public void DispatcherShouldPublishEventOnDispatch()
    {
        // Arrange
        var action = new FetchCounterAction();

        // Act
         SetupAsyncDispatcher();
         _sut.DispatchAsync<CounterSlice, FetchCounterAction>(action);

        // Assert
        var lastEvent = _eventPublisher.LastPublishedEvent;
        Assert.NotNull(lastEvent);
        Assert.Equal("FetchCounterAction", lastEvent.EventType);
        Assert.Equal("CounterSlice", lastEvent.SliceType);
        Assert.Equal(action, lastEvent.Action);
        Assert.Equal(new CounterSlice { Value = 0, IsLoading = true}, lastEvent.NewState);
        Assert.Equal(new CounterSlice { Value = 0 }, lastEvent.PreviousState);
    }

    private void SetupAsyncDispatcher()
    {
        var services = new ServiceCollection();
        var counterSlice = new CounterSlice { Value = 0, IsLoading = false };
        var userSlice = new UserSlice { Name = "", IsLoading = false };

        services.AddBlazorRedux(new BlazorReduxOption()
        {
            Slices = [counterSlice, userSlice],
            Assembly = Assembly.GetExecutingAssembly()
        });
        services.AddTransient<IStoreEventPublisher, SpyStoreEventPublisher>(_ => _eventPublisher);

        var serviceProvider = services.BuildServiceProvider();
        _sut = serviceProvider.GetRequiredService<IAsyncDispatcher>();
        _store = serviceProvider.GetRequiredService<Store>();
    }

    private async Task DispatchAsync<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        await _sut.DispatchAsync<TSlice, TAction>(action);
    }

    private void UpdateSlice<T>(T slice) where T : class, ISlice
    {
        _store.UpdateSlice(slice);
    }

    private void Verify<T>(T? expected) where T : class, ISlice
    {
        var actual = _store.GetSlice<T>();
        Assert.Equivalent(expected, actual);

        if (actual is not null)
        {
            Assert.NotSame(expected, actual);
        }
    }
}