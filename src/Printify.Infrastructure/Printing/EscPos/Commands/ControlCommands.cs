namespace Printify.Infrastructure.Printing.EscPos.Commands;

/// <summary>
/// Supported text alignment values for ESC a (justification) commands.
/// </summary>
public enum TextJustification
{
    /// <summary>
    /// Align text to the left margin.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Center text relative to the printable width.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Align text to the right margin.
    /// </summary>
    Right = 2
}

/// <summary>
/// Paper cut operation modes supported by ESC/POS cut commands.
/// </summary>
public enum PagecutMode
{
    /// <summary>
    /// Full paper cut (completely severs the paper).
    /// </summary>
    Full = 0,

    /// <summary>
    /// Partial paper cut (leaves a small connection for easy tear-off).
    /// </summary>
    Partial = 1,

    /// <summary>
    /// Partial cut leaving one point uncut.
    /// </summary>
    PartialOnePoint = 2,

    /// <summary>
    /// Partial cut leaving three points uncut.
    /// </summary>
    PartialThreePoint = 3,
}

/// <summary>
/// Type of real-time status request (DLE EOT n parameter).
/// </summary>
public enum StatusRequestType : byte
{
    /// <summary>DLE EOT 1 - Printer status</summary>
    PrinterStatus = 0x01,

    /// <summary>DLE EOT 2 - Offline cause status</summary>
    OfflineCause = 0x02,

    /// <summary>DLE EOT 3 - Error cause status</summary>
    ErrorCause = 0x03,

    /// <summary>DLE EOT 4 - Paper roll sensor status</summary>
    PaperRollSensor = 0x04
}

/// <summary>
/// An audible/attention bell signal.
/// </summary>
public sealed record Bell : EscPosCommand;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
/// <param name="Mode">The type of cut to perform (full or partial).</param>
/// <param name="FeedMotionUnits">Feed distance in motion units before cutting (GS V m n).</param>
public sealed record CutPaper(PagecutMode Mode, int? FeedMotionUnits = null) : EscPosCommand;

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Pin">Target drawer pin.</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record Pulse(int Pin, int OnTimeMs, int OffTimeMs)
    : EscPosCommand;

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
public sealed record Initialize : EscPosCommand;

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
    : EscPosCommand;

/// <summary>
/// DLE EOT n - Real-time status transmission request.
/// Client requests printer status; printer responds immediately with 1 byte.
/// </summary>
/// <param name="RequestType">Type of status being requested (1-4).</param>
public sealed record StatusRequest(StatusRequestType RequestType) : EscPosCommand;

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
    bool IsOffline) : EscPosCommand;
