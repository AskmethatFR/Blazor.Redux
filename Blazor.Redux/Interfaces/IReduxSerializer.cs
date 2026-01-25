using Blazor.Redux.Core;
using Blazor.Redux.Serialization;

namespace Blazor.Redux.Interfaces;

public interface IReduxSerializer
{
    SerializedAction SerializeAction(IAction action);
    SerializedState SerializeState(RootStateSnapshot snapshot);
    IAction DeserializeAction(string json);
    RootStateSnapshot DeserializeState(string json);
}
