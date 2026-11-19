using System.Diagnostics.CodeAnalysis;

namespace PSLauncher.Models;

public interface ITreeItem
{
    int Index { get; }
    ITreeItem? Parent { get; }
    string Name { get; }
    object? RawValue { get; }
    int Level { get; set; }
    bool HasChildren { get; set; }
    bool IsExpaned { get; set; }
    ObjectNodeType NodeType { get; }
    ObjectValueType ValueType { get; }
    void SetParent(ITreeItem parent);
    string GetColumnValue(string? columnName);
    /// <summary>
    /// Get child nodes
    /// </summary>
    /// <returns></returns>
    IEnumerable<ITreeItem> GetChildren();
    [MemberNotNullWhen(true, nameof(RawValue))]
    bool HasAvailableValue { get; }
}

public interface IResultData<T> : ITreeItem where T : ITreeItem
{
    /// <summary>
    /// Sort functions by each column
    /// </summary>
    static abstract Dictionary<string, Func<IEnumerable<ITreeItem>, bool, IEnumerable<ITreeItem>>> Sorters { get; }
}
