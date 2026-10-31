using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace PSLauncher.Models;

public class FileResultData(FileSystemInfo file, int index) : IResultData, INotifyPropertyChanged
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

    public static IEnumerable<FileResultData> ExtractObject(DirectoryInfo parentDir)
    {
        int index = 0;
        foreach (var file in parentDir.GetFileSystemInfos())
        {
            var fileData = new FileResultData(file, index++);
            yield return fileData;
        }
    }

    public static readonly Dictionary<string, Func<IEnumerable<IResultData>, bool, IEnumerable<IResultData>>> Sorters = new() {
        ["Name"] = (items, asc) => ResultList.SortTreeItems(items, static x => x.Name, asc),
        ["LastWriteTime"] = (items, asc) => ResultList.SortTreeItems(items.OfType<FileResultData>(), static x => x.LastWriteTime, asc),
        ["Size"] = (items, asc) => ResultList.SortTreeItems(items.OfType<FileResultData>(), static x => x.Size, asc),
    };
}
