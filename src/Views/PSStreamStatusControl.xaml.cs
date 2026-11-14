using System.Windows.Controls;
using PSLauncher.Models;
using PSLauncher.Shell;

namespace PSLauncher.Views;

public partial class PSStreamStatusControl : UserControl
{
    public PSStreamStatusControl()
    {
        InitializeComponent();
    }
    public ObjectTreeList ErrorList { get; } = new() { Description = "Error", Kind = ResultDataKind.Objects };
    public ObjectTreeList WarningList { get; } = new() { Description = "Warning", Kind = ResultDataKind.Objects };
    public ObjectTreeList VerboseList { get; } = new() { Description = "Verbose", Kind = ResultDataKind.Objects };
    public ObjectTreeList InformationList { get; } = new() { Description = "Information", Kind = ResultDataKind.Objects };
    public ObjectTreeList DebugList { get; } = new() { Description = "Debug", Kind = ResultDataKind.Objects };

    public void AddRecord(PSDataRecord record)
    {
        switch (record.StreamType)
        {
            case StreamType.Error:
                ErrorList.Append(record.Value);
                return;
            case StreamType.Warning:
                WarningList.Append(record.Value);
                return;
            case StreamType.Verbose:
                VerboseList.Append(record.Value);
                return;
            case StreamType.Debug:
                DebugList.Append(record.Value);
                return;
            case StreamType.Information:
                InformationList.Append(record.Value);
                return;
            default:
                return;
        }
    }

    public void ClearStreams()
    {
        ErrorList.Clear();
        WarningList.Clear();
        VerboseList.Clear();
        InformationList.Clear();
        DebugList.Clear();
    }
}
