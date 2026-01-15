using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.Epl.CommandDescriptors;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Builds the EPL command trie once and keeps it immutable for reuse.
/// </summary>
public sealed class EplCommandTrieProvider : CommandTrieProvider<ICommandDescriptor>
{
    protected override IEnumerable<ICommandDescriptor> AllDescriptors =>
    [
        // Text commands
        new ScalableTextDescriptor(),

        // Drawing commands
        new DrawHorizontalLineDescriptor(),
        new PrintBarcodeDescriptor(),
        new DrawLineDescriptor(),

        // Graphics commands
        new PrintGraphicDescriptor(),

        // Print commands
        new PrintDescriptor(),

        // Configuration commands
        new SetLabelWidthDescriptor(),
        new SetLabelHeightDescriptor(),
        new SetPrintSpeedDescriptor(),
        new SetPrintDarknessDescriptor(),
        new SetPrintDirectionDescriptor(),
        new SetInternationalCharacterDescriptor(),
        new SetCodePageDescriptor(),
        new ClearBufferDescriptor(),
    ];
}
