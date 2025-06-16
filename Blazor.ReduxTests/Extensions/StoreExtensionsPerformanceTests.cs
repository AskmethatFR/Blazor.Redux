using System.Diagnostics;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;
using Xunit.Abstractions;

namespace Blazor.ReduxTests.Extensions;

public class StoreExtensionsPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public StoreExtensionsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void UpdateShouldBePerformantForManyOperations()
    {
        // Arrange
        var initialSlice = new CounterSlice { Value = 0 };
        var store = Store.Init(initialSlice);
        const int iterations = 10000;

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            initialSlice = store.UpdateSlice(initialSlice with { Value = initialSlice.Value + 1 });
        }
        
        stopwatch.Stop();

        // Assert
        var finalSlice = store.GetSlice<CounterSlice>();
        Assert.NotNull(finalSlice);
        Assert.Equal(iterations, finalSlice.Value);

        _output.WriteLine($"Temps pour {iterations} updates: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Moyenne par update: {stopwatch.ElapsedTicks / (double)iterations} ticks");

        // Performance assertion (ajuster selon les besoins)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Updates trop lentes: {stopwatch.ElapsedMilliseconds}ms pour {iterations} opérations");
    }

    [Fact]
    public void UpdateShouldHandleComplexObjectsEfficiently()
    {
        // Arrange
        var initialSlice = new ComplexSlice 
        { 
            Counter = new CounterSlice { Value = 0 },
            Items = Enumerable.Range(0, 1000).Select(i => $"item{i}").ToList(),
            Metadata = Enumerable.Range(0, 100).ToDictionary(i => $"key{i}", i => (object)i)
        };
        var store = Store.Init(initialSlice);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 100; i++)
        {
            initialSlice = store.UpdateSlice<ComplexSlice>(initialSlice with 
            {
                Counter = initialSlice.Counter with { Value = initialSlice.Counter.Value + 1 },
                Items = [.. initialSlice.Items, $"newitem{i}"]
            });
        }
        
        stopwatch.Stop();

        // Assert
        var finalSlice = store.GetSlice<ComplexSlice>();
        Assert.NotNull(finalSlice);
        Assert.Equal(100, finalSlice.Counter.Value);
        Assert.Equal(1100, finalSlice.Items.Count);

        _output.WriteLine($"Temps pour 100 updates complexes: {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            "Updates d'objets complexes trop lentes");
    }
}

public record ComplexSlice : ISlice
{
    public CounterSlice Counter { get; set; }
    public List<string> Items { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}