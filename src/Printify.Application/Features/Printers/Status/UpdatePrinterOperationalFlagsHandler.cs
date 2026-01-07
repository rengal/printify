using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Microsoft.Extensions.Logging;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class UpdatePrinterOperationalFlagsHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator,
    IPrinterStatusStream statusStream,
    ILogger<UpdatePrinterOperationalFlagsHandler> logger)
    : IRequestHandler<UpdatePrinterOperationalFlagsCommand, PrinterOperationalFlags>
{
    public async Task<PrinterOperationalFlags> Handle(
        IReceiveContext<UpdatePrinterOperationalFlagsCommand> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        if (!HasUpdates(request))
        {
            throw new BadRequestException("At least one operational flag must be provided.");
        }

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (printer is null)
        {
            logger.LogWarning(
                "Printer {PrinterId} not found for workspace {WorkspaceId} when updating operational flags",
                request.PrinterId,
                request.Context.WorkspaceId);
            throw new PrinterNotFoundException(request.PrinterId);
        }

        var existing = await printerRepository.GetOperationalFlagsAsync(printer.Id, cancellationToken)
            .ConfigureAwait(false);
        var effectiveTargetState = existing?.TargetState ?? PrinterTargetState.Started;
        if (!string.IsNullOrWhiteSpace(request.TargetState))
        {
            var desiredTargetState = Domain.Mapping.DomainMapper.ParsePrinterTargetState(request.TargetState);
            if (desiredTargetState != effectiveTargetState)
            {
                if (desiredTargetState == PrinterTargetState.Started)
                {
                    var settings = await printerRepository.GetSettingsAsync(printer.Id, cancellationToken)
                        .ConfigureAwait(false);
                    // Settings are persisted separately; missing settings indicate a data integrity issue.
                    if (settings is null)
                    {
                        throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
                    }

                    try
                    {
                        await listenerOrchestrator.AddListenerAsync(printer, settings, desiredTargetState, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Failed to start listener for printer {PrinterId} in workspace {WorkspaceId}",
                            request.PrinterId,
                            request.Context.WorkspaceId);
                        throw new PrinterListenerStartFailedException("Printer failed to start.");
                    }
                }
                else
                {
                    await listenerOrchestrator.RemoveListenerAsync(printer, desiredTargetState, cancellationToken)
                        .ConfigureAwait(false);
                }

                effectiveTargetState = desiredTargetState;
            }
        }
        var nextCoverOpen = request.IsCoverOpen ?? existing?.IsCoverOpen ?? false;
        var nextPaperOut = request.IsPaperOut ?? existing?.IsPaperOut ?? false;
        var nextOffline = request.IsOffline ?? existing?.IsOffline ?? false;
        var nextHasError = request.HasError ?? existing?.HasError ?? false;
        var nextPaperNearEnd = request.IsPaperNearEnd ?? existing?.IsPaperNearEnd ?? false;
        if (existing is not null
            && existing.TargetState == effectiveTargetState
            && existing.IsCoverOpen == nextCoverOpen
            && existing.IsPaperOut == nextPaperOut
            && existing.IsOffline == nextOffline
            && existing.HasError == nextHasError
            && existing.IsPaperNearEnd == nextPaperNearEnd)
        {
            return existing;
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var next = new PrinterOperationalFlags(
            printer.Id,
            effectiveTargetState,
            updatedAt,
            nextCoverOpen,
            nextPaperOut,
            nextOffline,
            nextHasError,
            nextPaperNearEnd);

        var update = new PrinterOperationalFlagsUpdate(
            printer.Id,
            updatedAt,
            TargetState: effectiveTargetState == (existing?.TargetState ?? PrinterTargetState.Started)
                ? null
                : effectiveTargetState,
            IsCoverOpen: request.IsCoverOpen,
            IsPaperOut: request.IsPaperOut,
            IsOffline: request.IsOffline,
            HasError: request.HasError,
            IsPaperNearEnd: request.IsPaperNearEnd);
        await printerRepository.UpsertOperationalFlagsAsync(update, cancellationToken).ConfigureAwait(false);

        // Publish operational flag deltas for active-printer SSE without forcing full snapshots.
        statusStream.Publish(
            printer.OwnerWorkspaceId,
            new PrinterStatusUpdate(
                printer.Id,
                updatedAt,
                OperationalFlagsUpdate: update));

        return next;
    }

    private static bool HasUpdates(UpdatePrinterOperationalFlagsCommand request)
    {
        return request.IsCoverOpen.HasValue
               || request.IsPaperOut.HasValue
               || request.IsOffline.HasValue
               || request.HasError.HasValue
               || request.IsPaperNearEnd.HasValue
               || !string.IsNullOrWhiteSpace(request.TargetState);
    }
}

