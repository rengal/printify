namespace Printify.Web.Contracts.Documents.Shared.Elements;

/// <summary>
/// Reason for stream completion.
/// </summary>
public enum CompletionReason
{
    ClientDisconnected,
    DataTimeout
}

/// <summary>
/// Canonical string tokens for completion reasons exposed via the web API.
/// </summary>
public static class CompletionReasonNames
{
    public const string ClientDisconnected = "clientDisconnected";
    public const string DataTimeout = "dataTimeout";
}
