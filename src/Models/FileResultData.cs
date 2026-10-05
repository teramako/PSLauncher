using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace PSLauncher.Models;

public class FileResultData(FileSystemInfo file) : IResultData, INotifyPropertyChanged
{
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
    public event PropertyChangedEventHandler? PropertyChanged;

    public static IEnumerable<FileResultData> ExtractObject(DirectoryInfo parentDir)
    {
        foreach (var file in parentDir.GetFileSystemInfos())
        {
            var fileData = new FileResultData(file);
            yield return fileData;
        }
    }
}
