namespace Blazor.Redux.Core;

public sealed class DispatchQueue : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AsyncLocal<int> _recursionDepth = new();

    public void Execute(Action action)
    {
        var isReentrant = _recursionDepth.Value > 0;

        if (!isReentrant)
        {
            _gate.Wait();
        }

        _recursionDepth.Value++;

        try
        {
            action();
        }
        finally
        {
            _recursionDepth.Value--;

            if (!isReentrant)
            {
                _gate.Release();
            }
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        var isReentrant = _recursionDepth.Value > 0;

        if (!isReentrant)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
        }

        _recursionDepth.Value++;

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _recursionDepth.Value--;

            if (!isReentrant)
            {
                _gate.Release();
            }
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
