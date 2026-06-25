using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

/// <summary>
/// Immutable snapshot of the complete root state at a point in time.
/// Contains all registered slices keyed by their type.
/// </summary>
/// <param name="Slices">Read-only dictionary of all slice states, keyed by slice type.</param>
public sealed record RootStateSnapshot(IReadOnlyDictionary<Type, ISlice> Slices);
