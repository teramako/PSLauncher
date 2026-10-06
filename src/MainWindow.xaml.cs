using PSLauncher.Models;
using PSLauncher.Views;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PSLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        CommandBox.Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        _completionWindow?.Close();
        _completionWindow = null;
        Hide();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var runspace = App.Runspace;
        runspace.AvailabilityChanged += Runspace_AvailabilityChanged;
        CommandBox.Focus();
        InitCommandBox();
        InitLocationBox();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (_completionWindow?.IsVisible ?? false)
            {
                _completionWindow.Hide();
            }
            DragMove();
        }
    }

    public const string CommandPrefix = @"Start-Process";

    private PowerShell? _currentPowerShell;
    public bool IsRunspaceRunning { get; private set; }

    private void Runspace_AvailabilityChanged(object? sender, RunspaceAvailabilityEventArgs e)
    {
        UpdateRunspaceAvailablity(e.RunspaceAvailability);
    }
    private void UpdateRunspaceAvailablity(RunspaceAvailability availability)
    {
        StopRunspaceButton.Dispatcher.InvokeAsync(() =>
        {
            StopRunspaceButton.IsEnabled = IsRunspaceRunning = availability switch
            {
                RunspaceAvailability.None or RunspaceAvailability.Available => false,
                _ => true,
            };
        }, DispatcherPriority.Background);
    }
    private void StopRunspace(object sender, RoutedEventArgs e)
    {
        if (IsRunspaceRunning)
        {
            _currentPowerShell?.Stop();
        }
    }

    private async Task ExecuteScript(string script)
    {
        if (IsRunspaceRunning)
            return;

        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }
        if (script.StartsWith('!'))
        {
            script = $@"{CommandPrefix} {script[1..]}";
        }
        using var ps = _currentPowerShell = PowerShell.Create(App.Runspace);
        try
        {
            ps.Commands.Clear();
            ps.AddScript(script);
            var results = await ps.InvokeAsync();

            UpdateStatus();
            ShowResult(results);
        }
        catch (PipelineStoppedException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            ShowResult(ex);
        }
        finally
        {
            _currentPowerShell = null;
        }
    }

    private void ShowResult(PSDataCollection<PSObject> psDataCollection)
    {
        var results = ResultList.Build(psDataCollection);
        ResultContent.Content = results;
    }
    private void ShowResult(object obj)
    {
        var results = ResultList.Build(obj);
        ResultContent.Content = results;
    }

    private void UpdateStatus()
    {
        var cwd = App.Runspace.SessionStateProxy.Path.CurrentLocation.Path;
        PushLocation(cwd);
    }

    private void ExitApplication(object sender, ExecutedRoutedEventArgs e)
    {
        Application.Current.Shutdown(0);
    }


    private void FocusToResultContent(object sender, ExecutedRoutedEventArgs e)
    {
        if (ResultContent.TryFindChild<ContentPresenter>(out var contentPresenter)
            && contentPresenter.TryFindChild<UserControl>(out var control))
        {
            if (control is IResultContentControl resultControl)
            {
                resultControl.FocusToContent();
            }
        }
    }
}
