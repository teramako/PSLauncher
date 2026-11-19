using PSLauncher.PSCompleters;
using System.Management.Automation;

namespace PSLauncher.PSCommands;

[Cmdlet(VerbsCommon.Set, "GridColumn")]
public class SetGridColumnCommand: Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string? Type { get; set; }

    [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
    public GridColumn[]? Columns { get; set; }

    private Type? _type;
    private List<GridColumn> _columns = [];

    protected override void BeginProcessing()
    {
        if (Type is null)
            return;
        if (!LanguagePrimitives.TryConvertTo<Type>(Type, out _type))
        {
            var ex = new ArgumentException($"Invalid type: '{Type}'");
            ThrowTerminatingError(new(ex, "InvalieType", ErrorCategory.InvalidArgument, Type));
        }
    }

    protected override void ProcessRecord()
    {
        if (Columns is null || Columns.Length == 0)
            return;

        _columns.AddRange(Columns);
    }

    protected override void EndProcessing()
    {
        if (_type is null)
            return;
        PSObjectsMembers.Add(_type, _columns);
    }
}
