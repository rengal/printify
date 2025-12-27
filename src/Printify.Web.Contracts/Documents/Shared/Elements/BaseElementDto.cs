using System.Collections.Generic;

namespace Printify.Web.Contracts.Documents.Shared.Elements;

/// <summary>
/// Base contract for all document elements with sequencing metadata.
/// </summary>
public abstract record BaseElementDto
{
    /// <summary>
    /// Raw command bytes encoded for debugging or UI display.
    /// </summary>
    public string CommandRaw { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of the command (one entry per line).
    /// </summary>
    public IReadOnlyList<string> CommandDescription { get; init; } = [];

    /// <summary>
    /// Length of the command in bytes.
    /// </summary>
    public int LengthInBytes { get; init; }
}

/// <summary>
/// Marker base for elements that produce visible output during printing.
/// </summary>
public abstract record PrintingElementDto : BaseElementDto;

/// <summary>
/// Marker base for elements that modify state or encode diagnostics without rendering.
/// </summary>
public abstract record NonPrintingElementDto : BaseElementDto;
