namespace Printify.Application.Exceptions;

public sealed class PrinterListenerStartFailedException(string message) : Exception(message);
