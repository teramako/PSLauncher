using System.Diagnostics.CodeAnalysis;

namespace PSLauncher;

public static class PSObjectsMembers
{
    private static readonly Dictionary<Type, Dictionary<string, GridColumn>> _members = [];

    public record class GridColumn(string Name, string Header, int DisplayIndex = -1, bool Enalbed = true, bool Visible = true)
    {
        public GridColumn(string name, int displayIndex = -1, bool enalbed = true, bool visible = true)
            : this(name, name, displayIndex, enalbed, visible)
        {
        }
    }

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
