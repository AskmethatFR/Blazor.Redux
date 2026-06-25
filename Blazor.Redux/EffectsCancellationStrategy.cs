namespace Blazor.Redux;

/// <summary>
/// Strategy for cancelling active effect operations when new actions arrive.
/// </summary>
public enum EffectsCancellationStrategy
{
    /// <summary>
    /// No automatic cancellation. Effects run concurrently; previous operations
    /// are not cancelled when new actions arrive.
    /// </summary>
    None = 0,

    /// <summary>
    /// Uses Rx <c>Switch()</c> operator to cancel the previous effect
    /// subscription when a new action triggers the next effect cycle.
    /// Only applies to effects implementing <see cref="Interfaces.ICancelableEffect"/>.
    /// </summary>
    RxSwitch = 1
}
