using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed record StreamPrinterSidebarQuery(RequestContext Context)
    : IRequest;

public sealed record PrinterSidebarStreamResult(
    string EventName,
    IAsyncEnumerable<PrinterSidebarSnapshot> Updates) : IResponse;

