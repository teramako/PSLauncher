using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Security;

namespace PSLauncher.Models;

public class ResultList
{
    public required object? RootObject { get; init; }
    public string Description { get; init; } = string.Empty;
    public string DisplayData { get; init; } = string.Empty;
    public bool HasDisplayData => !string.IsNullOrEmpty(DisplayData);
    public ResultDataKind Kind { get; init; } = ResultDataKind.Null;
    public ObservableCollection<IResultData>? Items { get; init; }

    /// <summary>
    /// Expands the row at the specified index and inserts its child rows into the collection.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="InvalidDataException"/>
    public void Expand(int index)
    {
        if (Items is null)
            throw new InvalidDataException("Items collection is null.");

        var parentRow = Items[index];
        if (parentRow is null || parentRow.IsExpaned || parentRow.RawValue is null)
            return;
        var parentLevel = parentRow.Level;
        bool hasChilren = false;
        IEnumerable<IResultData> children = Kind is ResultDataKind.Files && parentRow.RawValue is DirectoryInfo dir
            ? FileResultData.ExtractObject(dir)
            : ResultData.ExtractObject(parentRow.RawValue);
        foreach (var child in children)
        {
            child.Level = parentLevel + 1;
            Items.Insert(++index, child);
            hasChilren = true;
        }
        if (hasChilren)
        {
            parentRow.IsExpaned = true;
        }
        else
        {
            parentRow.HasChildren = false;
        }

    }
    /// <summary>
    /// Collapses the row at the specified index and removes its child rows from the collection.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>Index of the parent row</returns>
    /// <exception cref="InvalidDataException"/>
    public int Collapse(int index)
    {
        if (Items is null)
            throw new InvalidDataException("Items collection is null.");

        var parentIndex = index;
        var parentRow = Items[index];
        if (parentRow is null) return -1;
        if (!parentRow.IsExpaned && parentRow.Level > 0)
        {
            while (parentIndex >= 0)
            {
                parentIndex--;
                var prev = Items[parentIndex];
                if (prev is null) return -1;
                if (prev.Level != parentRow.Level)
                {
                    parentRow = prev;
                    break;
                }
            }
        }
        parentRow.IsExpaned = false;
        while (Items.Count > parentIndex + 1)
        {
            var child = Items[parentIndex + 1];
            if (child is null || child.Level <= parentRow.Level)
                break;

            Items.RemoveAt(parentIndex + 1);
        }
        return parentIndex;
    }

