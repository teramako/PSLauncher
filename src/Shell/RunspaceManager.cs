using System.Collections.Concurrent;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSLauncher.Shell;

internal static class RunspaceManager
{
    private static readonly ConcurrentDictionary<Runspace, SemaphoreSlim> locks= [];
    private static readonly ConcurrentDictionary<Runspace, ConcurrentDictionary<PowerShellRunner, bool>> shells = [];

    /// <summary>
    /// Get semaphore for the <paramref name="runspace"/>
    /// </summary>
    /// <param name="runspace"></param>
    /// <returns></returns>
    internal static SemaphoreSlim GetSemaphore(this Runspace runspace)
    {
        return locks.GetOrAdd(runspace, _ =>
        {
            runspace.StateChanged += OnStateChangedAsync;
            return new(1, 1);
        });
    }

    /// <summary>
    /// Unregier the <paramref name="runspace"/> from <seealso cref="RunspaceManager"/>
    /// </summary>
    /// <param name="runspace"></param>
    /// <returns></returns>
    /// <remarks>
    /// All <seealso cref="PowerShellRunner"/>s related the <paramref name="runspace"/> will be disposed.
    /// </remarks>
    internal static async Task UnregisterRunspaceAsync(Runspace runspace)
    {
        if (shells.TryRemove(runspace, out var runners))
        {
            var tasks = runners.Keys.Select(runner => runner.DisposeAsync().AsTask());
            await Task.WhenAll(tasks);
        }
        if (locks.TryRemove(runspace, out var semaphore))
        {
            runspace.StateChanged -= OnStateChangedAsync;
            semaphore.Dispose();
        }
    }

    private static async void OnStateChangedAsync(object? sender, RunspaceStateEventArgs e)
    {
        if (sender is not Runspace runspace)
            return;
        switch (e.RunspaceStateInfo.State)
        {
            case RunspaceState.Closed:
            case RunspaceState.Closing:
                await UnregisterRunspaceAsync(runspace);
                break;
        }
    }

    /// <summary>
    /// Register the <paramref name="runner"/> of to <seealso cref="RunspaceManager"/>
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="shell"></param>
    /// <returns></returns>
    internal static bool RegisterShell(this PowerShellRunner runner, PowerShell shell)
    {
        var dict = shells.GetOrAdd(shell.Runspace, _ => []);
        return dict.TryAdd(runner, default);
    }

    /// <summary>
    /// Unregister the <paramref name="runner"/> from <seealso cref="RunspaceManager"/>
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="shell"></param>
    /// <returns></returns>
    internal static bool UnregisterShell(this PowerShellRunner runner, PowerShell shell)
    {
        if (shells.TryGetValue(shell.Runspace, out var runners))
        {
            return runners.TryRemove(runner, out _);
        }
        return false;
    }

    internal static IEnumerable<PowerShellRunner> GetAvailableRunners()
    {
        return shells.Values.SelectMany(runners => runners.Keys)
                            .Where(runner => !runner.IsDisposed && !runner.IsRunning);
    }

    private readonly static Lazy<InitialSessionState> initialSessionState = new(() =>
    {
        var initialSessionState = InitialSessionState.CreateDefault2();
        initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;

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
        psModulePaths.Add(Path.Join(App.PwshInstallPath, "Modules"));

        initialSessionState.ImportPSModulesFromPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PSModules"));

        initialSessionState.EnvironmentVariables.Add(
            new SessionStateVariableEntry("PSModulePath",
                                          string.Join(Path.PathSeparator, psModulePaths),
                                          "PSModulePath for PSLauncher"));
        return initialSessionState;

    });

    internal static Runspace CreateRunspace(string name)
    {
        var runSpace = RunspaceFactory.CreateRunspace(initialSessionState.Value);
        runSpace.Name = name;
        runSpace.Open();

        // Set home as initial directory 
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        runSpace.SessionStateProxy.Path.SetLocation(home);

        locks.GetOrAdd(runSpace, _ => new(1, 1));

        return runSpace;
    }

    private static readonly Lazy<Runspace> _mainRunspace = new(static () => CreateRunspace("MainRunspace"));
    private static readonly Lazy<Runspace> _subRunspace = new(static () => CreateRunspace("SubRunspace"));

    /// <summary>
    /// Main Runspace used for executing command and getting completions
    /// </summary>
    public static Runspace MainRunspace => _mainRunspace.Value;

    /// <summary>
    /// Sub Runspace used for the other utlilities
    /// </summary>
    public static Runspace SubRunspace => _subRunspace.Value;
}
