namespace Blazor.Redux.Interfaces;

/// <summary>
/// Specialized marker interface for an epic — an <see cref="IEffect"/>
/// that transforms action streams using Rx operators.
/// Epics are long-running stream transformations that observe actions
/// and emit new actions, typically handling complex async workflows.
/// </summary>
public interface IEpic : IEffect
{
}
