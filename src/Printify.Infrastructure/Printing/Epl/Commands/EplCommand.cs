namespace Printify.Infrastructure.Printing.Epl.Commands;

using Printify.Domain.Printing;

/// <summary>
/// Base type for all EPL protocol commands.
/// </summary>
public abstract record EplCommand : Command;
