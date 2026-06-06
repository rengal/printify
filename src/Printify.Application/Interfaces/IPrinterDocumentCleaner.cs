namespace Printify.Application.Interfaces;

/// <summary>
/// Deletes a printer's documents together with their now-orphaned media rows and files,
/// so clearing history or deleting a printer never leaves dangling data behind.
/// </summary>
public interface IPrinterDocumentCleaner
{
    Task DeleteByPrinterAsync(Guid printerId, CancellationToken cancellationToken);
}
