namespace Blazor.Redux.Core;

public sealed class DispatchQueue : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

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
