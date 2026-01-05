using System;
using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Pin;

public sealed record SetPrinterPinnedCommand(RequestContext Context, Guid PrinterId, bool IsPinned)
    : IRequest<PrinterDetailsSnapshot>;
