using System.Collections;

namespace PSLauncher.Models;

public interface IResultList
{
    string Description { get; }
    object? RootObject { get; }
    ICollection Items { get; }
    int Count { get; }
    ResultDataKind Kind { get; }
}
