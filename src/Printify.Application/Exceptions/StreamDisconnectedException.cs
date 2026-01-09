namespace Printify.Application.Exceptions;

public sealed class StreamDisconnectedException : Exception
{
    public StreamDisconnectedException(string? message) : base(message) { }
}
