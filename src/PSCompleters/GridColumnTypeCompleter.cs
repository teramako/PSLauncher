using System.Management.Automation;
using System.Management.Automation.Language;
using System.Collections;

namespace PSLauncher.PSCompleters;

public sealed class GridColumnTypeCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName,
                                                          string parameterName,
                                                          string wordToComplete,
                                                          CommandAst commandAst,
                                                          IDictionary fakeBoundParameters)
    {
        bool completed = false;

        foreach (var type in PSObjectsMembers.Types)
        {
            if (type.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)
                || (type.FullName?.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                yield return new(type.FullName, type.Name, CompletionResultType.Type, type.FullName);
                completed = true;
            }
        }
        if (completed)
            yield break;

        CommandCompletion? matches = null;
        try
        {
            matches = CommandCompletion.CompleteInput($"[{wordToComplete}", wordToComplete.Length, null);
        }
        catch
        {
        }
        if (matches is null)
            yield break;

        foreach (var completion in matches.CompletionMatches)
        {
            yield return completion;
        }
    }
}
