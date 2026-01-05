using Printify.Application.Printing;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Mapping;

internal static class PrinterMapper
{
    internal static PrinterState MapListenerState(PrinterListenerStatus status)
    {
        return status switch
        {
            PrinterListenerStatus.OpeningPort => PrinterState.Starting,
            PrinterListenerStatus.Listening => PrinterState.Started,
            PrinterListenerStatus.Idle => PrinterState.Stopped,
            PrinterListenerStatus.Failed => PrinterState.Error,
            _ => throw new InvalidOperationException("unknown runtime status")
        };
    }

    internal static PrinterResponseDto ToResponseDto(
        this Printer printer,
        PrinterSettings settings,
        PrinterOperationalFlags? operationalFlags,
        PrinterRuntimeStatus? runtimeStatus)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return new PrinterResponseDto(
            ToPrinterDto(printer),
            ToSettingsDto(settings),
            ToOperationalFlagsDto(operationalFlags),
            ToRuntimeStatusDto(runtimeStatus));
    }

    internal static PrinterResponseDto ToResponseDto(this PrinterDetailsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Printer.ToResponseDto(snapshot.Settings, snapshot.OperationalFlags, snapshot.RuntimeStatus);
    }

    internal static PrinterDto ToPrinterDto(this Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        return new PrinterDto(
            printer.Id,
            printer.DisplayName,
            printer.IsPinned,
            printer.LastViewedDocumentId,
            printer.LastDocumentReceivedAt);
    }

    internal static PrinterSettingsDto ToSettingsDto(this PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new PrinterSettingsDto(
            DomainMapper.ToString(settings.Protocol),
            settings.WidthInDots,
            settings.HeightInDots,
            settings.ListenTcpPortNumber,
            settings.EmulateBufferCapacity,
            settings.BufferDrainRate,
            settings.BufferMaxCapacity);
    }

    internal static PrinterOperationalFlagsDto? ToOperationalFlagsDto(PrinterOperationalFlags? flags)
    {
        if (flags is null)
        {
            return null;
        }

        return new PrinterOperationalFlagsDto(
            flags.PrinterId,
            flags.TargetState.ToString(),
            flags.UpdatedAt,
            flags.IsCoverOpen,
            flags.IsPaperOut,
            flags.IsOffline,
            flags.HasError,
            flags.IsPaperNearEnd);
    }

    internal static PrinterRuntimeStatusDto? ToRuntimeStatusDto(PrinterRuntimeStatus? status)
    {
        if (status is null)
        {
            return null;
        }

        return new PrinterRuntimeStatusDto(
            status.PrinterId,
            status.State.ToString(),
            status.UpdatedAt,
            status.BufferedBytes,
            status.Drawer1State.ToString(),
            status.Drawer2State.ToString());
    }

    internal static PrinterRuntimeStatusUpdateDto? ToRuntimeStatusUpdateDto(
        PrinterRuntimeStatusUpdate? status)
    {
        if (status is null)
        {
            return null;
        }

        return new PrinterRuntimeStatusUpdateDto(
            status.State?.ToString(),
            status.UpdatedAt,
            status.BufferedBytes,
            status.Drawer1State?.ToString(),
            status.Drawer2State?.ToString());
    }

    internal static PrinterOperationalFlagsUpdateDto? ToOperationalFlagsUpdateDto(
        PrinterOperationalFlagsUpdate? update)
    {
        if (update is null)
        {
            return null;
        }

        return new PrinterOperationalFlagsUpdateDto(
            update.PrinterId,
            update.UpdatedAt,
            update.TargetState?.ToString(),
            update.IsCoverOpen,
            update.IsPaperOut,
            update.IsOffline,
            update.HasError,
            update.IsPaperNearEnd);
    }

    internal static PrinterSidebarSnapshotDto ToSidebarSnapshotDto(
        this PrinterSidebarSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new PrinterSidebarSnapshotDto(
            ToPrinterDto(snapshot.Printer),
            ToStateOnlyRuntimeStatusDto(snapshot.RuntimeStatus));
    }

    internal static PrinterStatusUpdateDto ToStatusUpdateDto(this PrinterStatusUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        return new PrinterStatusUpdateDto(
            update.PrinterId,
            update.UpdatedAt,
            ToRuntimeStatusUpdateDto(update.RuntimeUpdate),
            ToOperationalFlagsUpdateDto(update.OperationalFlagsUpdate),
            update.Settings is null ? null : ToSettingsDto(update.Settings),
            update.Printer is null ? null : ToPrinterDto(update.Printer));
    }

    private static PrinterRuntimeStatusDto? ToStateOnlyRuntimeStatusDto(PrinterRuntimeStatus? status)
    {
        if (status is null)
        {
            return null;
        }

        return new PrinterRuntimeStatusDto(
            status.PrinterId,
            status.State.ToString(),
            status.UpdatedAt,
            null,
            null,
            null);
    }
}
