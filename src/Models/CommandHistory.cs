using System.Diagnostics.CodeAnalysis;

namespace PSLauncher.Models;

internal class CommandHistory
{
    public record HistoryInfo(int Id, string CommandLine, DateTime Timestamp);

    private readonly List<HistoryInfo> _history = [];

    private int _lastId;
    private int _currentIndex;

    public void Add(string commandLine, DateTime timestamp)
    {
        if (timestamp.Kind is not DateTimeKind.Utc)
            timestamp = timestamp.ToUniversalTime();

        _history.Add(new(++_lastId, commandLine, timestamp));
    }

    public bool TryGetPrevious([MaybeNullWhen(false)] out HistoryInfo historyInfo)
    {
        historyInfo = default;
        if (_currentIndex <= 0)
            return false;

        historyInfo = _history[--_currentIndex];
        return true;
    }

    public bool TryGetNext([MaybeNullWhen(false)] out HistoryInfo historyInfo)
    {
        historyInfo = default;
        if (_currentIndex >= _history.Count - 1)
            return false;

        historyInfo = _history[++_currentIndex];
        return true;
    }

    internal void ResetIndex()
    {
        _currentIndex = _history.Count;
    }
    internal void Clear()
    {
        _history.Clear();
        _lastId = 0;
        _currentIndex = 0;
    }
}
