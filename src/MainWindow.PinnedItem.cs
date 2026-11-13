using System.Collections;
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

    public void PinItem(object pinItem)
    {
        PinnedItem.DataContext = new PinnedItem(pinItem);

        RunspaceManager.MainRunspace.SessionStateProxy.SetVariable("PinnedItem", pinItem);
    }

    public void PinItems(IList selectedItems)
    {
        if (selectedItems.Count == 0)
            return;

        object pinItem;
        List<object> items = [];
        HashSet<Type> types = [];
        foreach (var obj in selectedItems)
        {
            var item = obj;
            if (obj is IResultData data)
            {
                if (data is { NodeType: ObjectNodeType.VirtualRow } or { RawValue: null })
                    continue;
                item = data.RawValue;
            }

            items.Add(item);
            if (Utils.GetDataType(item) is Type t)
            {
                types.Add(t);
            }
        }

        if (items.Count == 0)
            return;
        if (items.Count == 1)
        {
            pinItem = items[0];
        }
        else
        {
            if (types.Count == 1)
            {
                var t = types.First();
                var typedArray = Array.CreateInstance(t, items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    typedArray.SetValue(items[i], i);
                }
                pinItem = typedArray;
            }
            else
            {
                pinItem = items.ToArray();
            }
        }

        PinnedItem.DataContext = new PinnedItem(pinItem);
        RunspaceManager.MainRunspace.SessionStateProxy.SetVariable("PinnedItem", pinItem);
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
