namespace Blazor.Redux.Core;

public enum SnapshotStrategy
{
    DeepCopy,
    [Obsolete("Reference mode exposes mutable state through snapshots. Use DeepCopy.")]
    Reference
}
