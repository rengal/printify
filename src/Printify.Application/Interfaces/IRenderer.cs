using Printify.Domain.Documents;
using Printify.Domain.Layout;

namespace Printify.Application.Interfaces;

/// <summary>
/// Renders protocol commands into a visual canvas.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders a document with protocol commands into a canvas.
    /// </summary>
    /// <param name="document">The document containing protocol commands.</param>
    /// <returns>A canvas with rendered visual primitives.</returns>
    Canvas Render(Document document);
}
