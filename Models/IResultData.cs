namespace PSLauncher.Models;

public interface IResultData
{
    string Name { get; }
    object? RawValue { get; }
    int Level { get; set; }
    bool HasChildren { get; set; }
    bool IsExpaned { get; set; }
}
