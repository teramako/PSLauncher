using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PSLauncher;

public partial class MainWindow
{
    private void InitLocationBox()
    {
        LocationCollection.Add(App.Runspace.SessionStateProxy.Path.CurrentLocation.Path);
        LocationBox.ItemsSource = LocationCollection;
        LocationBox.SelectedIndex = 0;
    }
    /// <summary>
    /// PowerShell directory navigation history.
    /// <para>
    /// This collection is used as the <seealso cref="LocationBox"/> ComboBox's ItemsSource.
    /// </para>
    /// </summary>
    private ObservableCollection<string> LocationCollection { get; } = [];

    /// <summary>
    /// Handles the preview key down event for the location box.
    /// </summary>
    /// <remarks>
    /// If the Enter key is pressed, it confirms the selection in
    /// the location box and prevents further handling of the event.
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LocationBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter)
        {
            SelectionComfirmedOnLocationBox();
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Handles the preview mouse left button up event for the location box.
    /// </summary>
    /// <remarks>
    /// When a ComboBoxItem is selected via mouse click,
    /// it changes the current working directory in the PowerShell Runspace.
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LocationBox_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!LocationBox.IsDropDownOpen)
            return;

        if (WpfUtils.TryFindParent<ComboBoxItem, ComboBox>(e.OriginalSource as DependencyObject, out var item))
        {
            e.Handled = true;
            SelectionComfirmedOnLocationBox(item.DataContext as string);
        }
    }

    /// <summary>
    /// Confirms the selection in the location box and changes the current working directory in the PowerShell Runspace.
    /// </summary>
    /// <param name="path"></param>
    private void SelectionComfirmedOnLocationBox(string? path = null)
    {
        path ??= LocationBox.Text;
        try
        {
            if (path is not string text)
                return;

            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                LocationBox.Text = LocationBox.SelectedItem as string;
                return;
            }

            PushLocation(fullPath);
            App.Runspace.SessionStateProxy.Path.SetLocation(fullPath);
        }
        finally
        {
            LocationBox.IsDropDownOpen = false;
            CommandBox.Focus();
        }
    }

    /// <summary>
    /// Adds the specified path to the location collection and selects it.
    /// </summary>
    /// <remarks>
    /// This method does not change the <seealso cref="SessionStateProxy.Path"/>.
    /// </remarks>
    /// <param name="path">Current Working Directory</param>
    public void PushLocation(string path)
    {
        int index = LocationCollection.IndexOf(path);
        if (index < 0)
        {
            LocationCollection.Insert(0, path);
            LocationBox.SelectedIndex = 0;
        }
        else
        {
            LocationBox.SelectedIndex = index;
        }
    }
}
