using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Security;
using System.Windows.Media;

namespace PSLauncher.Models;

public class PinnedItem : ResultList
{
    public PinnedItem(object obj)
    {
        RootObject = obj;
        var baseType = Utils.GetTypeName(obj.GetType());
        switch (obj)
        {
            case string:
            case int or uint or long or ulong or short or ushort or byte or sbyte or float or double or decimal:
            case bool:
                Kind = ResultDataKind.Simple;
                Description = baseType;
                ToolTipText = $"{baseType}: {obj}";
                DisplayData = $"{obj}";
                break;
            case Enum enumVal:
                Kind = ResultDataKind.Simple;
                Description = $"{enumVal}";
                DisplayData = ToolTipText = $"Enum({baseType}) {enumVal:G} (0x{enumVal:X})";
                break;
            case SecureString secureString:
                Kind = ResultDataKind.Simple;
                Description = $"SecureString";
                DisplayData = ToolTipText = $"{secureString}";
                break;
            case PSCredential credential:
                Kind = ResultDataKind.Simple;
                Description = $"PSCredential [{credential.UserName}]";
                DisplayData = ToolTipText = $"{credential}";
                break;
            case DirectoryInfo dir:
                Kind = ResultDataKind.Files;
                Description = $"Directory {{ {dir.Name} }}";
                ToolTipText = $"Directory: {dir.FullName}";
                DisplayData = dir.FullName;
                break;
            case FileInfo fileInfo:
                Kind = ResultDataKind.Files;
                Description = $"File {{ {fileInfo.Name} }}";
                ToolTipText = $"File: {fileInfo.FullName}";
                DisplayData = fileInfo.FullName;
                break;
            case Array array:
                Kind = ResultDataKind.Complex;
                Description = baseType;
                ToolTipText = $"{baseType} {{ Length = {array.Length} }}";
                break;
            case IDictionary dictionary:
                Kind = ResultDataKind.Complex;
                Description = baseType;
                ToolTipText = $"{baseType} {{ Count = {dictionary.Count} }}";
                break;
            case ICollection collection:
                Kind = ResultDataKind.Complex;
                Description = baseType;
                ToolTipText = $"{baseType} {{ Count = {collection.Count} }}";
                break;
            case PSObject pso:
                Kind = ResultDataKind.Complex;
                Description = $"PSObject({pso.TypeNames[0]})";
                DisplayData = ToolTipText = $"{pso}";
                break;
            case Exception ex:
                Kind = ResultDataKind.Complex;
                Description = baseType;
                ToolTipText = $"{baseType}: {ex.Message}";
                DisplayData = $"{ex}";
                break;
            default:
                Kind = ResultDataKind.Complex;
                Description = baseType;
                ToolTipText = Utils.GetTypeName(obj.GetType(), abbreviate: true, dropNamespace: false);
                DisplayData = Utils.ObjectToString(obj);
                break;
        };
    }

    public void UpdateItems()
    {
        if (RootObject is null || Kind is ResultDataKind.Simple or ResultDataKind.NotSpecified)
            return;

        if (Items.Count > 0)
        {
            Items.Clear();
        }

        IEnumerable<IResultData> items = RootObject switch
        {
            DirectoryInfo dir => FileResultData.ExtractObject(dir),
            FileInfo file => [new FileResultData(file)],
            _ => ResultData.ExtractObject(RootObject),
        };
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public string ToolTipText { get; } = string.Empty;

    private ImageSource? _icon;
    public ImageSource? Icon
    {
        get
        {
            if (_icon is not null) return _icon;
            if (RootObject is FileSystemInfo fileSystemInfo)
            {
                return _icon = NativeMethods.GetIcon(fileSystemInfo);
            }
            return null;
        }
    }
}
