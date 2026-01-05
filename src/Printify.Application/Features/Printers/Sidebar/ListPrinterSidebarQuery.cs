using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed record ListPrinterSidebarQuery(RequestContext Context)
    : IRequest<IReadOnlyList<PrinterSidebarSnapshot>>;
