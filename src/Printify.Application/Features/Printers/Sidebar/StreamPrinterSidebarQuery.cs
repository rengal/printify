using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed record StreamPrinterSidebarQuery(RequestContext Context)
    : IRequest<PrinterSidebarStreamResult>;

public sealed record PrinterSidebarStreamResult(
    string EventName,
    IAsyncEnumerable<PrinterSidebarSnapshot> Updates);
