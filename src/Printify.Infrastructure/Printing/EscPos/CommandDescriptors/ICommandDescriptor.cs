using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// ESC/POS-specific command descriptor interface.
/// </summary>
public interface ICommandDescriptor : global::Printify.Infrastructure.Printing.Common.ICommandDescriptor<ParserState>
{
}
