using Printify.Domain.Printers;
using Printify.Domain.Users;

namespace Printify.Domain.Services;

/// <summary>
/// Command-side service responsible for persisting domain resources.
/// </summary>
public interface IResourceCommandService
{
    ValueTask<long> CreateDocumentAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default);

    ValueTask<long> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken = default);

    ValueTask<bool> UpdateUserAsync(long id, SaveUserRequest request, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteUserAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<long> CreatePrinterAsync(SavePrinterRequest request, CancellationToken cancellationToken = default);

    ValueTask<bool> UpdatePrinterAsync(long id, SavePrinterRequest request, CancellationToken cancellationToken = default);

    ValueTask<bool> DeletePrinterAsync(long id, CancellationToken cancellationToken = default);
}