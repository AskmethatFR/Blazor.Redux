using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Applies a saved root state snapshot to the store, replacing all slice state.
/// Used for hydration, time-travel debugging, or state restoration.
/// </summary>
public interface IStateSnapshotApplier
{
    /// <summary>
    /// Applies the given snapshot to the store.
    /// When <paramref name="strictValidation"/> is true, the snapshot must contain
    /// exactly the same slice types as the current store.
    /// When false, unknown slice types in the snapshot are still rejected,
    /// but missing current slices are allowed.
    /// </summary>
    /// <param name="snapshot">Root state snapshot to apply.</param>
    /// <param name="strictValidation">If true, requires exact slice set match.</param>
    void ApplySnapshot(RootStateSnapshot snapshot, bool strictValidation = true);
}
