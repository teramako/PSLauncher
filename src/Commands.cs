using System.Windows.Input;

namespace PSLauncher;

public static class Commands
{
    public static readonly RoutedUICommand FocusToResultContent = new("Focus Result", "FocusResult", typeof(Commands));
    public static readonly RoutedUICommand StopExecution = new("Stop Execution", "StopExecution", typeof(Commands));
    public static readonly RoutedUICommand PinItem = new("Pin Item", "Pin", typeof(Commands));
    public static readonly RoutedUICommand UnpinItem = new("Unpin Item", "Unpin", typeof(Commands));
}
