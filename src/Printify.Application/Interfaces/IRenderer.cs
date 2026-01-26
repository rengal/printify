using Printify.Domain.Documents;
using Printify.Domain.Layout;

namespace Printify.Application.Interfaces;

/// <summary>
/// Renders protocol commands into visual canvases.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders a document with protocol commands into one or more canvases.
    /// Multiple canvases are created for page breaks (ESC/POS) or print commands (EPL).
    /// </summary>
    /// <param name="document">The document containing protocol commands.</param>
    /// <returns>Array of canvases with rendered visual primitives.</returns>
    Canvas[] Render(Document document);
}
