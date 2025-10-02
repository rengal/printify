using System.Threading;
using System.Threading.Tasks;
using Printify.Contracts.Documents;

namespace Printify.Contracts.Documents.Services;

/// <summary>
/// Command-side service responsible for persisting parsed documents and associated media payloads.
/// </summary>
public interface IResouceCommandService
{
    /// <summary>
    /// Persists a parsed document and returns the generated identifier.
    /// Implementations may transform mutable elements (for example, raster image content) before storage.
    /// </summary>
    ValueTask<long> CreateAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default);
}
