using System;

namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload used to update printer configuration.
/// </summary>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="TcpListenPort">Optional TCP listener override for the printer.</param>
public sealed record UpdatePrinterRequestDto(
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    int? TcpListenPort);
