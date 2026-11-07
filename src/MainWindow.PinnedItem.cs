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

    private void PinItem(object sender, ExecutedRoutedEventArgs e)
    {
        object pinItem;
        if (e.Parameter is IResultData { RawValue: not null } data)
        {
            pinItem = data.RawValue;
        }
        else if (e.Parameter is IList { Count: > 0 } list)
        {
            List<object> items = [];
            HashSet<Type> types = [];
            foreach (var item in list.OfType<IResultData>())
            {
                if (item is { NodeType: ObjectNodeType.VirtualRow } or { RawValue: null })
                    continue;

                items.Add(item.RawValue);

                if (item.DataType != typeof(void))
                    types.Add(item.DataType);
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
        }
        else
        {
            return;
        }

        PinnedItem.DataContext = new PinnedItem(pinItem);
        RunspaceManager.MainRunspace.SessionStateProxy.SetVariable("PinnedItem", pinItem);
    }
    private void CanPinItem(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = e.Parameter switch
        {
            IResultData data => data is not { RawValue: null } and
                                        not { NodeType: ObjectNodeType.VirtualRow },
            IList list => list.OfType<IResultData>()
                              .Any(x => x is not { RawValue: null } and
                                             not { NodeType: ObjectNodeType.VirtualRow }),
            _ => false,
        };
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
