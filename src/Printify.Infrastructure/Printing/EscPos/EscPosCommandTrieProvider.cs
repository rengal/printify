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
        var commandPrefixBytes = descriptors
            .Where(descriptor => !descriptor.Prefix.IsEmpty)
            .Select(descriptor => descriptor.Prefix.Span[0])
            .Distinct()
            .ToHashSet();

        AddDescriptor(root, new ErrorDescriptor(commandPrefixBytes));
        AddDescriptor(root, new AppendToLineBufferDescriptor());
        foreach (var descriptor in descriptors)
        {
            AddDescriptor(root, descriptor);
        }
        return Freeze(root);
    }

    private static void AddDescriptor(MutableNode root, ICommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Prefix.IsEmpty)
        {
            root.Descriptors.Add(descriptor);
            return;
        }

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

        // Check if this node already has a descriptor
        if (current.Descriptors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Two descriptors have the same prefix: " +
                $"{current.Descriptors[0].GetType().Name} and {descriptor.GetType().Name}");
        }

        current.Descriptors.Add(descriptor);
    }

    private static EscPosCommandTrieNode Freeze(MutableNode node)
    {
        var frozenChildren = new Dictionary<byte, EscPosCommandTrieNode>(node.Children.Count);
        var isLeaf = !node.Children.Any();

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
        public ICommandDescriptor? Descriptor { get; }
    }
}
