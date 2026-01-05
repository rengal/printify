using MediatR;
using Microsoft.Extensions.Logging;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class UpdatePrinterRealtimeStatusHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator,
    IPrinterStatusStream statusStream,
    ILogger<UpdatePrinterRealtimeStatusHandler> logger)
    : IRequestHandler<UpdatePrinterRealtimeStatusCommand, PrinterRealtimeStatus>
{
    public async Task<PrinterRealtimeStatus> Handle(
        UpdatePrinterRealtimeStatusCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Reject empty updates.
        if (!HasUpdates(request))
        {
            throw new ArgumentException("At least one realtime status field must be provided.", nameof(request));
        }

        // Enforce workspace ownership before mutating printer state.
        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (printer is null)
        {
            logger.LogWarning(
                "Printer {PrinterId} not found for workspace {WorkspaceId} when updating realtime status",
                request.PrinterId,
                request.Context.WorkspaceId);
            throw new PrinterNotFoundException(request.PrinterId);
        }

        // Parse drawer states early to fail fast on invalid user input.
        var drawer1State = ParseDrawerState(request.Drawer1State);
        var drawer2State = ParseDrawerState(request.Drawer2State);

        var existingStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, cancellationToken);
        // Default to Started to preserve the legacy target-state behavior for missing records.
        var currentTargetState = existingStatus?.TargetState ?? PrinterTargetState.Started;
        PrinterTargetState? newTargetState = null;
        var targetStateChanged = false;
        if (!string.IsNullOrWhiteSpace(request.TargetStatus))
        {
            // Translate API string into domain target state and track whether it actually changes.
            var targetState = DomainMapper.ParsePrinterTargetState(request.TargetStatus);
            newTargetState = targetState;
            targetStateChanged = currentTargetState != targetState;
            if (targetStateChanged)
            {
                logger.LogInformation(
                    "Updating printer {PrinterId} target state to {TargetState} for workspace {WorkspaceId}",
                    request.PrinterId,
                    targetState,
                    request.Context.WorkspaceId);
            }

            if (targetState == PrinterTargetState.Started)
            {
                logger.LogInformation(
                    "Starting listener for printer {PrinterId} in workspace {WorkspaceId}",
                    request.PrinterId,
                    request.Context.WorkspaceId);
                await listenerOrchestrator.AddListenerAsync(printer, targetState, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation(
                    "Stopping listener for printer {PrinterId} in workspace {WorkspaceId}",
                    request.PrinterId,
                    request.Context.WorkspaceId);
                await listenerOrchestrator.RemoveListenerAsync(printer, targetState, cancellationToken).ConfigureAwait(false);
            }
        }

        // Listener status is runtime-only, while the rest is persisted realtime state.
        var runtime = listenerOrchestrator.GetStatus(printer);
        var state = MapListenerState(runtime.Status);
        var updatedAt = DateTimeOffset.UtcNow;
        // Ensure snapshots keep a concrete target state even when the request does not update it.
        var effectiveTargetState = newTargetState ?? currentTargetState;
        // Create a baseline snapshot when no prior status exists in storage.
        var baseStatus = existingStatus ?? new PrinterRealtimeStatus(
            printer.Id,
            effectiveTargetState,
            state,
            updatedAt);

        // Build a partial update from the request so persistence updates only the provided fields.
        var update = new PrinterRealtimeStatusUpdate(
            printer.Id,
            updatedAt,
            TargetState: targetStateChanged || existingStatus is null ? effectiveTargetState : null,
            State: state,
            IsCoverOpen: request.IsCoverOpen,
            IsPaperOut: request.IsPaperOut,
            IsOffline: request.IsOffline,
            HasError: request.HasError,
            IsPaperNearEnd: request.IsPaperNearEnd,
            Drawer1State: drawer1State,
            Drawer2State: drawer2State);

        // Merge incoming fields with existing state to preserve untouched values.
        var updatedStatus = update.ApplyTo(baseStatus);

        // Skip persistence and publish when nothing actually changed.
        if (!HasRealtimeChanges(baseStatus, updatedStatus) && !targetStateChanged)
        {
            return baseStatus;
        }

        // Persist and broadcast the new snapshot so subscribers stay consistent.
        await printerRepository.UpsertRealtimeStatusAsync(update, cancellationToken).ConfigureAwait(false);
        statusStream.Publish(printer.OwnerWorkspaceId, updatedStatus);

        return updatedStatus;
    }

    private static bool HasUpdates(UpdatePrinterRealtimeStatusCommand request)
    {
        return !string.IsNullOrWhiteSpace(request.TargetStatus)
               || request.IsCoverOpen.HasValue
               || request.IsPaperOut.HasValue
               || request.IsOffline.HasValue
               || request.HasError.HasValue
               || request.IsPaperNearEnd.HasValue
               || request.Drawer1State is not null
               || request.Drawer2State is not null;
    }

    private static DrawerState? ParseDrawerState(string? value) //todo debugnow to DomainMapper
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<DrawerState>(value, true, out var parsed))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Unsupported drawer state '{value}'.");
        }

        if (parsed == DrawerState.OpenedByCommand)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Drawer state 'OpenedByCommand' cannot be set via API.");
        }

        return parsed;
    }

    private static bool HasRealtimeChanges(PrinterRealtimeStatus current, PrinterRealtimeStatus updated)
    {
        return current.IsCoverOpen != updated.IsCoverOpen
               || current.IsPaperOut != updated.IsPaperOut
               || current.IsOffline != updated.IsOffline
               || current.HasError != updated.HasError
               || current.IsPaperNearEnd != updated.IsPaperNearEnd
               || current.Drawer1State != updated.Drawer1State
               || current.Drawer2State != updated.Drawer2State;
    }

    private static PrinterState MapListenerState(PrinterListenerStatus status) //todo debugnow move to DomainMapper
    {
        return status switch
        {
            PrinterListenerStatus.OpeningPort => PrinterState.Starting,
            PrinterListenerStatus.Listening => PrinterState.Started,
            PrinterListenerStatus.Idle => PrinterState.Stopped,
            PrinterListenerStatus.Failed => PrinterState.Error,
            _ => throw new InvalidOperationException("Unknown listener status")
        };
    }
}
