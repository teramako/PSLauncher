using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace PSLauncher;

public static class PSObjectsMembers
{
    private static readonly Dictionary<Type, Dictionary<string, GridColumn>> _members = [];

    public static IReadOnlyCollection<Type> Types => _members.Keys;

    public static void Add(Type type, Dictionary<string, GridColumn> columns)
        => _members[type] = columns;
    public static void Add(Type type, IEnumerable<GridColumn> columns)
        => _members[type] = columns.ToDictionary(col => col.Name);
    public static void Add<T>(IEnumerable<GridColumn> columns)
        => Add(typeof(T), columns);

    public static void Add<T>(IEnumerable columns)
    {
        Add<T>(GetColumns(columns));

        static IEnumerable<GridColumn> GetColumns(IEnumerable columns)
        {
            foreach (var obj in columns)
                if (LanguagePrimitives.TryConvertTo<GridColumn>(obj, out var column))
                    yield return column;
        }
    }

    public static bool TryGetColumns(Type type, [MaybeNullWhen(false)] out Dictionary<string, GridColumn> columns)
        => _members.TryGetValue(type, out columns);
    public static bool TryGetColumns<T>([MaybeNullWhen(false)] out Dictionary<string, GridColumn> columns)
        => TryGetColumns(typeof(T), out columns);
    public static ReadOnlyCollection<PSGridColumn>? GetPSGridColumns(Type type)
    {
        if (TryGetColumns(type, out var dict))
        {
            return [.. dict.Values.Select(col => new PSGridColumn(type, col))];
        }
        return null;
    }
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

/// <summary>
/// Represents a grid column and its associated .NET type.
/// </summary>
/// <param name="Type">The .NET type associated with the grid column.</param>
/// <param name="Column">The grid column definition.</param>
public record class PSGridColumn(Type Type, GridColumn Column)
{
    public Type Type { get; } = Type;
    private readonly GridColumn Column = Column;
    public string Name => Column.Name;
    public string Header => Column.Header;
    public int DisplayIndex => Column.DisplayIndex;
    public bool Enabled => Column.Enabled;
    public bool Visible => Column.Visible;
}
