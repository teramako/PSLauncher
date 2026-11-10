using System.Windows.Input;

namespace PSLauncher;

public static class Commands
{
    public static readonly RoutedUICommand FocusToResultContent = new("Focus Result", "FocusResult", typeof(Commands));
    public static readonly RoutedUICommand StopExecution = new("Stop Execution", "StopExecution", typeof(Commands));
    public static readonly RoutedUICommand PinItem = new("Pin Item", "Pin", typeof(Commands))
    {
        InputGestures = { new KeyGesture(Key.P, ModifierKeys.Control) }
    };
    public static readonly RoutedUICommand UnpinItem = new("Unpin Item", "Unpin", typeof(Commands))
    {
        InputGestures = { new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift) }
    };

    #region DataGrid
    public static readonly RoutedUICommand CopyRow = new("Copy Row", "CopyRow", typeof(Commands))
    {
        InputGestures = { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) }
    };
    public static readonly RoutedUICommand OpenInObjectsTreeWindow = new("Open in Objects Tree Window", "OpenInObjectsTreeWindow", typeof(Commands));
    public static readonly RoutedUICommand OpenInFilesTreeWindow = new("Open in Files Tree Window", "OpenInFilesTreeWindow", typeof(Commands));
    #endregion
}
