namespace PSLauncher.Models;

public interface IResultData
{
    int Index { get; }
    IResultData? Parent { get; }
    string Name { get; }
    object? RawValue { get; }
    int Level { get; set; }
    bool HasChildren { get; set; }
    bool IsExpaned { get; set; }
    ObjectNodeType NodeType { get; }
    ObjectValueType ValueType { get; }
    Type DataType { get; }
    void SetParent(IResultData parent);
    string GetColumnValue(string? columnName);
    /// <summary>
    /// Get child nodes
    /// </summary>
    /// <returns></returns>
    IEnumerable<IResultData> GetChildren();
}

public interface IResultData<T> : IResultData where T : IResultData
{
    /// <summary>
    /// Sort functions by each column
    /// </summary>
    static abstract Dictionary<string, Func<IEnumerable<IResultData>, bool, IEnumerable<IResultData>>> Sorters { get; }
}
