using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Blazor.Redux.Interfaces;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Blazor.Redux.Extensions;
using IDispatcher = Blazor.Redux.Interfaces.IDispatcher;
using Store = Blazor.Redux.Core.Store;

// Define actions for benchmarking
public record IncrementAction(int Amount = 1) : IAction;

// Define a simple state for benchmarking that implements ISlice
public record CounterState(int Count = 0) : ISlice;

[FeatureState]
public record FluxorCounterState
{
    public int Count { get; init; }
}

// Blazor.Redux reducer
public record CounterReducer : IReducer<CounterState, IncrementAction>
{
    public CounterState Reduce(CounterState state, IncrementAction action)
    {
        return state with { Count = action.Amount };
    }
}

// BlazorHooked reducer
public record CounterHookedReducer
{
    public static CounterState Reduce(CounterState state, IncrementAction action)
    {
        return state with { Count = state.Count + action.Amount };
    }
}

// Fluxor reducer
public static class CounterFluxorReducer
{
    [ReducerMethod]
    public static FluxorCounterState Reduce(FluxorCounterState state, IncrementAction action)
    {
        return state with { Count = state.Count + action.Amount };
    }
}

// Benchmark class
[MemoryDiagnoser]
public class StateManagementBenchmark
{
    private ServiceProvider _blazorReduxProvider;
    private ServiceProvider _fluxorProvider;

    [GlobalSetup]
    public async Task Setup()
    {
        // Setup Blazor.Redux with DI
        var serviceCollection = new ServiceCollection();
        var initialCounterSlice = new CounterState { Count = 0 };

        serviceCollection.AddBlazorRedux(new BlazorReduxOption()
        {
            Slices = [initialCounterSlice],
            Assembly = typeof(Program).Assembly
        });
        _blazorReduxProvider = serviceCollection.BuildServiceProvider();

        // Get the store and add our slice
        // var store = blazorReduxProvider.GetRequiredService<Blazor.Redux.Core.Store>();
        // store.UpdateSlice(new CounterState(Count: 0));
        // _blazorReduxStore = store;

        // Setup BlazorHooked
        // _blazorHookedStore = new Store<CounterState>(new CounterState(), (state, action) =>
        // {
        //     if (action is IncrementAction incrementAction)
        //         return CounterHookedReducer.Reduce(state, incrementAction);
        //     return state;
        // });

        //Setup Fluxor
        var fluxorServices = new ServiceCollection();
        fluxorServices.AddFluxor(options =>
        {
            options.ScanAssemblies(typeof(CounterFluxorReducer).Assembly);
        });
        _fluxorProvider = fluxorServices.BuildServiceProvider();
        var fluxorStore = _fluxorProvider.GetRequiredService<Fluxor.IStore>();
        await fluxorStore.InitializeAsync();
    }

    [Benchmark]
    public int BlazorRedux_Dispatch_AndReadState()
    {
        var dispatcher = _blazorReduxProvider.GetRequiredService<IDispatcher>();
        var amount = 1;
        dispatcher.Dispatch<CounterState, IncrementAction>(new IncrementAction(amount));
        var store = _blazorReduxProvider.GetRequiredService<Store>();

        return store.GetSlice<CounterState>()!.Count;
    }

    [Benchmark]
    public int Fluxor_Dispatch_AndReadState()
    {
        var dispatcher = _fluxorProvider.GetRequiredService<Fluxor.IDispatcher>();
        var amount = 1;
        dispatcher.Dispatch(new IncrementAction(amount));
        var state = _fluxorProvider.GetRequiredService<IState<FluxorCounterState>>();

        return state.Value.Count;
    }

}

// Run the benchmarks
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<StateManagementBenchmark>();
    }
}
