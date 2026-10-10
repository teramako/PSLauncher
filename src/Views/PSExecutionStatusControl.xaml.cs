using System.Management.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using PSLauncher.Models;

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

    public void AddRecord<T>(ResultList target, object? sender, DataAddedEventArgs e) where T : class
    {
        var record = (sender as PSDataCollection<T>)?[e.Index];
        if (record is null) return;
        Dispatcher.InvokeAsync(() => target.Append(record), DispatcherPriority.Background);
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
