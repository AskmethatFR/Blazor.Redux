namespace Blazor.Redux.Core;

/// <summary>
/// Thread-safe serial dispatch queue that ensures actions are processed
/// one at a time in FIFO order, preventing concurrent state mutations.
/// Provides both synchronous and asynchronous execution paths.
/// </summary>
public sealed class DispatchQueue : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Executes an action synchronously on the dispatch queue.
    /// Blocks the calling thread until the action completes.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    public void Execute(Action action)
    {
        _gate.Wait();
        try
        {
            action();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Executes an async action on the dispatch queue.
    /// Awaits the action before releasing the queue.
    /// </summary>
    /// <param name="action">Async action to execute.</param>
    public async Task ExecuteAsync(Func<Task> action)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
