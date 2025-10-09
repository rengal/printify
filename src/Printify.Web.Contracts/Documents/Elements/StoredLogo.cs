namespace Printify.Web.Contracts.Documents.Elements;

/// <summary>
/// Prints a logo stored in printer memory by its identifier.
/// Corresponds to ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogo(int Sequence, int LogoId) : PrintingElement(Sequence);

