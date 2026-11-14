using System.Collections;

namespace PSLauncher.Models;

/// <summary>
/// Container for a table view.
/// </summary>
public class TableList : IObjectList
{
    public required string Description { get; init; }
    public object? RootObject { get; init; }
    public required List<object?> Items { get; init; }
    ICollection IObjectList.Items => Items;
    public int Count => Items.Count;
    public ResultDataKind Kind => ResultDataKind.Table;
    public required Type DataType { get; init; }

    /// <summary>
    /// Build <seealso cref="TableList"/> from single object.
    /// </summary>
    /// <remarks>
    /// if <paramref name="obj"/> is a <seealso cref="String"/> or a primitive value (eg. <see cref="int"/>), 
    /// </remarks>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static TableList? Build(object? obj)
    {
        if (obj is null)
            return null;

        var data = obj;

        if (data is IEnumerable enumerable)
            return Build(enumerable);

        var t = Utils.GetDataType(data);
        if (t == typeof(ValueType))
        {
            data = new ValueData(obj);
        }
        else if (t == typeof(string))
        {
            data = new ValueData(obj);
            t = typeof(ValueType);
        }

        return new()
        {
            Description = Utils.GetTypeName(t),
            RootObject = obj,
            Items = [data],
            DataType = t
        };
    }

    /// <summary>
    /// Build <seealso cref="TableList"/> from multiple objects.
    /// </summary>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static TableList? Build(IEnumerable enumerable)
    {
        List<object?> list = [];
        HashSet<Type> types = [];
        foreach (var obj in enumerable)
        {
            list.Add(obj);
            var t = Utils.GetDataType(obj);
            if (t == typeof(string))
            {
                t = typeof(ValueType);
            }
            types.Add(t);
        }

        if (list.Count == 0)
            return null;

        if (types.Count == 1)
        {
            var t = types.First();
            if (t == typeof(ValueType))
            {
                return new()
                {
                    Description = "Values",
                    RootObject = enumerable,
                    Items = [.. list.Select(x => new ValueData(x))],
                    DataType = t
                };
            }
            return new()
            {
                Description = Utils.GetTypeName(t),
                RootObject = enumerable,
                Items = list,
                DataType = t
            };
        }

        return new()
        {
            Description = "Objects",
            RootObject = enumerable,
            Items = [.. list.Select(x => new ValueData(x))],
            DataType = typeof(object)
        };
    }
}
