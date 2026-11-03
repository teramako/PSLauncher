using PSLauncher.Models;
using System.ComponentModel;
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

    private List<DataGridColumn>? ObjectDataGridColumns;
    private List<DataGridColumn>? FileDataGridColumns;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var newValue = e.NewValue as ResultList;
        Root.Visibility = newValue is { Kind: ResultDataKind.Simple or ResultDataKind.NotSpecified }
                        ? Visibility.Collapsed
                        : Visibility.Visible;
        ChangeColumns(newValue, e.OldValue as ResultList);
        SetContextMenu(newValue);
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
            case ResultDataKind.Objects:
                columns = ObjectDataGridColumns ??=
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
    private void SetContextMenu(ResultList? dataList)
    {
        if (dataList is null)
            return;
        switch (dataList.Kind)
        {
            case ResultDataKind.Objects:
                Root.ContextMenu = TryFindResource("ObjectsContextMenu") as ContextMenu;
                break;
            case ResultDataKind.Files:
                Root.ContextMenu = TryFindResource("FilesContextMenu") as ContextMenu;
                break;
            default:
                Root.ContextMenu = null;
                break;
        }
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
            case Key.P when Keyboard.Modifiers is ModifierKeys.Control:
                var item = data.Items[selectedIndex];
                Commands.PinItem.Execute(item, dataGrid);
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

    private void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not ResultList dataList)
            return;
        var col = e.Column;
        var currentDirection = col.SortDirection;
        ListSortDirection? sortDirection = currentDirection switch
        {
            ListSortDirection.Ascending => ListSortDirection.Descending,
            ListSortDirection.Descending => null,
            _ => ListSortDirection.Ascending,
        };
        if (dataList.Sort(sortDirection, col.SortMemberPath))
        {
            col.SortDirection = sortDirection;
            e.Handled = true;
        }
    }

    private void OnCanCopyValue(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = Root.SelectedItem is IResultData item && item.ValueType switch
        {
            ObjectValueType.Null => false,
            ObjectValueType.String or ObjectValueType.Number or ObjectValueType.Boolean
            or ObjectValueType.Enum or ObjectValueType.Temporal or ObjectValueType.Exception
            or ObjectValueType.File or ObjectValueType.Directory => true,
            _ => !string.IsNullOrEmpty(Utils.ObjectToString(item.RawValue))
        };
        e.Handled = true;
    }

    private void OnCopyValue(object sender, ExecutedRoutedEventArgs e)
    {
        if (Root.SelectedItem is IResultData item)
        {
            e.Handled = true;
            switch (item.ValueType)
            {
                case ObjectValueType.File or ObjectValueType.Directory:
                    var filePath= $"{item.RawValue}";
                    var data = new DataObject();
                    data.SetFileDropList([filePath]);
                    data.SetText(filePath);
                    Clipboard.SetDataObject(data, true);
                    break;
                case ObjectValueType.String or ObjectValueType.Number:
                case ObjectValueType.Boolean or ObjectValueType.Enum:
                case ObjectValueType.Temporal or ObjectValueType.Exception:
                    Clipboard.SetText($"{item.RawValue}");
                    break;
                default:
                    var text = Utils.ObjectToString(item.RawValue);
                    if (string.IsNullOrEmpty(text))
                        return;
                    Clipboard.SetText(text);
                    break;
            }
        }
    }
    private void OnCanCopyRow(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = Root.SelectedItem is IResultData;
        e.Handled = true;
    }

    private void OnCopyRow(object sender, ExecutedRoutedEventArgs e)
    {
        var text = Root.SelectedItem switch
        {
            ResultData item => $"{item.Name}\t{Utils.ObjectToString(item.RawValue)}\t{item.Type}",
            FileResultData file => $"{file.Name}\t{file.LastWriteTime}\t{file.Size}",
            _ => string.Empty
        };
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
            e.Handled = true;
        }
    }
}
