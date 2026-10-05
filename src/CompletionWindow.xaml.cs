using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSLauncher;

public partial class CompletionWindow : Window
{
    public MainWindow MainWindow => (MainWindow)Application.Current.MainWindow;

    public CompletionWindow(CommandCompletion commandCompletion)
    {
        InitializeComponent();
        DataContext = this;
        CurrentCommandCompletion = commandCompletion;
        SetTheme();
    }

    private void SetTheme()
    {
#pragma warning disable WPF0001
        switch (ThemeMode.Value)
        {
            case "Dark":
                Resources["PopupBackgroundColor"] = Color.FromRgb(44, 44, 44);
                break;
            case "Light":
            default:
                Resources["PopupBackgroundColor"] = Color.FromRgb(211, 211, 211);
                break;

        }
#pragma warning restore WPF0001
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_ACTIVATEAPP)
        {
            bool activated = wParam != IntPtr.Zero;
            if (!activated)
            {
                // Hide Completion Window when the other application is activated
                Hide();
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Handle the Loaded event of the window to set the window styles and add a hook for window messages
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);

        HwndSource source = HwndSource.FromHwnd(hwnd);
        source.AddHook(WndProc);
    }

    /// <summary>
    /// Ensure detail popup is closed, when Visibility is changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        PopupTimer?.Stop();
        DetailPopup.IsOpen = false;
    }

    /// <summary>
    /// Get or set the current <seealso cref="CommandCompletion"/> object
    /// </summary>
    public CommandCompletion CurrentCommandCompletion
    {
        get;
        set
        {
            CompletionList.ItemsSource = value.CompletionMatches;
            field = value;
        }
    }

    /// <summary>
    /// Select the next item in the completion list
    /// </summary>
    public void SelectNext()
    {
        var currentIndex = CompletionList.SelectedIndex;
        var index = currentIndex;
        if (currentIndex < 0)
        {
            index = 0;
        }
        else if (currentIndex >= CompletionList.Items.Count - 1)
        {
            index = -1;
        }
        else
        {
            index++;
        }
        CompletionList.SelectedIndex = index;
        ScrollIntoView(index);
    }

    /// <summary>
    /// Select the previous item in the completion list
    /// </summary>
    public void SelectPrevious()
    {
        var currentIndex = CompletionList.SelectedIndex;
        var index = currentIndex switch
        {
            < 0 => CompletionList.Items.Count - 1,
            0 => -1,
            _ => currentIndex - 1
        };
        CompletionList.SelectedIndex = index;
        ScrollIntoView(index);
    }

    /// <summary>
    /// Scroll the selected item into view in the completion list
    /// </summary>
    /// <param name="index"></param>
    private void ScrollIntoView(int index)
    {
        if (index < 0 || index > CurrentCommandCompletion.CompletionMatches.Count - 1)
            return;
        CompletionList.ScrollIntoView(CurrentCommandCompletion.CompletionMatches[index]);
    }

    /// <summary>
    /// DispatcherTimer to show the detail popup for the selected item in the completion list
    /// </summary>
    private DispatcherTimer? PopupTimer;
    /// <summary>
    /// Handle the tick event of the <seealso cref="PopupTimer"/>
    /// to show the detail popup for the selected item in the completion list
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PopupTimer_Tick(object? sender, EventArgs e)
    {
        PopupTimer?.Stop();
        PopupDetail();
    }
    /// <summary>
    /// Show the detail popup for the selected item in the completion list
    /// </summary>
    private void PopupDetail()
    {
        if (CompletionList.SelectedItem is CompletionResult item && !string.IsNullOrEmpty(item.ToolTip))
        {
            DetailText.Text = item.ToolTip;
            var container = CompletionList.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            if (container is null)
                return;
            DetailPopup.PlacementTarget = container;
            DetailPopup.IsOpen = true;
        }
    }

    /// <summary>
    /// Handle selection changed event on the completion list
    /// to update the current match index and show the detail popup
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CurrentCommandCompletion.CurrentMatchIndex = CompletionList.SelectedIndex;
        DetailPopup.IsOpen = false;
        PopupTimer ??= new(TimeSpan.FromMicroseconds(200), DispatcherPriority.Background, PopupTimer_Tick, Dispatcher);
        PopupTimer.Stop();
        PopupTimer.Start();
    }

    /// <summary>
    /// Handle double-click event on the completion list
    /// to accept the selected completion
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDoubleClick(object sender, RoutedEventArgs e)
    {
        if (CompletionList.SelectedIndex < 0)
            return;
        CurrentCommandCompletion.CurrentMatchIndex = CompletionList.SelectedIndex;
        MainWindow.AcceptCompletion(CurrentCommandCompletion);
        Hide();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (TryOnKeyDown(e))
        {
            e.Handled = true;
            return;
        }
        base.OnPreviewKeyDown(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (TryOnKeyDown(e))
        {
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Try to handle the key down event
    /// </summary>
    /// <param name="e"></param>
    /// <returns>true if the event was handled; otherwise, false</returns>
    public bool TryOnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                MainWindow.AcceptCompletion(CurrentCommandCompletion);
                Hide();
                return true;
            case Key.Escape:
                Hide();
                return true;
            case Key.Down:
            case Key.N when Keyboard.Modifiers is ModifierKeys.Control:
            case Key.Tab when Keyboard.Modifiers is ModifierKeys.None:
                SelectNext();
                return true;
            case Key.Up:
            case Key.P when Keyboard.Modifiers is ModifierKeys.Control:
            case Key.Tab when Keyboard.Modifiers is ModifierKeys.Shift:
                SelectPrevious();
                return true;
        }
        return false;
    }
}
