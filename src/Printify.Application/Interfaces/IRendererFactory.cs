using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

/// <summary>
/// Factory for obtaining protocol-specific renderers.
/// </summary>
public interface IRendererFactory
{
    /// <summary>
    /// Gets the appropriate renderer for the specified protocol.
    /// </summary>
    /// <param name="protocol">The printer protocol.</param>
    /// <returns>A renderer for the specified protocol.</returns>
    /// <exception cref="NotSupportedException">Thrown when the protocol is not supported.</exception>
    IRenderer GetRenderer(Protocol protocol);
}
