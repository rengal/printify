using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.List;

public sealed record ListPrintersQuery(RequestContext Context) : IRequest;

public sealed record PrinterListResponse(IReadOnlyList<PrinterDetailsSnapshot> Printers) : IResponse;
