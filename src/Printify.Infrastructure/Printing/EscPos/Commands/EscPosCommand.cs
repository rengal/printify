namespace Printify.Infrastructure.Printing.EscPos.Commands;

using Printify.Domain.Printing;

/// <summary>
/// Base type for all ESC/POS protocol commands.
/// </summary>
public abstract record EscPosCommand : Command;
