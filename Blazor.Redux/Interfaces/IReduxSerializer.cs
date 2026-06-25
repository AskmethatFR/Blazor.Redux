using Blazor.Redux.Core;
using Blazor.Redux.Serialization;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Serializes and deserializes Redux actions and state for persistence,
/// hydration, or transmission across boundaries.
/// </summary>
public interface IReduxSerializer
{
    /// <summary>
    /// Serializes an action into a portable format.
    /// </summary>
    /// <param name="action">Action to serialize.</param>
    /// <returns>Serialized action data.</returns>
    SerializedAction SerializeAction(IAction action);

    /// <summary>
    /// Serializes a root state snapshot into a portable format.
    /// </summary>
    /// <param name="snapshot">Root state snapshot to serialize.</param>
    /// <returns>Serialized state data.</returns>
    SerializedState SerializeState(RootStateSnapshot snapshot);

    /// <summary>
    /// Deserializes an action from its serialized JSON representation.
    /// </summary>
    /// <param name="json">JSON string of a serialized action.</param>
    /// <returns>Deserialized action instance.</returns>
    IAction DeserializeAction(string json);

    /// <summary>
    /// Deserializes a root state snapshot from its serialized JSON representation.
    /// </summary>
    /// <param name="json">JSON string of a serialized state.</param>
    /// <returns>Deserialized root state snapshot.</returns>
    RootStateSnapshot DeserializeState(string json);
}
