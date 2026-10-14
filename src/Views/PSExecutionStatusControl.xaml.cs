using System.Management.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using PSLauncher.Models;
using PSLauncher.Shell;

namespace PSLauncher.Views;

public partial class PSExecutionStatusControl : UserControl
{
    public PSExecutionStatusControl()
    {
        InitializeComponent();
    }
    public ResultList ErrorList { get; } = new() { Description = "Error" };
    public ResultList WarningList { get; } = new() { Description = "Warning" };
    public ResultList VerboseList { get; } = new() { Description = "Verbose" };
    public ResultList InformationList { get; } = new() { Description = "Information" };
    public ResultList DebugList { get; } = new() { Description = "Debug" };

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
                VerboseList.Append(record.Value);
                return;
            case StreamType.Information:
                VerboseList.Append(record.Value);
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
