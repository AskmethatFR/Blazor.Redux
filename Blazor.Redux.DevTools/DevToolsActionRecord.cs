namespace Blazor.Redux.DevTools;

public record DevToolsAction
{
    public string Type { get; init; }
    public string SliceType { get; init; }
    public object? Payload { get; init; }
    public object? Meta { get; init; }
    public DateTime Timestamp { get; init; }

    public DevToolsAction(string type, string sliceType, object? payload = null, object? meta = null)
    {
        Type = type;
        SliceType = sliceType;
        Payload = payload;
        Meta = meta;
        Timestamp = DateTime.UtcNow;
    }
}