    /// <summary>
    /// Toggle expand/collapse the row at the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public bool Toggle(int index)
    {
        if (Items is null)
            throw new InvalidDataException("Items collection is null.");

        if (index < 0)
            return false;
        var row = Items[index];
        if (row is not null)
        {
            if (!row.HasChildren)
                return false;
            if (row.IsExpaned)
            {
                Collapse(index);
            }
            else
            {
                Expand(index);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Build <seealso cref="ResultList"/> instance from results of <seealso cref="PowerShell.Invoke()"/>
    /// </summary>
    /// <param name="psObjects"></param>
    /// <returns></returns>
    public static ResultList? Build(PSDataCollection<PSObject> psObjects)
    {
        int itemCount = psObjects.Count;
        if (itemCount == 0)
        {
            return null;
        }
        if (itemCount == 1)
        {
            return Build(psObjects[0]);
        }

        List<object?> objects = [];
        bool isString = false;
        bool isFileSystemInfo = false;
        foreach (var obj in Utils.GetBaseObjects(psObjects))
        {
            objects.Add(obj);
            isString = obj is string;
            isFileSystemInfo = obj is FileSystemInfo;
        }
        if (isString)
        {
            var strings = objects.OfType<string>();
            return new ResultList
            {
                RootObject = psObjects,
                Description = itemCount > 1 ? $"String {{ Count = {itemCount} }}" : "String",
                DisplayData = string.Join(Environment.NewLine, strings),
                Kind = ResultDataKind.Simple,
            };
        }
        else if (isFileSystemInfo)
        {
            var fileInfos = objects.OfType<FileSystemInfo>();
            return new ResultList
            {
                RootObject = psObjects,
                Items = new(fileInfos.Select(file => new FileResultData(file))),
                Description = itemCount > 1 ? $"FileSystemInfo {{ Count = {itemCount} }}" : "FileSystemInfo",
                Kind = ResultDataKind.Files,
            };
        }

        return new ResultList
        {
            RootObject = objects,
            Items = new(ResultData.ExtractObject(objects)),
            Description = itemCount > 1 ? $"{{ Count = {itemCount} }}" : string.Empty,
            Kind = ResultDataKind.Complex,
        };
    }

    /// <summary>
    /// Build <seealso cref="ResultList"/> instance from a single <seealso cref="PSObject"/>
    /// </summary>
    /// <param name="psObject"></param>
    /// <returns></returns>
    public static ResultList? Build(PSObject? psObject) => Build(Utils.GetBaseObject(psObject));

    /// <summary>
    /// Build <seealso cref="ResultList"/> instance from an object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static ResultList? Build(object? obj)
    {
        if (obj is null)
        {
            return new ResultList
            {
                RootObject = null,
                Kind = ResultDataKind.Null,
                Description = "(null)"
            };
        }
        var baseType = Utils.GetTypeName(obj.GetType());
        return obj switch
        {
            string str => new ResultList
            {
                RootObject = str,
                Kind = ResultDataKind.Simple,
                Description = baseType,
                DisplayData = str
            },
            int or uint or long or ulong or short or ushort or byte or sbyte or float or double or decimal or bool => new ResultList
            {
                RootObject = obj,
                Kind = ResultDataKind.Simple,
                Description = baseType,
                DisplayData = $"{obj}"
            },
            Enum enumVal => new ResultList
            {
                RootObject = obj,
                Kind = ResultDataKind.Simple,
                Description = $"Enum({baseType}) {enumVal:G} (0x{enumVal:X})",
                DisplayData = $"{enumVal}"
            },
            SecureString secureString => new ResultList
            {
                RootObject = obj,
                Kind = ResultDataKind.Simple,
                Description = $"SecureString",
                DisplayData = $"{secureString}"
            },
            PSCredential credential => new ResultList
            {
                RootObject = obj,
                Kind = ResultDataKind.Simple,
                Description = $"PSCredential [{credential.UserName}]",
                DisplayData = $"{credential}"
            },
            PSObject pso => new ResultList
            {
                RootObject = obj,
                Kind = ResultDataKind.Simple,
                Description = $"PSObject({pso.TypeNames[0]})",
                DisplayData = $"{pso}"
            },
            DirectoryInfo dir => new ResultList
            {
                RootObject = obj,
                Items = new(FileResultData.ExtractObject(dir)),
                Kind = ResultDataKind.Files,
                Description = $"{baseType} {{ FullName = {dir.FullName} }}",
            },
            Array array => new ResultList
            {
                RootObject = obj,
                Items = new(ResultData.ExtractObject(array)),
                Kind = ResultDataKind.Complex,
                Description = $"{baseType} {{ Length = {array.Length} }}",
            },
            IDictionary dictionary => new ResultList
            {
                RootObject = obj,
                Items = new(ResultData.ExtractObject(dictionary)),
                Kind = ResultDataKind.Complex,
                Description = $"{baseType} {{ Count = {dictionary.Count} }}",
            },
            ICollection collection => new ResultList()
            {
                RootObject = obj,
                Kind = ResultDataKind.Complex,
                Description = $"{baseType} {{ Count = {collection.Count} }}",
            },
            Exception ex => new ResultList()
            {
                RootObject = obj,
                Items = new(ResultData.ExtractObject(ex)),
                Kind = ResultDataKind.Complex,
                Description = $"{baseType}: {ex.Message}",
                DisplayData = $"{ex}"
            },
            _ => new ResultList
            {
                RootObject = obj,
                Items = new(ResultData.ExtractObject(obj)),
                Kind = ResultDataKind.Complex,
                Description = baseType,
                DisplayData = Utils.ObjectToString(obj)
            },
        };
    }
}
