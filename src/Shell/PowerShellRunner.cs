using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Channels;

namespace PSLauncher.Shell;

internal class PowerShellRunner : IAsyncDisposable, INotifyPropertyChanged
{
    public static PowerShellRunner Main => field is null || field._isDisposed
                                           ? (field = new(RunspaceManager.MainRunspace))
                                           : field;

    public static PowerShellRunner Sub => field is null || field._isDisposed
                                           ? (field = new(RunspaceManager.SubRunspace))
                                           : field;
    public static PowerShellRunner Create(Runspace runspace) => new(runspace);

    private PowerShellRunner(Runspace runspace)
    {
        _shell = PowerShell.Create(runspace);
        _shell.InvocationStateChanged += OnInvocationStateChanged;
        _semaphore = runspace.GetSemaphore();
        RunspaceManager.RegisterShell(this, _shell);
    }

    private readonly PowerShell _shell;

    /// <summary>
    /// Semaphore of the runspace
    /// </summary>
    private readonly SemaphoreSlim _semaphore;

    public bool IsRunning
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(IsRunning)));
            }
        }
    }

    public Exception? InvocationErrorReason
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(InvocationErrorReason)));
            }
        }
    }

    private void Clear()
    {
        _shell.Commands.Clear();
        _shell.Streams.ClearStreams();
    }

    public async Task<PSDataCollection<PSObject>> SimpleInvokeAsync(PSCommand commands, CancellationToken cancellationToken = default)
    {
        return await SimpleInvokeAsync<PSObject>(commands, cancellationToken);
    }

    public async Task<PSDataCollection<T>> SimpleInvokeAsync<T>(PSCommand commands, CancellationToken cancellationToken = default)
    {
        PSDataCollection<T> output = [];
        await InvokeAsync<object, T>(commands, null, output, false, cancellationToken);
        return output;
    }

    public async Task<PSDataCollection<PSObject>> InvokeAsync(string script,
                                                              bool useLocalScope = false,
                                                              CancellationToken cancellationToken = default)
    {
        PSCommand cmd = new();
        cmd.AddScript(script, useLocalScope);
        PSDataCollection<PSObject> output = [];
        await InvokeAsync<object, PSObject>(cmd, null, output, true, cancellationToken);
        return output;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="commands"></param>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="useOutputHandler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ParseException"></exception>
    /// <exception cref="PipelineStoppedException"></exception>
    private async Task<bool> InvokeAsync<TInput, TResult>(PSCommand commands,
                                                          PSDataCollection<TInput>? input,
                                                          PSDataCollection<TResult> output,
                                                          bool useOutputHandler = false,
                                                          CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await _semaphore.WaitAsync(cancellationToken);

        if (useOutputHandler)
        {
            output.DataAdded += OnOutputDataAdded;
            RegisterDataAddedHandler(OnDataAdded);
        }

        try
        {
            try
            {
                _shell.Commands = commands;
                Task<PSDataCollection<PSObject>> invokeTask = _shell.InvokeAsync(input, output);
                Task cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
                Task completed = await Task.WhenAny(invokeTask, cancelTask);

                bool isSuccess = false;

                if (completed == cancelTask)
                {
                    _shell.Stop();
                }

#if DEBUG
                Debug.Print($"PowerShell Execution: {_shell.InvocationStateInfo.State}");
                Debug.Print($"Exception: {_shell.InvocationStateInfo.Reason}");
#endif
                switch (_shell.InvocationStateInfo)
                {
                    case { State: PSInvocationState.Completed }:
                        isSuccess = true;
                        break;
                    case { Reason: Exception ex }:
                        throw ex;
                }
                
                return isSuccess;
            }
            finally
            {
                if (useOutputHandler)
                    UnregisterDataAddedHandler(OnDataAdded);
                Clear();
            }
        }
        finally
        {
            _semaphore.Release();
        }
        
    }

    //public static Channel<PSDataRecord> CreateChannel(int capacity = 256,
    //                                                  BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
    //{
    //    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
    //    return Channel.CreateBounded<PSDataRecord>(new BoundedChannelOptions(capacity)
    //    {
    //        FullMode = fullMode,
    //        SingleReader = true,
    //        SingleWriter = true
    //    });
    //}
    //public async Task<bool> InvokeAsync<T>(PSDataCollection<T>? input,
    //                                       ChannelWriter<PSDataRecord> channelWriter,
    //                                       CancellationToken cancellationToken = default)
    //{
    //    ObjectDisposedException.ThrowIf(_isDisposed, this);

    //    await _semaphore.WaitAsync(cancellationToken);

    //    PSDataCollection<PSObject> output = [];
    //    output.DataAdded += DataAddedHandler;
    //    RegisterDataAddedHandler(DataAddedHandler);

    //    try
    //    {
    //        try
    //        {
    //            Task invokeTask = _shell.InvokeAsync(input, output).ContinueWith(_ =>
    //            {
    //               channelWriter.TryComplete();
    //            }, cancellationToken);
    //            Task cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
    //            Task completed = await Task.WhenAny(invokeTask, cancelTask);
    //            if (completed == cancelTask)
    //            {
    //                _shell.Stop();
    //                channelWriter.TryComplete(_shell.InvocationStateInfo.Reason);
    //                return false;
    //            }
    //            return true;
    //        }
    //        finally
    //        {
    //            UnregisterDataAddedHandler(DataAddedHandler);
    //            Clear();
    //        }
    //    }
    //    finally
    //    {
    //        _semaphore.Release();
    //    }

    //    void DataAddedHandler(object? sender, DataAddedEventArgs e)
    //    {
    //        if (TryGetRecordFromEvent(sender, e, out var record))
    //        {
    //            channelWriter.TryWrite(record);
    //        }
    //    }
    //}

    private void OnInvocationStateChanged(object? sender, PSInvocationStateChangedEventArgs e)
    {
        switch (e.InvocationStateInfo.State)
        {
            case PSInvocationState.Running:
                IsRunning = true;
                break;
            case PSInvocationState.Completed:
                IsRunning = false;
                break;
            case PSInvocationState.Failed:
                InvocationErrorReason = e.InvocationStateInfo.Reason;
                IsRunning = false;
                break;
        }
    }

    private void OnOutputDataAdded(object? sender, DataAddedEventArgs e)
    {
        var record = (sender as PSDataCollection<PSObject>)?[e.Index];
        if (record is null) return;
        OnOutput?.Invoke(_shell, new(new(StreamType.Output, record)));
    }

    private static bool TryGetRecordFromEvent(object? sender, DataAddedEventArgs e, out PSDataRecord record)
    {
        switch (sender)
        {
            case PSDataCollection<ErrorRecord> errorRecords:
                record = new(StreamType.Error, errorRecords[e.Index]);
                break;
            case PSDataCollection<WarningRecord> warningRecords:
                record = new(StreamType.Warning, warningRecords[e.Index]);
                break;
            case PSDataCollection<VerboseRecord> verboseRecords:
                record = new(StreamType.Verbose, verboseRecords[e.Index]);
                break;
            case PSDataCollection<DebugRecord> debugRecords:
                record = new(StreamType.Debug, debugRecords[e.Index]);
                break;
            case PSDataCollection<InformationRecord> informationRecords:
                record = new(StreamType.Information, informationRecords[e.Index]);
                break;
            case PSDataCollection<ProgressRecord> progressRecords:
                record = new(StreamType.Progress, progressRecords[e.Index]);
                break;
            case PSDataCollection<PSObject> outputRecords:
                record = new(StreamType.Output, outputRecords[e.Index]);
                break;
            default:
                record = default;
                return false;
        }
        return true;
    }

    private void OnDataAdded(object? sender, DataAddedEventArgs e)
    {
        if (TryGetRecordFromEvent(sender, e, out var record))
        {
            OnOutput?.Invoke(_shell, new(record));
        }
    }

    private void RegisterDataAddedHandler(EventHandler<DataAddedEventArgs> handler)
    {
        _shell.Streams.Error.DataAdded += handler;
        _shell.Streams.Warning.DataAdded += handler;
        _shell.Streams.Verbose.DataAdded += handler;
        _shell.Streams.Debug.DataAdded += handler;
        _shell.Streams.Information.DataAdded += handler;
    }
    private void UnregisterDataAddedHandler(EventHandler<DataAddedEventArgs> handler)
    {
        _shell.Streams.Error.DataAdded -= handler;
        _shell.Streams.Warning.DataAdded -= handler;
        _shell.Streams.Verbose.DataAdded -= handler;
        _shell.Streams.Debug.DataAdded -= handler;
        _shell.Streams.Information.DataAdded -= handler;
    }

    private volatile bool _isDisposed;
    public bool IsDisposed => _isDisposed;
    public event EventHandler<PSOutputEventArgs>? OnOutput;
    public event PropertyChangedEventHandler? PropertyChanged;

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_isDisposed)
                return;

            Clear();
            _shell.Dispose();
            _isDisposed = true;
            RunspaceManager.UnregisterShell(this, _shell);
            GC.SuppressFinalize(this);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
