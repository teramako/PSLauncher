using System.Diagnostics.CodeAnalysis;

namespace PSLauncher;

public static class PSObjectsMembers
{
    private static readonly Dictionary<Type, Dictionary<string, GridColumn>> _members = [];

    public static void Add(Type type, Dictionary<string, GridColumn> columns)
        => _members[type] = columns;
    public static void Add(Type type, IEnumerable<GridColumn> columns)
        => _members[type] = columns.ToDictionary(col => col.Name);
    public static void Add<T>(IEnumerable<GridColumn> columns)
        => Add(typeof(T), columns);

    public static bool TryGetColumns(Type type, [MaybeNullWhen(false)] out Dictionary<string, GridColumn> columns)
        => _members.TryGetValue(type, out columns);
    public static bool TryGetColumns<T>([MaybeNullWhen(false)] out Dictionary<string, GridColumn> columns)
        => TryGetColumns(typeof(T), out columns);
}

/// <summary>
/// Defines the name, header, display order, and enabled and visibility states of a grid column.
/// </summary>
/// <remarks>If no header is specified, the column name is used as the header.</remarks>
public record class GridColumn()
{
    public GridColumn(string name, int displayIndex = -1, bool enabled = true, bool visible = true)
        : this(name, name, displayIndex, enabled, visible)
    {
    }
    public GridColumn(string name, string header, int displayIndex = -1, bool enabled = true, bool visible = true)
        : this()
    {
        Name = name;
        Header = header;
        DisplayIndex = displayIndex;
        Enabled = enabled;
        Visible = visible;
    }
    public required string Name { get; init; }
    public string Header
    {
        get => string.IsNullOrEmpty(field) ? Name : field;
        init;
    }
    public int DisplayIndex { get; init; } = -1;
    public bool Enabled { get; init; } = true;
    public bool Visible { get; init; } = true;
}
