using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

public interface IStateSnapshotApplier
{
    void ApplySnapshot(RootStateSnapshot snapshot, bool strictValidation = true);
}
