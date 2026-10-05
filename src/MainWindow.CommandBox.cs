using System.Management.Automation.Language;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSLauncher;

public partial class MainWindow
{
    private void InitCommandBox()
    {
        TextCompositionManager.AddPreviewTextInputStartHandler(CommandBox, CommandBox_TextCompositionStarted);
        TextCompositionManager.AddPreviewTextInputHandler(CommandBox, CommandBox_TextCompositionEnded);
    }

    /// <summary>
    /// IME conversion status for <seealso cref="CommandBox"/>
    /// </summary>
    public bool ImeComposing { get; private set; }

    /// <summary>
    /// Handler for the <seealso cref="CommandBox"/>'s IME conversion start event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <see cref="TextCompositionEventHandler"/>
    private void CommandBox_TextCompositionStarted(object sender, TextCompositionEventArgs e)
    {
        ImeComposing = true;
    }
    /// <summary>
    /// Handler for the <seealso cref="CommandBox"/>'s IME conversion completion event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <see cref="TextCompositionEventHandler"/>
    private void CommandBox_TextCompositionEnded(object sender, TextCompositionEventArgs e)
    {
        ImeComposing = false;
    }

    private async void CommandBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ImeComposing)
        {
            // Skip processing while IME input is active
            return;
        }
        if (IsCompletionWindowVisible)
        {
            if (_completionWindow.TryOnKeyDown(e))
            {
                e.Handled = true;
                return;
            }
        }
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                string script = WpfUtils.GetInnerText(CommandBox);
                await ExecuteScript(script);
                break;
            case Key.Escape:
                e.Handled = true;
                Hide();
                break;
            case Key.C when Keyboard.Modifiers is ModifierKeys.Control:
                e.Handled = true;
                if (IsRunspaceRunning)
                {
                    StopRunspace(sender, e);
                }
                else
                {
                    CommandBox.Document.Blocks.Clear();
                    CommandBox.CaretPosition = CommandBox.Document.ContentEnd;
                }
                break;
            case Key.Tab:
                e.Handled = true;
                StartCompletion(autoAcceptSingleMatch: true);
                break;
            case Key.Space when Keyboard.Modifiers is ModifierKeys.Control:
                e.Handled = true;
                StartCompletion(autoAcceptSingleMatch: false);
                break;
        }
    }

    /// <summary>
    /// <seealso cref="TextChangedEventHandler"/> of <seealso cref="CommandBox"/>
    /// <para>
    /// Stops or starts <seealso cref="CommandBoxHighlightTimer"/> to highlight PowerShell code
    /// </para>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CommandBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CommandBoxHighlightTimer ??= new(TimeSpan.FromMicroseconds(60), DispatcherPriority.Background, Highlight_Tick, CommandBox.Dispatcher);
        CommandBoxHighlightTimer.Stop();
        CommandBoxHighlightTimer.Start();
    }

    private DispatcherTimer? CommandBoxHighlightTimer;

    /// <summary>
    /// <seealso cref="DispatcherTimer.Tick"/> event handler for <seealso cref="CommandBoxHighlightTimer"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Highlight_Tick(object? sender, EventArgs e)
    {
        CommandBoxHighlightTimer?.Stop();
        HighlightCommandBox();
    }

    /// <summary>
    /// Highlight the PowerShell code in <seealso cref="CommandBox"/>
    /// </summary>
    private void HighlightCommandBox()
    {
        var richTextBox = CommandBox;
        var script = WpfUtils.GetInnerText(richTextBox);
        int offset = 0;
        if (script.StartsWith('!'))
        {
            script = $@"{CommandPrefix} {script[1..]}";
            offset = CommandPrefix.Length;
        }
        ResetFormat();
        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }

        var ast = Parser.ParseInput(script, out var tokens, out var errors);
        Format(tokens);
        if (errors is not null)
        {
            FormatError(errors);
        }

        void ResetFormat()
        {
            var fullRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            fullRange.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
            fullRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
        }
        void Format(IEnumerable<Token> tokens, bool nesting = false)
        {
            foreach (var token in tokens)
            {
                switch (token.Kind)
                {
                    case TokenKind.EndOfInput:
                        return;
                    case TokenKind.Generic:
                    case TokenKind.Identifier:
                        if (token.TokenFlags.HasFlag(TokenFlags.CommandName))
                        {
                            Apply(token, Brushes.Yellow);
                            break;
                        }
                        Apply(token, Brushes.White);
                        break;
                    case TokenKind.Parameter:
                        Apply(token, Brushes.DodgerBlue);
                        break;
                    case TokenKind.Comment:
                        Apply(token, Brushes.Green);
                        break;
                    case TokenKind.StringLiteral:
                        Apply(token, Brushes.LightGreen);
                        break;
                    case TokenKind.StringExpandable:
                        Apply(token, Brushes.LightPink);
                        if (nesting && token is StringExpandableToken expandableToken and { NestedTokens.Count: > 0 })
                        {
                            Format(expandableToken.NestedTokens, nesting);
                        }
                        break;
                    case TokenKind.Variable:
                    case TokenKind.SplattedVariable:
                        Apply(token, Brushes.Cyan);
                        break;
                    default:
                        Apply(token, Brushes.Gray);
                        break;
                }
            }
        }
        void Apply(Token token, SolidColorBrush brush)
        {
            var range = WpfUtils.GetRange(richTextBox, token.Extent.StartOffset - offset, token.Extent.EndOffset - offset);
            range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
        }
        void FormatError(ParseError[] errors)
        {
            if (errors.Length == 0)
                return;
            var deco = new TextDecoration
            {
                Pen = new Pen(Brushes.Red, 1)
                {
                    DashStyle = DashStyles.Dot
                }
            };
            var decos = new TextDecorationCollection { deco };
            foreach (var err in errors)
            {
                ApplyError(err, decos);
            }
        }
        void ApplyError(ParseError err, TextDecorationCollection decos)
        {
            var range = WpfUtils.GetRange(richTextBox ,err.Extent.StartOffset - offset, err.Extent.EndOffset - offset);
            range.ApplyPropertyValue(Inline.TextDecorationsProperty, decos);
        }
    }
}
