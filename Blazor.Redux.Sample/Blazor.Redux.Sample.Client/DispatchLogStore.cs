namespace Blazor.Redux.Sample.Client;

public sealed class DispatchLogStore
{
    private readonly List<string> _entries = new();

    public IReadOnlyList<string> Entries => _entries;

    public event Action? Changed;

    public void Add(string entry)
    {
        _entries.Add(entry);
        Changed?.Invoke();
    }

    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }
}
