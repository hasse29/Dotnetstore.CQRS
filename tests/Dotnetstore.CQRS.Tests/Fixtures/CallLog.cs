namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed class CallLog
{
    private readonly Lock _gate = new();
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_gate)
            {
                return [.. _entries];
            }
        }
    }

    public void Add(string entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }
    }
}
