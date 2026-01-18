namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Non-visual debug information for protocol state changes.
/// Maintains 1-to-1 correspondence with protocol commands (has CommandRaw, LengthInBytes).
/// Mixed with visual primitives in Canvas.Items to show command sequence.
/// </summary>
public sealed record DebugInfo(
    string DebugType,
    IReadOnlyDictionary<string, string> Parameters,
    byte[] CommandRaw,
    int LengthInBytes,
    IReadOnlyList<string> CommandDescription) : BaseElement;
