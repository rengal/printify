using System;
using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Delete;

public sealed record DeletePrinterCommand(RequestContext Context, Guid PrinterId) : IRequest;

