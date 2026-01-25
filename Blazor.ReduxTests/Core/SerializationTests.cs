using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;
using Blazor.Redux.Serialization;

namespace Blazor.ReduxTests.Core;

public class SerializationTests
{
    [Fact]
    public void SerializerRoundTripsAction()
    {
        var serializer = new ReduxJsonSerializer();
        var action = new TestAction(7, "payload");

        var json = System.Text.Json.JsonSerializer.Serialize(serializer.SerializeAction(action));
        var deserialized = serializer.DeserializeAction(json);

        Assert.IsType<TestAction>(deserialized);
        var typed = (TestAction)deserialized;
        Assert.Equal(action.Amount, typed.Amount);
        Assert.Equal(action.Message, typed.Message);
    }

    [Fact]
    public void SerializerRoundTripsState()
    {
        var serializer = new ReduxJsonSerializer();
        var slices = new Dictionary<Type, ISlice>
        {
            { typeof(TestSliceA), new TestSliceA { Value = 5 } },
            { typeof(TestSliceB), new TestSliceB { Name = "Redux" } }
        };
        var snapshot = new RootStateSnapshot(slices);

        var json = System.Text.Json.JsonSerializer.Serialize(serializer.SerializeState(snapshot));
        var deserialized = serializer.DeserializeState(json);

        Assert.Equal(2, deserialized.Slices.Count);
        Assert.Equal(5, ((TestSliceA)deserialized.Slices[typeof(TestSliceA)]).Value);
        Assert.Equal("Redux", ((TestSliceB)deserialized.Slices[typeof(TestSliceB)]).Name);
    }
}

public record TestAction(int Amount, string Message) : IAction;

public record TestSliceA : ISlice
{
    public int Value { get; init; }
}

public record TestSliceB : ISlice
{
    public string Name { get; init; } = string.Empty;
}
