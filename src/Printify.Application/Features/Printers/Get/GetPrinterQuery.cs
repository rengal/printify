using System;
using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Get;

public sealed record GetPrinterQuery(Guid PrinterId, RequestContext Context) : IRequest;

