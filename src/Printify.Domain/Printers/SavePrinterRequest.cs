namespace Printify.Domain.Printers;

/// <summary>
/// Payload required to register a printer.
/// </summary>
/// <param name="OwnerUserId">Optional identifier of the owning user.</param>
/// <param name="OwnerSessionId">Identifier of the session that currently owns the printer.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="CreatedFromIp">IP address captured at registration time.</param>
public sealed record SavePrinterRequest(
    long? OwnerUserId,
    long OwnerSessionId,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    string CreatedFromIp);
