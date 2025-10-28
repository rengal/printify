namespace Printify.Application.Exceptions;

public sealed class PrinterNotFoundException(Guid printerId)
    : Exception($"Printer '{printerId}' could not be resolved.");
