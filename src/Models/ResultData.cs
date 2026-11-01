using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace PSLauncher.Models;

public class ResultData : INotifyPropertyChanged, IResultData
{
    public int Index { get; }
    public IResultData? Parent { get; private set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public Type? RawType { get; }
    public string Value { get; set; } = string.Empty;
    public object? RawValue { get; }

    public int Level { get; set; } = 0;

    public bool HasChildren { get; set; }

    public bool IsExpaned
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(IsExpaned)));
        }
    }
    public ObjectNodeType NodeType { get; }
    public ObjectValueType ValueType { get; } = ObjectValueType.Null;

    public ResultData(int index, object? rawValue)
        : this($"[{index}]", rawValue, ObjectNodeType.Index, index)
    {
    }
    public ResultData(string name, object? rawValue, ObjectNodeType nodeType, int index)
        : this(name, rawValue, rawValue?.GetType(), nodeType, index)
    {
    }
    public ResultData(string name, object? rawValue, Type? type, ObjectNodeType nodeType, int index)
    {
        Index = index;
        Name = name;
        RawValue = Utils.IsRefStructObject(rawValue) ? null : rawValue;
        RawType = type;
        NodeType = nodeType;
        Type = type is null ? "(null)" : Utils.GetTypeName(type);
        (Value, HasChildren, ValueType) = ValueToString(rawValue);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetParent(IResultData parent)
    {
        Parent = parent;
        Level = parent.Level + 1;
    }

    public static (string, bool, ObjectValueType) ValueToString(object? value)
    {
        return value switch
        {
            null => (string.Empty, false, ObjectValueType.Null),
            string str => (Utils.QuotedPrintable(str), false, ObjectValueType.String),
            int or uint or long or ulong or short or ushort or byte or sbyte or float or double or decimal
                => ($"{value}", false, ObjectValueType.Number),
            bool b => ($"{value}", false, ObjectValueType.Boolean),
            Enum enumVal => ($"{enumVal:G} (0x{enumVal:X})", false, ObjectValueType.Enum),
            Array array => ($"{{ Length = {array.Length} }}", array.Length > 0, ObjectValueType.Collection),
            PSObject pso => ($"PSObject({pso.TypeNames[0]})", true, ObjectValueType.Class),
            PSCredential credential => ($"PSCredential [{credential.UserName}]", false, ObjectValueType.Class),
            SecureString secureString => ($"SecureString", false, ObjectValueType.Class),
            IDictionary dictionary => ($"{{ Count = {dictionary.Count} }}", dictionary.Count > 0, ObjectValueType.Dictionary),
            ICollection collection => ($"{{ Count = {collection.Count} }}", collection.Count > 0, ObjectValueType.Collection),
            Exception ex => ($"{ex.Message}", true, ObjectValueType.Exception),
            FileInfo file => ($"{file.FullName}", true, ObjectValueType.File),
            DirectoryInfo dir => ($"{dir.FullName}", true, ObjectValueType.Directory),
            DateTime or DateTimeOffset or TimeSpan => ($"{value}", true, ObjectValueType.Temporal),
            _ => ($"{value}", true, value.GetType().IsValueType ? ObjectValueType.Struct : ObjectValueType.Class)
        };
    }

    public static IEnumerable<ResultData> ExtractObject(object value)
    {
        var obj = Utils.GetBaseObject(value);
        return obj switch
        {
            null or string or int or uint or long or ulong or short or ushort or byte or sbyte or float or double or decimal or bool
                => [],
            Array array => ExtractArray(array),
            IDictionary dictionary => ExtractDictionaryObject(dictionary),
            IEnumerable enumerable => ExtractEnumerableObject(enumerable),
            PSObject pso => ExtractPSObject(pso),
            _ => ExtractGenericObject(obj)
        };
    }

    private static IEnumerable<ResultData> ExtractEnumerableObject(IEnumerable enumerable)
    {
        int index = 0;
        foreach (var item in enumerable)
        {
            yield return new(index++, Utils.GetBaseObject(item));
        }
    }

    private static IEnumerable<ResultData> ExtractArray(Array array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            yield return new(i, Utils.GetBaseObject(array.GetValue(i)));
        }
    }

    private static IEnumerable<ResultData> ExtractDictionaryObject(IDictionary dictionary)
    {
        int index = 0;
        foreach (var key in dictionary.Keys)
        {
            var value = dictionary[key];
            yield return new($"{key}", Utils.GetBaseObject(value), ObjectNodeType.Index, index++);
        }
    }

    private static IEnumerable<ResultData> ExtractPSObject(PSObject pso)
    {
        int index = 0;
        foreach (var property in pso.Properties)
        {
            if (property.MemberType is PSMemberTypes.Method or PSMemberTypes.ScriptMethod or PSMemberTypes.ScriptProperty)
            {
                continue; // Skip methods
            }
            yield return new(property.Name, Utils.GetBaseObject(property.Value), ObjectNodeType.Property, index++);
        }
    }

    private static IEnumerable<ResultData> ExtractGenericObject(object obj)
    {
        int index = 0;
        var type = obj.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            ResultData row;
            try
            {
                var value = property.GetValue(obj);
                row = new(property.Name, Utils.GetBaseObject(value), ObjectNodeType.Property, index++);
            }
            catch (Exception ex)
            {
                row = new(property.Name, ex, property.DeclaringType, ObjectNodeType.Property, index++);
            }
            yield return row;
        }
    }

    public static readonly Dictionary<string, Func<IEnumerable<IResultData>, bool, IEnumerable<IResultData>>> Sorters = new() {
        ["Name"] = static (items, asc) => ResultList.SortTreeItems(items, static (x, y) =>
        {
            if (x.NodeType == y.NodeType)
            {
                return x.NodeType is ObjectNodeType.Index
                       ? x.Index.CompareTo(y.Index)
                       : x.Name.CompareTo(y.Name);
            }
            return x.NodeType.CompareTo(y.NodeType);
        }, asc),
        ["Type"] = (items, asc) => ResultList.SortTreeItems(items.OfType<ResultData>(), static x => x.Type, asc),
    };
}
