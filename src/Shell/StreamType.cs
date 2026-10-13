using System.Management.Automation.Language;

namespace PSLauncher.Shell;

/// <summary>
/// Represents the normalized stream categories emitted by the PowerShell runtime.
///
/// This enum mirrors the numeric values of PowerShell's <see cref="RedirectionStream"/>
/// so that all pipeline outputs (Output, Error, Warning, Information, Verbose, Debug)
/// can be handled consistently within PSLauncher.
///
/// <para>
/// PowerShell emits different record types depending on the stream:
/// - Output: any object
/// - Error: <see cref="System.Management.Automation.ErrorRecord"/>
/// - Warning: <see cref="System.Management.Automation.WarningRecord"/>
/// - Information: <see cref="System.Management.Automation.InformationRecord"/>
/// - Verbose / Debug: strings or structured records
///
/// By mapping these to a unified <see cref="StreamType"/>, the host application
/// can process all emitted data through a single abstraction layer.
/// </para>
/// </summary>
public enum StreamType
{
    Output = RedirectionStream.Output,
    Error = RedirectionStream.Error,
    Warning = RedirectionStream.Warning,
    Verbose = RedirectionStream.Verbose,
    Debug = RedirectionStream.Debug,
    Information = RedirectionStream.Information,

    /// <summary>
    /// Represents progress records emitted by PowerShell (ID 7),
    /// which are not part of <see cref="RedirectionStream"/> but are
    /// included here for completeness so the runner can surface them
    /// through the same unified mechanism.
    /// </summary>
    Progress = 7
}
