using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record StreamPrinterRuntimeQuery(
    RequestContext Context,
    Guid PrinterId)
    : IRequest;

public sealed record PrinterRuntimeStreamResult(
    string EventName,
    IAsyncEnumerable<PrinterStatusUpdate> Updates) : IResponse;

