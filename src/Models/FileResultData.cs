using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace PSLauncher.Models;

public class FileResultData : INotifyPropertyChanged, IResultData, IResultData<FileResultData>
{
    private readonly FileSystemInfo? file;
    private readonly ReadOnlyCollection<FileSystemInfo>? children;

    public FileResultData(FileSystemInfo file, int index = 0)
        : this(file.Name, file.FullName, file is FileInfo f ? f.Length : null, file.LastWriteTime, file.Attributes.HasFlag(FileAttributes.Directory), index)
    {
        this.file = file;
        HasChildren = IsDirectory = file.Attributes.HasFlag(FileAttributes.Directory);
    }
    public FileResultData(string name, IList<FileSystemInfo> children, int index = 0)
        : this(name, string.Empty, null, null, true, index)
    {
        Name = name;
        this.children = children.AsReadOnly();
        NodeType = ObjectNodeType.VirtualRow;
    }
    public FileResultData(string name, string fullName, long? size, DateTime? lastWriteTime, bool isDirectory, int index = 0)
    {
        Name = name;
        FullName = fullName;
        Size = size;
        LastWriteTime = lastWriteTime;
        HasChildren = IsDirectory = isDirectory;
        Index = index;
        ValueType = isDirectory ? ObjectValueType.Directory : ObjectValueType.File;
    }

    public int Index { get; }
    public IResultData? Parent { get; private set; }
    public object? RawValue => file;
    public string Name { get; }
    public ImageSource? Icon => file is not null ? NativeMethods.GetIcon(file) : null;
    public string FullName { get; }
    public long? Size { get; }
    public DateTime? LastWriteTime { get; }
    public bool IsDirectory { get; }
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
    public ObjectValueType ValueType { get; }
    public Type DataType { get; } = typeof(FileSystemInfo);
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
        "LastWriteTime" => $"{LastWriteTime}",
        _ => string.Empty
    };

    public IEnumerable<IResultData> GetChildren()
    {
        if (children is not null)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var childData = new FileResultData(children[i], i);
                childData.SetParent(this);
                yield return childData;
            }
        }
        else if (file is DirectoryInfo dirInfo)
        {
            foreach (var child in ExtractObject(dirInfo))
            {
                child.SetParent(this);
                yield return child;
            }
        }
    }

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
