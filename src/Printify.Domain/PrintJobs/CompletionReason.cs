namespace Printify.Domain.PrintJobs;

/// <summary>
/// Reason for stream completion.
/// </summary>
public enum PrintJobCompletionReason
{
    ClientDisconnected = 0,
    Canceled = 1,
    Faulted = 2,
    DataTimeout = 3
}
