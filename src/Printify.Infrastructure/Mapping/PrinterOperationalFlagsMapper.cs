using Printify.Domain.Printers;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Bidirectional mapper between PrinterOperationalFlags domain and persistence entities.
/// </summary>
internal static class PrinterOperationalFlagsMapper
{
    internal static PrinterOperationalFlags ToDomain(this PrinterOperationalFlagsEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PrinterOperationalFlags(
            entity.PrinterId,
            ParseTargetState(entity.TargetState),
            entity.UpdatedAt,
            entity.IsCoverOpen,
            entity.IsPaperOut,
            entity.IsOffline,
            entity.HasError,
            entity.IsPaperNearEnd);
    }

    internal static void MapToEntity(this PrinterOperationalFlags flags, PrinterOperationalFlagsEntity entity)
    {
        ArgumentNullException.ThrowIfNull(flags);
        ArgumentNullException.ThrowIfNull(entity);

        entity.PrinterId = flags.PrinterId;
        entity.TargetState = flags.TargetState.ToString();
        entity.UpdatedAt = flags.UpdatedAt;
        entity.IsCoverOpen = flags.IsCoverOpen;
        entity.IsPaperOut = flags.IsPaperOut;
        entity.IsOffline = flags.IsOffline;
        entity.HasError = flags.HasError;
        entity.IsPaperNearEnd = flags.IsPaperNearEnd;
    }

    internal static void MapToEntity(this PrinterOperationalFlagsUpdate update, PrinterOperationalFlagsEntity entity)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(entity);

        if (update.TargetState is not null)
        {
            entity.TargetState = update.TargetState.Value.ToString();
        }

        entity.UpdatedAt = update.UpdatedAt;

        if (update.IsCoverOpen is not null)
        {
            entity.IsCoverOpen = update.IsCoverOpen.Value;
        }

        if (update.IsPaperOut is not null)
        {
            entity.IsPaperOut = update.IsPaperOut.Value;
        }

        if (update.IsOffline is not null)
        {
            entity.IsOffline = update.IsOffline.Value;
        }

        if (update.HasError is not null)
        {
            entity.HasError = update.HasError.Value;
        }

        if (update.IsPaperNearEnd is not null)
        {
            entity.IsPaperNearEnd = update.IsPaperNearEnd.Value;
        }
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
