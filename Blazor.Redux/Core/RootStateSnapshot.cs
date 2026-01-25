using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

public sealed record RootStateSnapshot(IReadOnlyDictionary<Type, ISlice> Slices);
