using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.Epl.CommandDescriptors;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Builds the EPL command trie once and keeps it immutable for reuse.
/// </summary>
public sealed class EplCommandTrieProvider : CommandTrieProvider<EplParserState, ICommandDescriptor<EplParserState>>
{
    protected override IEnumerable<ICommandDescriptor<EplParserState>> AllDescriptors =>
    [
        // Text commands
        new EplA2TextDescriptor(),

        // Drawing commands
        new EplLODrawLineDescriptor(),
        new EplBBarcodeDescriptor(),
        new EplXDrawLineDescriptor(),

        // Graphics commands
        new EplGWGraphicWriteDescriptor(),

        // Print commands
        new EplPfPrintAndFeedDescriptor(),

        // Configuration commands
        new EplqWidthDescriptor(),
        new EplQHeightDescriptor(),
        new EplRSpeedDescriptor(),
        new EplSDarknessDescriptor(),
        new EplZDirectionDescriptor(),
        new EplIInternationalCharacterDescriptor(),
        new EpliCodePageDescriptor(),
        new EplNNAcknowledgeDescriptor(),
    ];
}
