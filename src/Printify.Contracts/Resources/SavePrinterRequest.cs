namespace Printify.Contracts.Resources;

/// <summary>
/// Payload required to register a printer for a user.
/// </summary>
/// <param name="OwnerUserId">Identifier of the owning user.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="CreatedFromIp">IP address captured at registration time.</param>
public sealed record SavePrinterRequest(
    long OwnerUserId,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    string CreatedFromIp);
