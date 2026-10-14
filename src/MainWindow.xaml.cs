using PSLauncher.Models;
using PSLauncher.Shell;
using PSLauncher.Views;
using System.ComponentModel;
using System.Management.Automation;
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

    private void StopExcution(object sender, RoutedEventArgs e)
    {
        if (ElapsedTimeControl.IsRunning
            && psExectionStopSource is { IsCancellationRequested: false })
        {
            psExectionStopSource.Cancel();
        }
    }

    private void CanStopExecution(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = ElapsedTimeControl.IsRunning;
    }

    public const string CommandPrefix = @"Start-Process";

    private CancellationTokenSource? psExectionStopSource;

    private async Task ExecuteScript(string script)
    {
        var runner = PowerShellRunner.Main;
        if (runner.IsRunning)
            return;

        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }
        if (script.StartsWith('!'))
        {
            script = $@"{CommandPrefix} {script[1..]}";
        }

        runner.OnOutput += OnOutput;

        StreamStatusPanel.ClearStreams();

        psExectionStopSource = new();
        using (psExectionStopSource)
        {
            try
            {
                PSDataCollection<PSObject> psResults;
                try
                {
                    ElapsedTimeControl.Start();
                    psResults = await runner.InvokeAsync(script, cancellationToken: psExectionStopSource.Token);
                }
                finally
                {
                    ElapsedTimeControl.Stop();
                }

                await ShowResult(psResults);
            }
            catch (PipelineStoppedException stoppedException)
            {
                // when canceled by user operation.
                if (psExectionStopSource.IsCancellationRequested)
                {
                    ElapsedTimeControl.TooltipText = stoppedException.Message;
                }
                else
                {
                    ShowResult(stoppedException);
                }
            }
            catch (ParseException parseException)
            {
                ElapsedTimeControl.TooltipText = parseException.Message;
            }
            catch (Exception ex)
            {
                ShowResult(ex);
            }
            finally
            {
                runner.OnOutput -= OnOutput;
                UpdateStatus();
            }
        }

        void OnOutput(object? sender, PSOutputEventArgs e)
        {
            StreamStatusPanel.Dispatcher.InvokeAsync(() => StreamStatusPanel.AddRecord(e.Record), DispatcherPriority.Background);
        }
    }

    private async Task ShowResult(PSDataCollection<PSObject> psDataCollection, CancellationToken cancellationToken = default)
    {
        var results = await ResultList.BuildAsync(psDataCollection, cancellationToken);
        ResultContent.Content = results;
    }
    private void ShowResult(object obj)
    {
        var results = ResultList.Build(obj);
        ResultContent.Content = results;
    }

    private void UpdateStatus()
    {
        var cwd = RunspaceManager.MainRunspace.SessionStateProxy.Path.CurrentLocation.Path;
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
