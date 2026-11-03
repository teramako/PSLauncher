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
    void SetParent(IResultData parent);
    string GetColumnValue(string? columnName);
}

public interface IResultData<T> : IResultData where T : IResultData
{
    /// <summary>
    /// Extract the child nodes of <paramref name="data"/>
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    static abstract IEnumerable<T> ExtractObject(IResultData data);

    /// <summary>
    /// Sort functions by each column
    /// </summary>
    static abstract Dictionary<string, Func<IEnumerable<IResultData>, bool, IEnumerable<IResultData>>> Sorters { get; }
}
