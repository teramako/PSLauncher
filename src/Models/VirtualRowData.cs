namespace PSLauncher.Models;

public class VirtualRowData(string name, object? rawValue, int index, ExpandType expandType)
    : ResultData(index, name, rawValue, string.Empty, string.Empty, ObjectNodeType.VirtualRow, ObjectValueType.Null, true)
{
    public ExpandType ExpandType { get; } = expandType;
}
