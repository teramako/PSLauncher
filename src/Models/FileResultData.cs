using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace PSLauncher.Models;

public class FileResultData(FileSystemInfo file, int index = 0) :  INotifyPropertyChanged, IResultData, IResultData<FileResultData>
{
    public int Index => index;
    public IResultData? Parent { get; private set; }
    public object? RawValue => file;
    public string Name => file.Name;
    public ImageSource? Icon => NativeMethods.GetIcon(file);
    public string FullName => file.FullName;
    public long? Size => IsDirectory ? null : ((FileInfo)file).Length;
    public DateTime LastWriteTime => file.LastWriteTime;
    public bool IsDirectory => file.Attributes.HasFlag(FileAttributes.Directory);
    public int Level { get; set; } = 0;
    public bool HasChildren { get; set; } = file.Attributes.HasFlag(FileAttributes.Directory);
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
    public ObjectValueType ValueType => IsDirectory ? ObjectValueType.Directory : ObjectValueType.File;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetParent(IResultData parent)
    {
        Parent = parent;
        Level = parent.Level + 1;
    }

    public string GetColumnValue(string? columnName) => columnName switch
    {
        "Name" => Name,
        "Size" => Size?.ToString() ?? string.Empty,
        "LastWriteTime" => LastWriteTime.ToString(),
        _ => string.Empty
    };

    public static IEnumerable<FileResultData> ExtractObject(IResultData data)
    {
        return data.RawValue is DirectoryInfo dirInfo ? ExtractObject(dirInfo) : [];
    }

    public static IEnumerable<FileResultData> ExtractObject(DirectoryInfo parentDir)
    {
        int index = 0;
        foreach (var file in parentDir.GetFileSystemInfos())
        {
            var fileData = new FileResultData(file, index++);
            yield return fileData;
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
        ["LastWriteTime"] = (items, asc) => ResultList.SortTreeItems(items.OfType<FileResultData>(), static x => x.LastWriteTime, asc),
        ["Size"] = (items, asc) => ResultList.SortTreeItems(items.OfType<FileResultData>(), static x => x.Size, asc),
    };
}
