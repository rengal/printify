namespace Printify.Application.Features.Printers.List;

public sealed record ListPrinterItemDto(
    long Id,
    string DisplayName,
    bool IsPinned,
    DateTimeOffset LastDocumentAt

);
