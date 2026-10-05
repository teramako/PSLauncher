using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace PSLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    static App()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolvePwshAssembly;
    }
    private static Assembly? ResolvePwshAssembly(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        var dll = Path.Join(PwshInstallPath, $"{name}.dll");
        if (File.Exists(dll))
        {
            Debug.Print($"Load assembly '{name}' from {dll}");
            return Assembly.LoadFrom(dll);
        }
        return null;
    }

    public App() : base()
    {
        LoadPwshAssemblies();
    }

    private static void LoadPwshAssemblies()
    {
        var dir = new DirectoryInfo(PwshInstallPath);
        foreach (var dll in dir.GetFiles("*.dll"))
        {
            switch (dll.Name)
            {
                case "Microsoft.PowerShell.Commands.Diagnostics.dll":
                case "Microsoft.PowerShell.Commands.Management.dll":
                case "Microsoft.PowerShell.Commands.Utility.dll":
                case "Microsoft.PowerShell.ConsoleHost.dll":
                case "Microsoft.PowerShell.Security.dll":
                case "Microsoft.WSMan.Management.dll":
                    Debug.Print($"Load {dll.Name} from PowerShell 7");
                    Load(dll.FullName);
                    break;
            }

        }
        static void Load(string path)
        {
            try
            {
                Assembly.LoadFrom(path);
            }
            catch
            { }
        }
    }
    public static string PwshInstallPath => field ??= GetPwshInstallPath();

    private static string GetPwshInstallPath()
    {
        const string baseKey = @"SOFTWARE\Microsoft\PowerShellCore\InstalledVersions";

        using var root = Registry.LocalMachine.OpenSubKey(baseKey)
                         ?? throw new InvalidOperationException("PowerShell 7 is not installed.");

        foreach (var subKeyName in root.GetSubKeyNames())
        {
            using var subKey = root.OpenSubKey(subKeyName);
            var path = subKey?.GetValue("InstallLocation") as string;
            if (!string.IsNullOrEmpty(path))
                return path;
        }
        throw new InvalidOperationException("PowerShell 7 is not installed.");
    }

    public System.Windows.Forms.NotifyIcon? NotifyIcon { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        InitializeNotifyIcon();

        MainWindow = new MainWindow();
        MainWindow.SourceInitialized += MainWindow_SourceInitialized;
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_isRunspaceCreated)
        {
            try
            {
                Runspace.Close();
            }
            catch { /* ignore */ }
            Runspace.Dispose();
        }
        if (MainWindow is not null)
        {
            var handle = new WindowInteropHelper(MainWindow).Handle;
            NativeMethods.UnregisterHotKey(handle, 1);
        }
        base.OnExit(e);
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        MainWindow.SourceInitialized -= MainWindow_SourceInitialized;
        var handle = new WindowInteropHelper(MainWindow).Handle;
        var source = HwndSource.FromHwnd(handle);
        source.AddHook(WndProc);
        NativeMethods.RegisterHotKey(handle,
                                     1,
                                     NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
                                     (uint)KeyInterop.VirtualKeyFromKey(Key.P));
    }

    private void InitializeNotifyIcon()
    {
        var startInfo = GetResourceStream(new Uri("powershell-blue.ico", UriKind.Relative));
        using var iconStream = startInfo.Stream;
        NotifyIcon = new System.Windows.Forms.NotifyIcon()
        {
            Icon = new System.Drawing.Icon(iconStream, new System.Drawing.Size(16, 16)),
            Visible = true,
            Text = "PSLauncher"
        };

        NotifyIcon.Click += (s, args) =>
        {
            ToggleWindow();
        };

        var contextMenu = NotifyIcon.ContextMenuStrip = new();
        contextMenu.Items.Add("&Open", null, (s, args) => ShowWindow());
        contextMenu.Items.Add("&Exit", null, (s, args) => Shutdown(0));
    }

    private void ShowWindow()
    {
        if (!MainWindow.IsVisible)
            MainWindow.Show();
        MainWindow.Activate();
    }

    private void ToggleWindow()
    {
        if (MainWindow.IsVisible)
        {
            MainWindow.Hide();
        }
        else
        {
            ShowWindow();
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY)
        {
            ToggleWindow();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static bool _isRunspaceCreated;

    private static Runspace CreateRunspace()
    {
        var initialSessionState = InitialSessionState.CreateDefault2();
        initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;

        #region Setting Modules (PSModulePath and Import)
        List<string> psModulePaths = [];

        var myDocumentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrEmpty(myDocumentsDir))
        {
            var modulePath = Path.Join(myDocumentsDir, "PSLauncher", "Modules");
            if (Directory.Exists(modulePath))
            {
                psModulePaths.Add(modulePath);
                var startupModule = Path.Join(modulePath, "PSLauncher.psm1");
                if (File.Exists(startupModule))
                {
                    initialSessionState.ImportPSModule(startupModule);
                }
            }
            psModulePaths.Add(Path.Join(myDocumentsDir, "PowerShell", "Modules"));
        }
        psModulePaths.Add(Path.Join(PwshInstallPath, "Modules"));

        initialSessionState.EnvironmentVariables.Add(
            new SessionStateVariableEntry("PSModulePath",
                                          string.Join(Path.PathSeparator, psModulePaths),
                                          "PSModulePath for PSLauncher")
        );
        #endregion

        var runSpace = RunspaceFactory.CreateRunspace(initialSessionState);
        runSpace.Open();

        // Set home as initial directory 
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        runSpace.SessionStateProxy.Path.SetLocation(home);

        return runSpace;
    }

    public static Runspace Runspace
    {
        get
        {
            if (field is not null)
                return field;

            field = CreateRunspace();
            _isRunspaceCreated = true;
            return field;
        }
    }
}
