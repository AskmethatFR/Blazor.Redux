using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;
using Blazor.Redux.Serialization;

namespace Blazor.ReduxTests.Core;

public class SerializationTests
{
    private static readonly Assembly TestAssembly = typeof(SerializationTests).Assembly;

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

    [Fact]
    public void SerializerWithSearchAssembliesRoundTripsAction()
    {
        var serializer = new ReduxJsonSerializer(searchAssemblies: [TestAssembly]);
        var action = new TestAction(3, "secure");

        var json = System.Text.Json.JsonSerializer.Serialize(serializer.SerializeAction(action));
        var deserialized = serializer.DeserializeAction(json);

        Assert.IsType<TestAction>(deserialized);
        Assert.Equal(3, ((TestAction)deserialized).Amount);
    }

    [Fact]
    public void SerializerWithSearchAssembliesRoundTripsState()
    {
        var serializer = new ReduxJsonSerializer(searchAssemblies: [TestAssembly]);
        var slices = new Dictionary<Type, ISlice>
        {
            { typeof(TestSliceA), new TestSliceA { Value = 42 } }
        };
        var snapshot = new RootStateSnapshot(slices);

        var json = System.Text.Json.JsonSerializer.Serialize(serializer.SerializeState(snapshot));
        var deserialized = serializer.DeserializeState(json);

        Assert.Equal(42, ((TestSliceA)deserialized.Slices[typeof(TestSliceA)]).Value);
    }

    [Fact]
    public void SerializerRejectsTypeOutsideAllowedAssemblies()
    {
        var serializer = new ReduxJsonSerializer(searchAssemblies: [TestAssembly]);
        var fakeTypeName = "Some.Fake.Type, Some.Unknown.Assembly";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            serializer.DeserializeAction(
                $"{{\"type\":\"{fakeTypeName}\",\"data\":{{\"value\":1}},\"version\":\"1\"}}"));

        Assert.Contains("Unable to resolve type", ex.Message);
        Assert.Contains("registered assemblies", ex.Message);
    }

    [Fact]
    public void SerializerRejectsTypeMismatch()
    {
        var typeMap = new Dictionary<string, Type>
        {
            { "evil-type", typeof(string) }
        };
        var serializer = new ReduxJsonSerializer(typeMap: typeMap, searchAssemblies: [TestAssembly]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            serializer.DeserializeAction(
                $"{{\"type\":\"evil-type\",\"data\":{{\"value\":1}},\"version\":\"1\"}}"));

        Assert.Contains("does not implement IAction", ex.Message);
    }

    [Fact]
    public void SerializerRejectsMissingTypeName()
    {
        var serializer = new ReduxJsonSerializer(searchAssemblies: [TestAssembly]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            serializer.DeserializeAction(
                $"{{\"type\":\"\",\"data\":{{\"value\":1}},\"version\":\"1\"}}"));

        Assert.Contains("missing", ex.Message);
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
