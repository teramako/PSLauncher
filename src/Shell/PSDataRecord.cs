namespace PSLauncher.Shell;

/// <summary>
/// Represents a single piece of data emitted from the PowerShell execution pipeline,
/// normalized into a common structure regardless of the original stream type.
///
/// This record is used by <seealso cref="PowerShellRunner"/> to unify all output streams
/// (Output, Error, Warning, Information, Verbose, Debug) into a single format
/// so that the caller can process them consistently.
///
/// <para>
/// <see cref="StreamType"/> identifies which PowerShell stream produced the value,
/// and <see cref="Value"/> contains the raw object associated with that stream.
/// </para>
/// </summary>
/// <param name="StreamType">
/// The PowerShell stream from which this record originated.
/// </param>
/// <param name="Value">
/// The raw value emitted by the stream. This may be a primitive value,
/// a PSObject, or a PowerShell-specific record type such as ErrorRecord
/// or InformationRecord.
/// </param>
public readonly record struct PSDataRecord(StreamType StreamType, object? Value)
{
}
