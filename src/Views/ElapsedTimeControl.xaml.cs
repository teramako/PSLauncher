using System.Diagnostics;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PSLauncher.Views;

public partial class ElapsedTimeControl : UserControl
{
    private const int interval_miliseonds = 200;

    public ElapsedTimeControl()
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
        DependencyProperty.Register(nameof(Elapsed), typeof(TimeSpan), typeof(ElapsedTimeControl));

    public void Start()
    {
        stopwatch.Restart();
        timer.Start();
    }

    public void Stop()
    {
        timer.Stop();
        stopwatch.Stop();
        Elapsed = stopwatch.Elapsed;
    }
}
