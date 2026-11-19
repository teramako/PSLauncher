using PSLauncher.Models;
using PSLauncher.Views;
using System.Management.Automation;

namespace PSLauncher.PSCommands;

public enum ViewType
{
    Tree = 0,
    Default = Tree,
    Table = 1,
}

[Cmdlet(VerbsData.Out, "GridView")]
[Alias("ogv")]
public class OutGridViewCommand : Cmdlet
{
    [Parameter(Position = 0)]
    public ViewType Type { get; set; }

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public PSObject? InputObject { get; set; }

    private readonly List<object> _data = [];

    private IObjectList? BuildObjectList()
    {
        if (_data.Count == 1)
        {
            return Type switch
            {
                ViewType.Table => TableList.Build(_data[0]),
                _ => ObjectTreeList.Build(_data[0])
            };
        }

        return Type switch
        {
            ViewType.Table => TableList.Build(_data),
            _ => ObjectTreeList.Build(_data)
        };
    }
    protected override void ProcessRecord()
    {
        var obj = Utils.GetBaseObject(InputObject);
        if (obj is null)
            return;

        WriteVerbose($"{_data.Count:3d}]: [{Utils.GetTypeName(obj.GetType())}] {obj}");
        _data.Add(obj);
    }

    protected override void EndProcessing()
    {
        if (_data.Count == 0)
            return;

        IObjectList? objectList = BuildObjectList();
        if (objectList is null)
            return;

        App.Current.Dispatcher.Invoke(() =>
        {
            var win = new ObjectsViewWindow(objectList);
            win.Show();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }
}
