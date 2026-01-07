using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed record ListPrinterSidebarQuery(RequestContext Context) : IRequest;

public sealed record PrinterSidebarListResponse(IReadOnlyList<PrinterSidebarSnapshot> Snapshots) : IResponse;
