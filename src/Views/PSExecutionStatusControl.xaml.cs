using System.Diagnostics;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PSLauncher.Views;

public partial class PSExecutionStatusControl : UserControl
{
    private const int interval_miliseonds = 200;

    public PSExecutionStatusControl()
    {
        InitializeComponent();
        timer = new(TimeSpan.FromMicroseconds(interval_miliseonds),
                    DispatcherPriority.Background,
                    Timer_Tick,
                    Dispatcher);
    }
    private readonly Stopwatch stopwatch = new();
    private readonly DispatcherTimer timer;

    private void Timer_Tick(object? sender, EventArgs e)
    {
        Elapsed = stopwatch.Elapsed;
    }

    public TimeSpan Elapsed
    {
        get => (TimeSpan)GetValue(ElapsedProperty);
        set => SetValue(ElapsedProperty, value);
    }

    public static readonly DependencyProperty ElapsedProperty =
        DependencyProperty.Register(nameof(Elapsed), typeof(TimeSpan), typeof(PSExecutionStatusControl));

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(PSExecutionStatusControl));

    public string? TooltipText
    {
        get => field = (string)GetValue(TooltipTextProperty);
        set
        {
            var val = string.IsNullOrEmpty(value) ? "Stop" : value;
            SetValue(TooltipTextProperty, val);
            field = val;
            if (field != "Stop")
            {
                MessageTooltip.IsOpen = true;
            }

        }
    } = "Stop";
    public static readonly DependencyProperty TooltipTextProperty =
        DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(PSExecutionStatusControl));

    public void Start()
    {
        stopwatch.Restart();
        timer.Start();
        IsRunning = true;
        TooltipText = null;
    }

    public void Stop()
    {
        timer.Stop();
        stopwatch.Stop();
        Elapsed = stopwatch.Elapsed;
        IsRunning = false;
    }
}
