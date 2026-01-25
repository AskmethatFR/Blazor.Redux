using System.Text.Json;

namespace Blazor.Redux.Serialization;

public sealed record SerializedSlice(string Type, JsonElement Data);

public sealed record SerializedState(List<SerializedSlice> Slices, string Version = "1");
