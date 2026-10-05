using PSLauncher.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PSLauncher.Views;

public partial class ObjectResultView : UserControl, IResultContentControl
{
    public ObjectResultView()
    {
        InitializeComponent();
    }

    public void FocusToContent()
    {
        // Implementation for focusing to the content
        if (!DetailExpander.IsExpanded)
        {
            DetailExpander.IsExpanded = true;
        }
        Dispatcher.BeginInvoke(() =>
        {
            FocusToResultGrid(ResultGrid, -1);
        }, DispatcherPriority.Background);
    }

    private void ResultGrid_GotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is DataGrid dataGrid)
        {
            FocusToResultGrid(dataGrid, -1);
        }
    }

    private void ResultData_RowKeyDown(object sender, KeyEventArgs e)
    {
        if (!WpfUtils.TryFindParent<DataGrid>(sender as DependencyObject, out var dataGrid))
            return;

        var data = DataContext as ResultList;
        if (data is null)
            return;
        var selectedIndex = dataGrid.SelectedIndex;
        if (selectedIndex < 0) return;
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = data.Toggle(selectedIndex);
                break;
            case Key.Right:
                data.Expand(selectedIndex);
                e.Handled = true;
                break;
            case Key.Left:
                var parentIndex = data.Collapse(selectedIndex);
                FocusToResultGrid(dataGrid, parentIndex);
                e.Handled = true;
                break;
            default:
                return;
        }
    }

    private void ResultGridCell_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!(e.OriginalSource as DependencyObject).TryFindParent<DataGridCell, DataGrid>(out var cell))
            return;

        if (cell.DataContext is not ResultData row)
            return;

        object? param = cell.Column.Header switch
        {
            "Name" => row.Name,
            "Value" => row.RawValue,
            "Type" => row.Type,
            _ => null
        };

        var cm = cell.ContextMenu ?? new ContextMenu();
        cm.Items.Clear();

        var item = new MenuItem()
        {
            Header = "_Copy",
        };
        if (param is not null)
        {
            item.Click += (sender, e) =>
            {
                Clipboard.SetText($"{param}");
            };
        }
        cm.Items.Add(item);
    }

    private void ResultData_Click(object sender, RoutedEventArgs e)
    {
        if (!WpfUtils.TryFindParent<DataGrid>(sender as DependencyObject, out var dataGrid))
            return;

        var selectedIndex = dataGrid.SelectedIndex;
        e.Handled = (DataContext as ResultList)?.Toggle(selectedIndex) ?? false;
    }

    private static void FocusToResultGrid(DataGrid dataGrid, int rowIndex = 0)
    {
        if (dataGrid.Items.Count == 0)
            return;
        if (rowIndex < 0)
            rowIndex = dataGrid.SelectedIndex;
        var index = Math.Clamp(rowIndex, 0, dataGrid.Items.Count - 1);
        dataGrid.SelectedIndex = index;
        dataGrid.UpdateLayout();
        if (dataGrid.ItemContainerGenerator.ContainerFromIndex(index) is not DataGridRow row)
            return;
        row.MoveFocus(new(FocusNavigationDirection.Next));
    }
}
