using PSLauncher.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PSLauncher.Views;

/// <summary>
/// ExpandableDataGridControl.xaml
/// </summary>
public partial class ExpandableDataGridControl : UserControl, IResultContentControl
{
    public ExpandableDataGridControl()
    {
        InitializeComponent();
    }

    private List<DataGridColumn>? ComplexDataGridColumns;
    private List<DataGridColumn>? FileDataGridColumns;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var newValue = e.NewValue as ResultList;
        Root.Visibility = newValue is { Kind: ResultDataKind.Simple or ResultDataKind.NotSpecified }
                        ? Visibility.Collapsed
                        : Visibility.Visible;
        ChangeColumns(newValue, e.OldValue as ResultList);
    }

    private void ChangeColumns(ResultList? newList, ResultList? oldList)
    {
        if (newList is null)
            return;
        if (oldList is not null && oldList.Kind == newList.Kind)
            return;

        List<DataGridColumn> columns;
        switch (newList.Kind)
        {
            case ResultDataKind.Complex:
                columns = ComplexDataGridColumns ??=
                [
                    (DataGridTemplateColumn)Resources["NameColumn"],
                    (DataGridTemplateColumn)Resources["ValueColumn"],
                    (DataGridTextColumn)Resources["TypeColumn"]
                ];
                break;
            case ResultDataKind.Files:
                columns = FileDataGridColumns ??=
                [
                    (DataGridTemplateColumn)Resources["FileNameColumn"],
                    (DataGridTextColumn)Resources["FileLastModifiedColumn"],
                    (DataGridTextColumn)Resources["FileSizeColumn"]
                ];
                break;
            default:
                return;
        }
        Root.Columns.Clear();
        columns.ForEach(Root.Columns.Add);
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is DataGrid dataGrid)
        {
            FocusToResultGrid(dataGrid, -1);
        }
    }

    public void FocusToContent()
    {
        Dispatcher.BeginInvoke(() =>
        {
            FocusToResultGrid(Root, -1);
        }, DispatcherPriority.Background);
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

    private void OnRowPreviewKeyDown(object sender, KeyEventArgs e)
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

    private void OnToggleRowExpander(object sender, RoutedEventArgs e)
    {
        if (!WpfUtils.TryFindParent<DataGrid>(sender as DependencyObject, out var dataGrid))
            return;

        var selectedIndex = dataGrid.SelectedIndex;
        e.Handled = (DataContext as ResultList)?.Toggle(selectedIndex) ?? false;
    }
}
