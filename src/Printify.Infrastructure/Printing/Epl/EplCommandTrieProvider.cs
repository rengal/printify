using Printify.Application.Interfaces;
using Printify.Infrastructure.Media;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.Epl.CommandDescriptors;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Builds the EPL command trie once and keeps it immutable for reuse.
/// </summary>
public sealed class EplCommandTrieProvider : CommandTrieProvider<ICommandDescriptor>
{
    private static readonly IMediaService MediaServiceInstance = new MediaService();
    private static readonly IEplBarcodeService BarcodeServiceInstance = new MediaService();

    protected override IEnumerable<ICommandDescriptor> AllDescriptors =>
    [
        // Text commands
        new ScalableTextDescriptor(),

        // Drawing commands
        new DrawHorizontalLineDescriptor(),
        new PrintBarcodeDescriptor(BarcodeServiceInstance),
        new DrawBoxDescriptor(),

        // Graphics commands
        new PrintGraphicDescriptor(MediaServiceInstance),

        // Print commands
        new PrintDescriptor(),

        // Configuration commands
        new SetLabelWidthDescriptor(),
        new SetLabelHeightDescriptor(),
        new SetPrintSpeedDescriptor(),
        new SetPrintDarknessDescriptor(),
        new SetPrintDirectionDescriptor(),
        new SetInternationalCharacterDescriptor(),
        new ClearBufferDescriptor(),

        // Control characters (no-op commands)
        new CarriageReturnDescriptor(),
        new LineFeedDescriptor(),
    ];
}
