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
    public ResultList ErrorList { get; } = new() { Description = "Error", Kind = ResultDataKind.Complex };
    public ResultList WarningList { get; } = new() { Description = "Warning", Kind = ResultDataKind.Complex };
    public ResultList VerboseList { get; } = new() { Description = "Verbose", Kind = ResultDataKind.Complex };
    public ResultList InformationList { get; } = new() { Description = "Information", Kind = ResultDataKind.Complex };
    public ResultList DebugList { get; } = new() { Description = "Debug", Kind = ResultDataKind.Complex };

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
