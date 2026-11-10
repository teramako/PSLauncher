using PSLauncher.Shell;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Security;

namespace PSLauncher.Models;

public class ResultList : INotifyPropertyChanged
{
    public object? RootObject { get; init; }
    public string Description { get; init; } = string.Empty;
    public string DisplayData { get; init; } = string.Empty;
    public bool HasDisplayData => !string.IsNullOrEmpty(DisplayData);
    public ResultDataKind Kind { get; init; }
    public ObservableCollection<IResultData> Items { get; init; } = [];

    /// <summary>
    /// Count of root items
    /// </summary>
    public int Count
    {
        get
        {
            if (field < 0)
            {
                field = Items.Count(item => item.Level == 0);
            }
            return field;
        }
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(Count)));
        }
    } = -1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Append(IResultData data)
    {
        Items.Add(data);
        Count += 1;
    }

    public void Append(object? data)
    {
        var c = Count;
        Items.Add(new ResultData(c, data));
        Count = c + 1;
    }

    public void Clear()
    {
        Count = 0;
        Items.Clear();
    }

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
        if (parentRow is null || parentRow.IsExpaned)
            return;
        var parentLevel = parentRow.Level;
        bool hasChildren = false;
        IEnumerable<IResultData> children = Sorter.Sorter is not null
                                            ? Sorter.Sorter(parentRow.GetChildren(), Sorter.Ascending)
                                            : parentRow.GetChildren();
        foreach (var child in children)
        {
            Items.Insert(++index, child);
            hasChildren = true;
        }
        if (hasChildren)
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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ResultList?> BuildAsync(PSDataCollection<PSObject> psObjects, CancellationToken cancellationToken = default)
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
        bool isString = true;
        bool isFileSystemInfo = true;
        await foreach (var obj in NormalizePSObjects(psObjects, cancellationToken))
        {
            objects.Add(obj);
            if (isString)
                isString = obj is string;
            if (isFileSystemInfo)
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
            return BuildAsFilesTree(objects.OfType<FileSystemInfo>());
        }

        return new ResultList
        {
            RootObject = psObjects,
            Items = [.. BuildExpandedItems(new ResultData("Results", psObjects, ObjectNodeType.VirtualRow))],
            Description = itemCount > 1 ? $"{{ Count = {itemCount} }}" : string.Empty,
            Kind = ResultDataKind.Objects,
        };
    }

    public static ResultList BuildAsFilesTree(IEnumerable<FileSystemInfo> fileSystemInfos)
    {
        int itemCount = 0;
        Dictionary<string, List<FileSystemInfo>> childrenMap = [];
        foreach (var fsInfo in fileSystemInfos)
        {
            var parentPath = Path.GetDirectoryName(fsInfo.FullName) ?? string.Empty;
            if (childrenMap.TryGetValue(parentPath, out var list))
                list.Add(fsInfo);
            else
                childrenMap[parentPath] = [fsInfo];

            itemCount++;
        }
        int index = 0;
        return new ResultList
        {
            RootObject = fileSystemInfos,
            Items = [.. childrenMap.SelectMany(kv => BuildExpandedItems(new FileResultData(kv.Key, kv.Value, index++)))],
            Description = itemCount > 1 ? $"FileSystemInfo {{ Count = {itemCount} }}" : "FileSystemInfo",
            Kind = ResultDataKind.Files,
        };
    }

    private (Func<IEnumerable<IResultData>, bool, IEnumerable<IResultData>>? Sorter, bool Ascending) Sorter = (null, true);

    public bool Sort(ListSortDirection? sortDirection, string colName = "Name")
    {
        Sorter = (null, false);
        List<IResultData> results;
        if (sortDirection is null)
        {
            results = [.. SortTreeItems(Items, x => x.Index)];
        }
        else
        {
            bool ascending = sortDirection is ListSortDirection.Ascending;
            switch (Kind)
            {
                case ResultDataKind.NotSpecified:
                case ResultDataKind.Simple:
                    return false;
                case ResultDataKind.Files:
                    if (!FileResultData.Sorters.TryGetValue(colName, out var fileSorter))
                        return false;
                    results = [.. fileSorter(Items, ascending)];
                    Sorter = (fileSorter, ascending);
                    break;
                case ResultDataKind.Objects:
                default:
                    if (!ResultData.Sorters.TryGetValue(colName, out var sorter))
                        return false;
                    results = [.. sorter(Items, ascending)];
                    Sorter = (sorter, ascending);
                    break;

            }
        }
        Items.Clear();
        results.ForEach(Items.Add);
        return true;
    }

    private static Dictionary<IResultData, List<T>> BuildChildrenMap<T>(IEnumerable<T> originalItems, out List<T> rootItems)
        where T : IResultData
    {
        rootItems = [];
        Dictionary<IResultData, List<T>> childrenMap = [];
        foreach (var item in originalItems)
        {
            if (item.Parent is null)
            {
                rootItems.Add(item);
                continue;
            }
            if (childrenMap.TryGetValue(item.Parent, out var children))
            {
                children.Add(item);
            }
            else
            {
                childrenMap.Add(item.Parent, [item]);
            }
        }
        return childrenMap;
    }
    private static IEnumerable<T> EnumerateTreeItems<T>(IEnumerable<T> items, Dictionary<IResultData, List<T>> childrenMap)
        where T : IResultData
    {
        foreach (var item in items)
        {
            yield return item;
            if (item.IsExpaned && childrenMap.TryGetValue(item, out var children))
            {
                foreach (var child in EnumerateTreeItems(children, childrenMap))
                    yield return child;
            }
        }
    }
    public static IEnumerable<T> SortTreeItems<T, TKey>(IEnumerable<T> originalItems,
                                                        Func<T, TKey> keySelector,
                                                        bool ascending = true)
        where T : IResultData
    {
        var childrenMap = BuildChildrenMap(originalItems, out var rootItems);
        foreach (var kv in childrenMap)
        {
            childrenMap[kv.Key] = [.. SortItems(kv.Value)];
        }

        return EnumerateTreeItems(SortItems(rootItems), childrenMap);

        IEnumerable<T> SortItems(IEnumerable<T> items)
        {
            return ascending ? items.OrderBy(keySelector) : items.OrderByDescending(keySelector);
        }
    }
    public static IEnumerable<T> SortTreeItems<T>(IEnumerable<T> originalItems,
                                                  Comparison<T> comparison,
                                                  bool ascending = true)
        where T : IResultData
    {
        return SortTreeItems(originalItems, Comparer<T>.Create(ascending ? comparison : (x, y) => comparison(y, x)));
    }
    public static IEnumerable<T> SortTreeItems<T>(IEnumerable<T> originalItems,
                                                  IComparer<T> comparer)
        where T : IResultData
    {
        var childrenMap = BuildChildrenMap(originalItems, out var rootItems);
        foreach (var kv in childrenMap)
        {
            childrenMap[kv.Key] = [.. kv.Value.Order(comparer)];
        }
        return EnumerateTreeItems(rootItems.Order(comparer), childrenMap);
    }

    /// <summary>
    /// Build <seealso cref="ResultList"/> instance from a single <seealso cref="PSObject"/>
    /// </summary>
    /// <param name="psObject"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(psObject))]
    public static ResultList? Build(PSObject? psObject) => Build(Utils.GetBaseObject(psObject));

    /// <summary>
    /// Build <seealso cref="ResultList"/> instance from an object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(obj))]
    public static ResultList? Build(object? obj, ResultDataKind kind = ResultDataKind.NotSpecified)
    {
        if (obj is null)
        {
            return null;
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
                Items = [.. BuildExpandedItems(new ResultData("PSObject", pso, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = $"PSObject({pso.TypeNames[0]})",
                DisplayData = $"{pso}"
            },
            FileSystemInfo fileSystemInfo when kind is ResultDataKind.Objects => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new ResultData("FileSystem", fileSystemInfo, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = baseType,
                DisplayData = $"{fileSystemInfo.FullName}"
            },
            FileSystemInfo fileSystemInfo => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new FileResultData(fileSystemInfo))],
                Kind = ResultDataKind.Files,
                Description = baseType,
                DisplayData = $"{fileSystemInfo.FullName}"
            },
            Array array => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new ResultData("Array", array, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = $"{baseType} {{ Length = {array.Length} }}",
            },
            IDictionary dictionary => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new ResultData("Dictionary", dictionary, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = $"{baseType} {{ Count = {dictionary.Count} }}",
            },
            ICollection collection => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new ResultData("Collection", collection, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = $"{baseType} {{ Count = {collection.Count} }}",
            },
            Exception ex => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new ResultData("Exception", ex, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = $"{baseType}: {ex.Message}",
                DisplayData = $"{ex}"
            },
            _ => new ResultList
            {
                RootObject = obj,
                Items = [.. BuildExpandedItems(new ResultData(baseType, obj, ObjectNodeType.VirtualRow))],
                Kind = ResultDataKind.Objects,
                Description = baseType,
                DisplayData = Utils.ObjectToString(obj)
            },
        };
    }
    private static IEnumerable<IResultData> BuildExpandedItems(IResultData parentRow)
    {
        yield return parentRow;
        bool hasChildren = false;
        foreach (var item in parentRow.GetChildren())
        {
            hasChildren = true;
            yield return item;
        }
        parentRow.IsExpaned = hasChildren;
    }

    private static async IAsyncEnumerable<object?> NormalizePSObjects(PSDataCollection<PSObject> pSObjects,
                                                                      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        PSDataCollection<PSObject> formatedObjects = [];
        foreach (var pso in pSObjects)
        {
            if (pso is null)
            {
                yield return null;
                continue;
            }
            var obj = Utils.GetBaseObject(pso);
            var t = obj.GetType();
            if (t.Namespace == "Microsoft.PowerShell.Commands.Internal.Format")
            {
                switch (t.Name)
                {
                    case "FormatStartData":
                        formatedObjects.Clear();
                        formatedObjects.Add(pso);
                        continue;
                    case "GroupStartData":
                    case "FormatEntryData":
                    case "GroupEndData":
                        formatedObjects.Add(pso);
                        continue;
                    case "FormatEndData":
                        formatedObjects.Add(pso);
                        try
                        {
                            foreach (var line in await ToStringFormaattedObjects(formatedObjects, cancellationToken))
                            {
                                yield return line;
                            }
                        }
                        finally
                        {
                            formatedObjects.Clear();
                        }

                        continue;
                }
            }

            if (formatedObjects.Count > 0)
            {
                foreach (var line in await ToStringFormaattedObjects(formatedObjects, cancellationToken))
                {
                    yield return line;
                }
            }
            yield return obj;
        }

    }

    private static async Task<PSDataCollection<string>> ToStringFormaattedObjects(PSDataCollection<PSObject> formatedData,
                                                                                  CancellationToken cancellationToken = default)
    {
        try
        {
            var cmd = new PSCommand();
            cmd.AddCommand("Out-String")
               .AddParameter("Stream")
               .AddParameter("InputObject", formatedData);
            return await PowerShellRunner.Sub.SimpleInvokeAsync<string>(cmd, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.Print("ToStringFormaattedObjects: {0}: {1}", ex.GetType().Name, ex.Message);
        }
        return [];
    }
}
