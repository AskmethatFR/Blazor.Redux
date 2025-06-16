# Blazor.Redux

A lightweight, reactive state management library for Blazor applications inspired by Redux principles.

## Overview

Blazor.Redux provides a predictable state container for Blazor applications with reactive observables, immutable state
updates, and dependency injection integration.

## Features

- **Immutable State Management**: State is organized into slices that are updated immutably
- **Action-Based Updates**: All state changes happen through dispatched actions
- **Reactive Observables**: Subscribe to state changes using System.Reactive
- **Async Support**: Handle asynchronous operations with async reducers
- **Dependency Injection**: Full integration with ASP.NET Core DI container
- **Type Safety**: Strongly typed slices, actions, and reducers

## Installation

```bash
bash dotnet add package Blazor.Redux
```

## Quick Start

### 1. Define Your State Slices

```csharp
public record CounterSlice : ISlice
{
    public int Value { get; init; }
    public bool IsLoading { get; init; }
}

public record UserSlice : ISlice
{
    public string Name { get; init; } = string.Empty;
    public bool IsAuthenticated { get; init; }
}
```

### 2. Create Actions

```csharp
public record IncrementAction(int Amount) : IAction;
public record SetLoadingAction(bool IsLoading) : IAction;
public record LoginAction(string Username) : IAction;
```

### 3. Implement Reducers

```csharp
public class CounterReducer : IReducer<CounterSlice, IncrementAction>
{
    public CounterSlice Reduce(CounterSlice slice, IncrementAction action)
    {
        return slice with { Value = slice.Value + action.Amount };
    }
}

public class LoadingReducer : IReducer<CounterSlice, SetLoadingAction>
{
    public CounterSlice Reduce(CounterSlice slice, SetLoadingAction action)
    {
        return slice with { IsLoading = action.IsLoading };
    }
}
```

### 4. Configure services

```csharp
    // Program.cs
builder.Services.AddBlazorRedux(new BlazorReduxOption()
{
    Slices = [ 
            new CounterSlice { Value = 0 },
            new UserSlice { Name = "", IsAuthenticated = false }
    ]
});

// Register reducers
builder.Services.AddScoped<IReducer<CounterSlice, IncrementAction>, CounterReducer>();
builder.Services.AddScoped<IReducer<CounterSlice, SetLoadingAction>, LoadingReducer>();
```

### 5. Use in Components

```csharp
           @page "/counter"
@inject Store Store
@inject IDispatcher Dispatcher
@implements IDisposable

<h3>Counter: @_counterValue</h3>
<button @onclick="Increment">+</button>
<button @onclick="Decrement">-</button>

@if (_isLoading)
{
    <p>Loading...</p>
}

@code {
    private int _counterValue;
    private bool _isLoading;
    private IDisposable? _subscription;

    protected override void OnInitialized()
    {
        // Subscribe to state changes
        _subscription = Store.ObserveSlice<CounterSlice>()
            .Subscribe(slice =>
            {
                _counterValue = slice.Value;
                _isLoading = slice.IsLoading;
                InvokeAsync(StateHasChanged);
            });
    }

    private void Increment()
    {
        Dispatcher.Dispatch<CounterSlice, IncrementAction>(new IncrementAction(1));
    }

    private void Decrement()
    {
        Dispatcher.Dispatch<CounterSlice, IncrementAction>(new IncrementAction(-1));
    }

    public void Dispose() => _subscription?.Dispose();
}
```

## Core Concepts

### Store

Central state container that holds all application slices. Provides methods to get, update, and observe state changes.

### Slices

Immutable portions of your application state that implement . Each slice represents a specific domain of your app.
`ISlice`

### Actions

Simple data objects implementing that describe what happened in your application. `IAction`

### Reducers

Pure functions implementing `IReducer<TSlice, TAction>` that specify how state changes in response to actions.

### Dispatchers

- **IDispatcher**: Synchronous action dispatch
- : Asynchronous action dispatch with async reducers **IAsyncDispatcher**

## Advanced Features

### Async Reducers

```csharp
public class ApiReducer : IAsyncReducer<UserSlice, LoginAction>
{
    private readonly HttpClient _httpClient;

    public ApiReducer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserSlice> ReduceAsync(UserSlice slice, LoginAction action)
    {
        var response = await _httpClient.PostAsync("/api/login", 
            JsonContent.Create(new { Username = action.Username }));
        
        var isAuthenticated = response.IsSuccessStatusCode;
        
        return slice with 
        { 
            Name = action.Username,
            IsAuthenticated = isAuthenticated 
        };
    }
}
```

### Observable Extensions

```csharp
// Observe specific properties
Store.SelectSlice<UserSlice, string>(user => user.Name)
    .Subscribe(name => Console.WriteLine($"User name changed: {name}"));

// Filter state changes
Store.WhereSlice<CounterSlice>(counter => counter.Value > 10)
    .Subscribe(counter => Console.WriteLine("Counter is high!"));

// Observe any changes
Store.ObserveAnyChange()
    .Subscribe(_ => Console.WriteLine("Something changed!"));
```

### Store Extensions

```csharp
// Get slice with default fallback
var counter = Store.GetSliceOrDefault<CounterSlice>();

// Direct slice access
var user = Store.GetSlice<UserSlice>();
```

## Best Practices

1. **Keep Reducers Pure**: No side effects, only pure transformations
2. **Use Records**: Leverage C# records for immutable slices and actions
3. **Single Responsibility**: Each reducer should handle one action type
4. **Dispose Subscriptions**: Always dispose of observable subscriptions
5. **Async for Side Effects**: Use async reducers for API calls and external operations

## API Reference

### Core Interfaces

- : Marker interface for state slices `ISlice`
- : Marker interface for actions `IAction`
- `IReducer<TSlice, TAction>`: Synchronous state reducer
- `IAsyncReducer<TSlice, TAction>`: Asynchronous state reducer
- `IDispatcher`: Action dispatcher
- : Observable store interface `IObservableStore`

### Main Classes

- `Store`: Central state container
- `Dispatcher`: Synchronous action dispatcher
- `AsyncDispatcher`: Asynchronous action dispatcher

## Requirements

- .NET 9.0+
- System.Reactive (included as dependency)

## License

see LICENSE file for details.

## Contributors

Open to contributions! Please read our contributing guidelines and submit pull requests.



