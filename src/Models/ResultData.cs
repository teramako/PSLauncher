using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace PSLauncher.Models;

public class ResultData : INotifyPropertyChanged, IResultData, IResultData<ResultData>
{
    public int Index { get; }
    public IResultData? Parent { get; private set; }
    public string Name { get; set; }
    public string Type { get; set; }
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
    public ResultData(string name, object? rawValue, ObjectNodeType nodeType, int index = 0)
        : this(name, rawValue, rawValue?.GetType(), nodeType, index)
    {
    }
    public ResultData(string name, object? rawValue, Type? type, ObjectNodeType nodeType, int index)
    {
        Index = index;
        Name = name;
        RawValue = Utils.IsRefStructObject(rawValue) ? null : rawValue;
        NodeType = nodeType;
        Type = type is null ? "(null)" : Utils.GetTypeName(type);
        (Value, HasChildren, ValueType) = ValueToString(rawValue);
    }
    public ResultData(int index, string name, object? rawValue, string value, string type, ObjectNodeType nodeType, ObjectValueType valueType, bool hasChildren)
    {
        Index = index;
        Name = name;
        RawValue = Utils.IsRefStructObject(rawValue) ? null : rawValue;
        Value = value;
        Type = type;
        ValueType = valueType;
        NodeType = nodeType;
        HasChildren = hasChildren;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetParent(IResultData parent)
    {
        Parent = parent;
        Level = parent.Level + 1;
    }

    public string GetColumnValue(string? columnName) => columnName switch
    {
        "Name" => Name,
        "Value" => Utils.ObjectToString(RawValue),
        "Type" => Type,
        _ => string.Empty
    };

    public IEnumerable<IResultData> GetChildren()
    {
        ExpandType expandType = this is VirtualRowData virtualRow ? virtualRow.ExpandType : ExpandType.Default;
        foreach (var child in ExtractObject(RawValue, expandType))
        {
            child.SetParent(this);
            yield return child;
        }
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

    public static IEnumerable<ResultData> ExtractObject(object? value, ExpandType expandType = ExpandType.Default)
    {
        var obj = Utils.GetBaseObject(value);
        return obj switch
        {
            null or string or int or uint or long or ulong or short or ushort or byte or sbyte or float or double or decimal or bool
                => [],
            Array array => ExtractArray(array),
            IDictionary dictionary => ExtractDictionaryObject(dictionary, expandType),
            IEnumerable enumerable => ExtractEnumerableObject(enumerable, expandType),
            PSObject pso => ExtractPSObject(pso),
            _ => ExtractGenericObject(obj)
        };
    }

    private static IEnumerable<ResultData> ExtractEnumerableObject(IEnumerable enumerable,
                                                                   ExpandType expandType = ExpandType.Default)
    {
        int index = 0;
        switch (expandType)
        {
            case ExpandType.Default:
                yield return new VirtualRowData("Members", enumerable, -1, ExpandType.Members);
                foreach (var item in enumerable)
                {
                    yield return new(index++, Utils.GetBaseObject(item));
                }
                break;
            case ExpandType.Members:
                foreach (var memberData in ExtractGenericObject(enumerable))
                    yield return memberData;
                break;
        }
    }

    private static IEnumerable<ResultData> ExtractArray(Array array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            yield return new(i, Utils.GetBaseObject(array.GetValue(i)));
        }
    }

    private static IEnumerable<ResultData> ExtractDictionaryObject(IDictionary dictionary,
                                                                   ExpandType expandType = ExpandType.Default)
    {
        int index = 0;
        switch (expandType)
        {
            case ExpandType.Default:
                yield return new VirtualRowData("Members", dictionary, -1, ExpandType.Members);
                foreach (DictionaryEntry entry in dictionary)
                {
                    yield return new($"{entry.Key}", Utils.GetBaseObject(entry.Value), ObjectNodeType.Index, index++);
                }
                break;
            case ExpandType.Members:
                foreach (var memberData in ExtractGenericObject(dictionary))
                    yield return memberData;
                break;
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
        int index = int.MinValue;
        var type = obj.GetType();
        foreach (var member in type.GetMembers())
        {
            switch (member)
            {
                case PropertyInfo prop:
                    if (prop.GetIndexParameters().Length > 0) continue; // Skip indexers
                    yield return CreatePropertyData(prop);
                    continue;
                case FieldInfo field:
                    yield return CreateFieldData(field);
                    continue;
                default:
                    continue;
            }
        }

        ResultData CreatePropertyData(PropertyInfo property)
        {
            try
            {
                var value = property.GetValue(obj);
                return new(property.Name, Utils.GetBaseObject(value), ObjectNodeType.Property, index++);
            }
            catch (Exception ex)
            {
                return new(property.Name, ex, property.PropertyType, ObjectNodeType.Property, index++);
            }
        }
        ResultData CreateFieldData(FieldInfo field)
        {
            try
            {
                var value = field.GetValue(obj);
                return new(field.Name, Utils.GetBaseObject(value), ObjectNodeType.Field, index++);
            }
            catch (Exception ex)
            {
                return new(field.Name, ex, field.FieldType, ObjectNodeType.Field, index++);
            }
        }
    }

    public static Dictionary<string, Func<IEnumerable<IResultData>, bool, IEnumerable<IResultData>>> Sorters { get; } = new() {
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
