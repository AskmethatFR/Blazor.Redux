namespace Blazor.Redux.Core;

/// <summary>
/// Strategy for creating state snapshots from the store.
/// Controls whether snapshots are deep copies (safe) or reference shares (fast but mutable).
/// </summary>
public enum SnapshotStrategy
{
    /// <summary>
    /// Creates deep copies of all slices in each snapshot.
    /// Safe for external consumers; prevents accidental mutation of store state.
    /// </summary>
    DeepCopy,

    /// <summary>
    /// Shares references to slice objects without copying.
    /// Performance optimization — exposes mutable state through snapshots.
    /// Use <see cref="DeepCopy"/> unless profiling shows a bottleneck.
    /// </summary>
    [Obsolete("Reference mode exposes mutable state through snapshots. Use DeepCopy.")]
    Reference
}
