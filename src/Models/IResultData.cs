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
}
