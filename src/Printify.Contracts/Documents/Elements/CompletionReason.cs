namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Reason for stream completion.
/// </summary>
public enum CompletionReason
{
    ClientDisconnected = 0,
    DataTimeout = 1
}
