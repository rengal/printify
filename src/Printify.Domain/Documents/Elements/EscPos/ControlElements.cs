namespace Printify.Domain.Documents.Elements.EscPos;

/// <summary>
/// An audible/attention bell signal.
/// </summary>
public sealed record Bell : NonPrintingElement;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
/// <param name="Mode">The type of cut to perform (full or partial).</param>
/// <param name="FeedMotionUnits">Feed distance in motion units before cutting (GS V m n).</param>
public sealed record CutPaper(PagecutMode Mode, int? FeedMotionUnits = null) : NonPrintingElement;

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Pin">Target drawer pin.</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record Pulse(int Pin, int OnTimeMs, int OffTimeMs)
    : NonPrintingElement;

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
public sealed record Initialize : NonPrintingElement;

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="AdditionalStatusByte">
/// Optional additional status byte value, if present in the protocol.
/// </param>
public sealed record GetPrinterStatus(
    byte StatusByte,
    byte? AdditionalStatusByte = null)
    : NonPrintingElement;

/// <summary>
/// DLE EOT n - Real-time status transmission request.
/// Client requests printer status; printer responds immediately with 1 byte.
/// </summary>
/// <param name="RequestType">Type of status being requested (1-4).</param>
public sealed record StatusRequest(StatusRequestType RequestType) : Element;

/// <summary>
/// Status response sent from printer to client (1 byte).
/// Generated in response to StatusRequest command.
/// </summary>
/// <param name="StatusByte">Single byte containing printer status flags.</param>
/// <param name="IsPaperOut">Paper end detected (bit 5).</param>
/// <param name="IsCoverOpen">Cover is open (bit 2).</param>
/// <param name="IsOffline">Printer is offline/error (bit 6).</param>
public sealed record StatusResponse(
    byte StatusByte,
    bool IsPaperOut,
    bool IsCoverOpen,
    bool IsOffline) : Element;
