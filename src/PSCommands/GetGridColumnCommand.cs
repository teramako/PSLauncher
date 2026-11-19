using PSLauncher.PSCompleters;
using System.Management.Automation;

namespace PSLauncher.PSCommands;

[Cmdlet(VerbsCommon.Get, "GridColumn")]
public class GetGridColumnCommand : Cmdlet
{
    [Parameter(Position = 0)]
    public string? Type { get; set; }

    private Type? _type;

    private void WriteColumns(Type type)
    {
        var columns = PSObjectsMembers.GetPSGridColumns(type);
        if (columns is null)
        {
            WriteWarning($"Columns of '{type}' is not defined.");
            return;
        }
        WriteObject(columns, true);
    }

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

    protected override void EndProcessing()
    {
        if (_type is not null)
        {
            WriteColumns(_type);
        }
        else
        {
            foreach (var type in PSObjectsMembers.Types)
            {
                WriteColumns(type);
            }
        }
    }
}
