using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Windows;
using System.Windows.Documents;

namespace PSLauncher;

public partial class MainWindow
{
    /// <summary>
    /// The completion window that shows the list of possible completions.
    /// </summary>
    private CompletionWindow? _completionWindow;

    /// <summary>
    /// Gets a value indicating whether the completion window is visible.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_completionWindow))]
    public bool IsCompletionWindowVisible => _completionWindow is { IsVisible: true };

    /// <summary>
    /// Starts the completion process by retrieving possible completions
    /// and displaying them in a completion window if necessary.
    /// </summary>
    /// <param name="autoAcceptSingleMatch">
    /// If set to true, automatically accepts the single match if only one completion is found.
    /// </param>
    private void StartCompletion(bool autoAcceptSingleMatch = false)
    {
        CommandCompletion? commandCompletion = GetCompletions();
        switch (commandCompletion?.CompletionMatches.Count)
        {
            case null:
            case 0:
                return;
            case 1:
                commandCompletion.CurrentMatchIndex = 0;
                if (autoAcceptSingleMatch)
                {
                    AcceptCompletion(commandCompletion);
                }
                else
                {
                    ShowCompletionWindow(commandCompletion);
                }
                return;
            default:
                ShowCompletionWindow(commandCompletion);
                return;
        }
    }

    /// <summary>
    /// Shows the completion window with the provided command completions.
    /// </summary>
    /// <param name="commandCompletion"></param>
    private void ShowCompletionWindow(CommandCompletion commandCompletion)
    {
        if (_completionWindow is null)
        {
            _completionWindow = new CompletionWindow(commandCompletion);
        }
        else
        {
            _completionWindow.CurrentCommandCompletion = commandCompletion;
        }
        Rect rect = CommandBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
        Point point = CommandBox.PointToScreen(new(rect.X, rect.Bottom));
        _completionWindow.Left = point.X;
        _completionWindow.Top = point.Y;
        _completionWindow.Show();
        CommandBox.Focus();
    }

    /// <summary>
    /// Accepts the currently selected completion from the provided command completions
    /// </summary>
    /// <param name="commandCompletion"></param>
    public void AcceptCompletion(CommandCompletion commandCompletion)
    {
        var i = commandCompletion.CurrentMatchIndex;
        if (i < 0 || i > commandCompletion.CompletionMatches.Count - 1)
        {
            return;
        }
        var completionResult = commandCompletion.CompletionMatches[i];
        var startOffset = commandCompletion.ReplacementIndex;
        var length = commandCompletion.ReplacementLength;
        var textRange = WpfUtils.GetRange(CommandBox, startOffset, startOffset + length);
        textRange.Text = completionResult.CompletionText;
        CommandBox.CaretPosition = textRange.End;
    }

    /// <summary>
    /// Gets the command completions for the current text in the command box.
    /// </summary>
    /// <remarks>
    /// This method uses the PowerShell TabExpansion2 command to retrieve possible
    /// completions based on the current text and cursor position in the command box.
    /// <para>
    /// If the command box starts with '!', it prepends the CommandPrefix to the script before invoking TabExpansion2.
    /// </para>
    /// </remarks>
    /// <returns></returns>
    private CommandCompletion? GetCompletions()
    {
        if (IsRunspaceRunning)
            return null;

        var cursorPos = WpfUtils.GetCaretIndex(CommandBox);
        int offset = 0;
        string script = WpfUtils.GetInnerText(CommandBox);
        if (script.StartsWith('!'))
        {
            script = $"{CommandPrefix} {script[1..]}";
            offset = CommandPrefix.Length;
        }

        using var ps = PowerShell.Create(App.Runspace);
        try
        {
            ps.Commands.Clear();
            var results = ps.AddCommand("TabExpansion2")
                            .AddParameter("inputScript", script)
                            .AddParameter("cursorColumn", cursorPos + offset)
                            .Invoke();
            if (results?.Count > 0 && results[0].BaseObject is CommandCompletion cc)
            {
                cc.ReplacementIndex -= offset;
                return cc;
            }
        }
        catch
        {
        }
        return null;
    }
}
