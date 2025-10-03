using Printify.Contracts.Documents;
using Printify.Contracts.Printers;
using Printify.Contracts.Users;

namespace Printify.Contracts.Documents.Services;

/// <summary>
/// Command-side service responsible for persisting domain resources.
/// </summary>
public interface IResouceCommandService
{
    ValueTask<long> CreateDocumentAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default);

    ValueTask<long> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken = default);

    ValueTask<long> CreatePrinterAsync(SavePrinterRequest request, CancellationToken cancellationToken = default);
}
