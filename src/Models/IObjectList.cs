using System.Collections;

namespace PSLauncher.Models;

public interface IObjectList
{
    string Description { get; }
    string DisplayData { get; }
    public bool HasDisplayData { get; }
    object? RootObject { get; }
    ICollection Items { get; }
    int Count { get; }
    ResultDataKind Kind { get; }
}
