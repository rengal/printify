using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Epl.Commands;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// EPL-specific barcode generation service.
/// Generates images from EPL barcode commands during parsing.
/// </summary>
public interface IEplBarcodeService
{
    /// <summary>
    /// Generates a barcode image using the supplied EPL barcode command parameters.
    /// </summary>
    /// <param name="type">EPL barcode type (e.g., "E30" for EAN-13).</param>
    /// <param name="data">Barcode data/content.</param>
    /// <param name="width">Module width (1-6, typically 2).</param>
    /// <param name="height">Barcode height in dots.</param>
    /// <param name="hri">Human readable interpretation flag.</param>
    /// <returns>MediaUpload containing the generated barcode image.</returns>
    Domain.Media.MediaUpload GenerateBarcodeMedia(
        string type,
        string data,
        int width,
        int height,
        char hri);
}
