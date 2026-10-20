using System.Windows.Input;
using PSLauncher.Models;
using PSLauncher.Shell;

namespace PSLauncher;

public partial class MainWindow
{
    private void PinnedItem_OnPopupOpened(object sender, EventArgs e)
    {
        var data = PinnedItem.DataContext as PinnedItem;
        if (data is null) return;
        data.UpdateItems();
    }

    private void PinItem(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is IResultData { RawValue: not null }  data)
        {
            PinnedItem.DataContext = new PinnedItem(data.RawValue);
            RunspaceManager.MainRunspace.SessionStateProxy.SetVariable("PinnedItem", data.RawValue);
        }
    }
    private void CanPinItem(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void UnpinItem(object sender, ExecutedRoutedEventArgs e)
    {
        PinnedItem.DataContext = null;
        RunspaceManager.MainRunspace.SessionStateProxy.SetVariable("PinnedItem", null);
    }

    private void CanUnpinItem(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = PinnedItem.DataContext is not null;
    }
}
