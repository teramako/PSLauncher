using System.Collections;
using System.ComponentModel;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace PSLauncher.Models;

public class ResultData : INotifyPropertyChanged, IResultData
{
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

    public ResultData(string name, object? rawValue)
    {
        Name = name;
        RawValue = Utils.IsRefStructObject(rawValue) ? null : rawValue;
        if (rawValue is null)
        {
            Type = "(null)";
        }
        else
        {
            RawType = rawValue.GetType();
            Type = RawType is null ? "(null)" : Utils.GetTypeName(RawType);
            (Value, HasChildren) = ValueToString(rawValue);
        }
    }
    public ResultData(string name, object? rawValue, Type? type)
    {
        Name = name;
        RawValue = Utils.IsRefStructObject(rawValue) ? null : rawValue;
        RawType = type;
        Type = type is null ? "(null)" : Utils.GetTypeName(type);
        (Value, HasChildren) = ValueToString(rawValue);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static (string, bool) ValueToString(object? value)
    {
        return value switch
        {
            null => (string.Empty, false),
            string str => (Utils.QuotedPrintable(str), false),
            int or uint or long or ulong or short or ushort or byte or sbyte or float or double or decimal or bool
                => ($"{value}", false),
            Enum enumVal => ($"{enumVal:G} (0x{enumVal:X})", false),
            Array array => ($"{{ Length = {array.Length} }}", array.Length > 0),
            PSObject pso => ($"PSObject({pso.TypeNames[0]})", true),
            PSCredential credential => ($"PSCredential [{credential.UserName}]", false),
            SecureString secureString => ($"SecureString", false),
            IDictionary dictionary => ($"{{ Count = {dictionary.Count} }}", dictionary.Count > 0),
            ICollection collection => ($"{{ Count = {collection.Count} }}", collection.Count > 0),
            Exception ex => ($"{ex.Message}", true),
            _ => ($"{value}", true),
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
            yield return new($"[{index++}]", Utils.GetBaseObject(item));
        }
    }

    private static IEnumerable<ResultData> ExtractArray(Array array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            yield return new($"[{i}]", Utils.GetBaseObject(array.GetValue(i)));
        }
    }

    private static IEnumerable<ResultData> ExtractDictionaryObject(IDictionary dictionary)
    {
        foreach (var key in dictionary.Keys)
        {
            var value = dictionary[key];
            yield return new($"{key}", Utils.GetBaseObject(value));
        }
    }

    private static IEnumerable<ResultData> ExtractPSObject(PSObject pso)
    {
        foreach (var property in pso.Properties)
        {
            if (property.MemberType is PSMemberTypes.Method or PSMemberTypes.ScriptMethod or PSMemberTypes.ScriptProperty)
            {
                continue; // Skip methods
            }
            yield return new(property.Name, Utils.GetBaseObject(property.Value));
        }
    }

    private static IEnumerable<ResultData> ExtractGenericObject(object obj)
    {
        var type = obj.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            ResultData row;
            try
            {
                var value = property.GetValue(obj);
                row = new(property.Name, Utils.GetBaseObject(value));
            }
            catch (Exception ex)
            {
                row = new(property.Name, ex, property.DeclaringType);
            }
            yield return row;
        }
    }
}
