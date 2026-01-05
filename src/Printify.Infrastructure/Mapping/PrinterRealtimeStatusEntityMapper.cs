using Printify.Domain.Printers;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Mapping;

internal static class PrinterRealtimeStatusEntityMapper
{
    internal static PrinterRealtimeStatus ToDomain(this PrinterRealtimeStatusEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PrinterRealtimeStatus(
            entity.PrinterId,
            ParseTargetState(entity.TargetState),
            // State is runtime-only and populated by listener services.
            PrinterState.Stopped,
            entity.UpdatedAt,
            entity.BufferedBytes,
            entity.IsCoverOpen,
            entity.IsPaperOut,
            entity.IsOffline,
            entity.HasError,
            entity.IsPaperNearEnd,
            ParseDrawerState(entity.Drawer1State),
            ParseDrawerState(entity.Drawer2State));
    }

    internal static void MapToEntity(this PrinterRealtimeStatus status, PrinterRealtimeStatusEntity entity)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(entity);

        entity.PrinterId = status.PrinterId;
        entity.TargetState = status.TargetState.ToString();
        entity.UpdatedAt = status.UpdatedAt;
        entity.BufferedBytes = status.BufferedBytes ?? 0;
        entity.IsCoverOpen = status.IsCoverOpen ?? false;
        entity.IsPaperOut = status.IsPaperOut ?? false;
        entity.IsOffline = status.IsOffline ?? false;
        entity.HasError = status.HasError ?? false;
        entity.IsPaperNearEnd = status.IsPaperNearEnd ?? false;
        entity.Drawer1State = (status.Drawer1State ?? DrawerState.Closed).ToString();
        entity.Drawer2State = (status.Drawer2State ?? DrawerState.Closed).ToString();
    }

    private static DrawerState ParseDrawerState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DrawerState.Closed;
        }

        return Enum.TryParse<DrawerState>(value, true, out var parsed)
            ? parsed
            : DrawerState.Closed;
    }

    private static PrinterTargetState ParseTargetState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PrinterTargetState.Stopped;
        }

        return Enum.TryParse<PrinterTargetState>(value, true, out var parsed)
            ? parsed
            : PrinterTargetState.Stopped;
    }
}
