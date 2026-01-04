using Printify.Application.Interfaces;
using Printify.Infrastructure.Media;
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Builds the ESC/POS command trie once and keeps it immutable for reuse.
/// </summary>
public sealed class EscPosCommandTrieProvider : IEscPosCommandTrieProvider
{
    private static readonly IMediaService RasterMediaService = new MediaService();

    private static readonly IEnumerable<ICommandDescriptor> AllDescriptors =
    [
        new BarcodePrintDescriptor(),
        new BarcodeSetHeightDescriptor(),
        new BarcodeSetLabelPositionDescriptor(),
        new BarcodeSetModuleWidthDescriptor(),
        new BelDescriptor(),
        new GetPrinterStatusDescriptor(),
        new FlushLineBufferAndFeedDescriptor(),
        new LegacyCarriageReturnDescriptor(),
        new PageCutDescriptor(),
        new PartialCutOnePointDescriptor(),
        new PartialCutThreePointDescriptor(),
        new PrintStoredLogoDescriptor(),
        new PulseDescriptor(),
        new QrCodeDescriptor(),
        new RasterBitImagePrintDescriptor(RasterMediaService),
        new ResetLineSpacingDescriptor(),
        new ResetPrinterDescriptor(),
        new SetBoldModeDescriptor(),
        new SetChineseCodePageDescriptor(),
        new SetCodePageDescriptor(),
        new SetFontDescriptor(),
        new SetJustificationDescriptor(),
        new SetLineSpacingDescriptor(),
        new SetReverseModeDescriptor(),
        new SetUnderlineModeDescriptor()
    ];


    public EscPosCommandTrieNode Root { get; } = Build(AllDescriptors);

    private static EscPosCommandTrieNode Build(IEnumerable<ICommandDescriptor> descriptors)
    {
        var root = new MutableNode();

        foreach (var descriptor in descriptors)
        {
            if (descriptor.Prefix.IsEmpty)
                throw new InvalidOperationException("Prefix must not be empty");
            AddDescriptor(root, descriptor);
        }

        return Freeze(root);
    }

    private static void AddDescriptor(MutableNode root, ICommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var current = root;

        foreach (var value in descriptor.Prefix.Span)
        {
            if (!current.Children.TryGetValue(value, out var next))
            {
                next = new MutableNode();
                current.Children[value] = next;
            }

            current = next;
        }

        // Check for contradiction: node has children (not a leaf) but also has a descriptor
        // OR node already has a descriptor
        if (current.Descriptor != null)
        {
            throw new InvalidOperationException(
                $"Two descriptors have the same prefix: " +
                $"{current.Descriptor.GetType().Name} and {descriptor.GetType().Name}");
        }

        if (current.Children.Count > 0)
        {
            throw new InvalidOperationException(
                $"Descriptor {descriptor.GetType().Name} creates a contradiction: " +
                $"node at prefix {Convert.ToHexString(descriptor.Prefix.Span)} has children but also needs a descriptor. " +
                $"This means another descriptor has a longer prefix that extends this one.");
        }

        current.Descriptor = descriptor;
    }

    private static EscPosCommandTrieNode Freeze(MutableNode node)
    {
        var frozenChildren = new Dictionary<byte, EscPosCommandTrieNode>(node.Children.Count);
        var isLeaf = node.Children.Count == 0;

        // Validate trie invariant: leaf nodes must have a descriptor, non-leaf nodes must not
        if (isLeaf && node.Descriptor == null)
        {
            throw new InvalidOperationException(
                "Internal error: leaf node without descriptor found during trie construction");
        }

        if (!isLeaf && node.Descriptor != null)
        {
            throw new InvalidOperationException(
                $"Contradiction in trie: non-leaf node has descriptor {node.Descriptor.GetType().Name}. " +
                $"This means another command extends this command's prefix.");
        }

        foreach (var child in node.Children)
        {
            var frozenChild = Freeze(child.Value);
            frozenChildren[child.Key] = frozenChild;
        }

        return new EscPosCommandTrieNode(frozenChildren, node.Descriptor, isLeaf);
    }

    private sealed class MutableNode
    {
        public Dictionary<byte, MutableNode> Children { get; } = new();
        public ICommandDescriptor? Descriptor { get; set; }
    }
}
