using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;
using Blazor.ReduxTests.Dispatching.Materials.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.ReduxTests.Dispatching;

/// <summary>
/// Tests pour Dispatcher
/// 
/// Dispatcher => Gère les demandes d'actions synchrones
/// Actions => Déclenchent des modifications de state via des reducers
/// Reducers => Appliquent les transformations au state
/// </summary>
public class DispatcherTests
{
    private IDispatcher _sut;
    private Store _store;

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public void DispatcherBeAbleToUpdateSlice(int value)
    {
        var action = new CounterSliceAction(value);
        var expected = new CounterSlice { Value = value };

        SetupDispatcher();
        Dispatch<CounterSlice, CounterSliceAction>(action);
        
        Verify(expected);
    }

    [Fact]
    public void DispatcherBeAbleToAddToExistingValue()
    {
        var initialSlice = new CounterSlice { Value = 5 };
        var action = new CounterSliceAction(3);
        var expected = new CounterSlice { Value = 8 };

        SetupDispatcher(initialSlice);
        Dispatch<CounterSlice, CounterSliceAction>(action);
        
        Verify(expected);
    }

    [Fact]
    public void DispatcherBeAbleToCumulateValues()
    {
        var initialSlice = new CounterSlice { Value = 0 };
        var action1 = new CounterSliceAction(2);
        var action2 = new CounterSliceAction(4);
        var expected = new CounterSlice { Value = 6 };

        SetupDispatcher(initialSlice);
        Dispatch<CounterSlice, CounterSliceAction>(action1);
        Dispatch<CounterSlice, CounterSliceAction>(action2);
        
        Verify(expected);
    }

    [Theory]
    [InlineData("Test Name")]
    [InlineData("Redux Test")]
    public void DispatcherBeAbleToUpdateNameSlice(string name)
    {
        var action = new NameAction(name);
        var expected = new NameSlice { Name = name };

        SetupDispatcher();
        Dispatch<NameSlice, NameAction>(action);
        
        Verify(expected);
    }

    [Fact]
    public void DispatcherBeAbleToUpdateDifferentSliceTypes()
    {
        var counterAction = new CounterSliceAction(10);
        var nameAction = new NameAction("Redux Test");
        var expectedCounter = new CounterSlice { Value = 10 };
        var expectedName = new NameSlice { Name = "Redux Test" };

        SetupDispatcher();
        Dispatch<CounterSlice, CounterSliceAction>(counterAction);
        Dispatch<NameSlice, NameAction>(nameAction);
        
        Verify(expectedCounter);
        Verify(expectedName);
    }

    [Fact]
    public void DispatcherNotBeAbleToAcceptNullAction()
    {
        SetupDispatcher();
        
        Assert.Throws<ArgumentNullException>(() =>
            _sut.Dispatch<CounterSlice, CounterSliceAction>(null!));
    }

    [Fact]
    public void DispatcherNotModifyStateWithUnregisteredReducer()
    {
        var counterSlice = new CounterSlice { Value = 5 };
        var nameSlice = new NameSlice { Name = "Test" };
        var action = new UnknownAction();

        SetupDispatcher(counterSlice, nameSlice);
        Dispatch<CounterSlice, UnknownAction>(action);

        Verify(counterSlice);
        Verify(nameSlice);
    }

    [Fact]
    public void DispatcherBeAbleToUseCorrectReducer()
    {
        var counterAction = new CounterSliceAction(7);
        var nameAction = new NameAction("Specific Name");
        var expectedCounter = new CounterSlice { Value = 7 };
        var expectedName = new NameSlice { Name = "Specific Name" };

        SetupDispatcher();
        Dispatch<CounterSlice, CounterSliceAction>(counterAction);
        Dispatch<NameSlice, NameAction>(nameAction);
        
        Verify(expectedCounter);
        Verify(expectedName);
    }

    [Fact]
    public void DispatcherBeAbleToKeepLastValue()
    {
        var action1 = new NameAction("Premier Nom");
        var action2 = new NameAction("Deuxième Nom");
        var action3 = new NameAction("Nom Final");
        var expected = new NameSlice { Name = "Nom Final" };

        SetupDispatcher();
        Dispatch<NameSlice, NameAction>(action1);
        Dispatch<NameSlice, NameAction>(action2);
        Dispatch<NameSlice, NameAction>(action3);
        
        Verify(expected);
    }

    [Fact]
    public void DispatcherBeAbleToBeTypeSafe()
    {
        var counterAction = new CounterSliceAction(42);
        var nameAction = new NameAction("TypeSafe");
        var expectedCounter = new CounterSlice { Value = 42 };
        var expectedName = new NameSlice { Name = "TypeSafe" };

        SetupDispatcher();
        Dispatch<CounterSlice, CounterSliceAction>(counterAction);
        Dispatch<NameSlice, NameAction>(nameAction);
        
        Verify(expectedCounter);
        Verify(expectedName);
    }

    [Fact]
    public void DispatcherNotBeAbleToDispatchWhenSliceNotInStore()
    {
        var action = new CounterSliceAction(5);

        SetupDispatcherWithoutCounterSlice();
        
        Assert.Throws<ArgumentNullException>(() =>
            Dispatch<CounterSlice, CounterSliceAction>(action));
    }

    private void SetupDispatcher(params ISlice[] slices)
    {
        var services = new ServiceCollection();
        var defaultSlices = slices.Length == 0 
            ? new ISlice[] { new CounterSlice(), new NameSlice() }
            : slices;

        services.AddRedux(defaultSlices);
        services.AddReducers(Assembly.GetExecutingAssembly());

        var serviceProvider = services.BuildServiceProvider();
        _sut = serviceProvider.GetRequiredService<IDispatcher>();
        _store = serviceProvider.GetRequiredService<Store>();
    }

    private void SetupDispatcherWithoutCounterSlice()
    {
        var services = new ServiceCollection();
        services.AddRedux(new NameSlice());
        services.AddReducers();

        var serviceProvider = services.BuildServiceProvider();
        _sut = serviceProvider.GetRequiredService<IDispatcher>();
        _store = serviceProvider.GetRequiredService<Store>();
    }

    private void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        _sut.Dispatch<TSlice, TAction>(action);
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

#region Test Materials

#endregion