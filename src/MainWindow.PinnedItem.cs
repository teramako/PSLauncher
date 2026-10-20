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
}
