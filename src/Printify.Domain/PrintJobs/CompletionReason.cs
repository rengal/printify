namespace Printify.Domain.PrintJobs;

/// <summary>
/// Reason for stream completion.
/// </summary>
public enum PrintJobCompletionReason
{
    ClientDisconnected = 0,
    DataTimeout = 1
}
