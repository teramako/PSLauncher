using System.Collections.Concurrent;
using System.Text;

namespace PSLauncher;

internal static class StringBuilderPool
{
    private static readonly ConcurrentBag<StringBuilder> _pool = [];

    public static StringBuilder Rent(int capacity)
    {
        if (_pool.TryTake(out var sb))
        {
            sb.Clear();
            return sb;
        }
        return new(capacity);
    }

    public static void Return(StringBuilder sb) {
        if (sb.Capacity > 2048)
        {
            return;
        }
        _pool.Add(sb);
    }
}
