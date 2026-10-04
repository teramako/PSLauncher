using System.Windows.Input;

namespace PSLauncher;

public static class Commands
{
    public static readonly RoutedUICommand FocusToResultContent = new("Focus Result", "FocusResult", typeof(Commands));
}
