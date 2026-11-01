namespace PSLauncher.Models;

public enum ObjectNodeType
{
    [Icon("PropertyNodeIcon")]
    Property,
    [Icon("FieldNodeIcon")]
    Field,
    [Icon("IndexNodeIcon")]
    Index,
    [Icon("VirtualRowIcon")]
    VirtualRow,
}
