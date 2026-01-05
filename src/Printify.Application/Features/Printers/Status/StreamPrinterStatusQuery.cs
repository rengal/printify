using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record StreamPrinterStatusQuery(
    RequestContext Context,
    Guid? PrinterId,
    PrinterRealtimeScope Scope)
    : IRequest<PrinterStatusStreamResult>;

public sealed record PrinterStatusStreamResult(
    string EventName,
    IAsyncEnumerable<PrinterRealtimeStatusUpdate> Updates);
