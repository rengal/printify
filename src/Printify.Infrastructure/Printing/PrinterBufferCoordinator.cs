using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Maintains buffer state per printer and publishes throttled runtime updates.
/// </summary>
public sealed class PrinterBufferCoordinator : BackgroundService, IPrinterBufferCoordinator
{
    private const int MaxDrainEventsPerFullBuffer = 20;
    private static readonly TimeSpan MaxPublishInterval = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan DrainTickInterval = TimeSpan.FromMilliseconds(1000);

    private readonly object gate = new();
    private readonly Dictionary<Guid, PrinterBufferState> states = new();
    private readonly SemaphoreSlim activeSignal = new(0, 1);
    private readonly IPrinterRuntimeStatusStore runtimeStatusStore;
    private readonly IPrinterStatusStream statusStream;

    public PrinterBufferCoordinator(
        IPrinterRuntimeStatusStore runtimeStatusStore,
        IPrinterStatusStream statusStream)
    {
        this.runtimeStatusStore = runtimeStatusStore;
        this.statusStream = statusStream;
    }

    public PrinterBufferSnapshot GetSnapshot(Printer printer, PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var state = GetOrCreateState(printer, settings, now);
            ApplyDrain(state, now);
            UpdateActiveState(state);

            return new PrinterBufferSnapshot(
                state.BufferedBytes,
                state.IsBusy,
                state.IsFull,
                state.IsEmpty);
        }
    }

    public int GetAvailableBytes(Printer printer, PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        if (!IsBufferSimulationEnabled(settings))
        {
            return int.MaxValue;
        }

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var state = GetOrCreateState(printer, settings, now);
            ApplyDrain(state, now);
            UpdateActiveState(state);
            var maxCapacity = state.BufferMaxCapacity.GetValueOrDefault();
            return Math.Max(0, maxCapacity - state.BufferedBytes);
        }
    }

    public void AddBytes(Printer printer, PrinterSettings settings, int byteCount)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        if (byteCount <= 0)
        {
            return;
        }

        PrinterStatusUpdate? update = null;
        Guid workspaceId = Guid.Empty;

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var state = GetOrCreateState(printer, settings, now);
            ApplyDrain(state, now);

            if (!IsBufferSimulationEnabled(settings))
            {
                if (state.BufferedBytes == 0)
                {
                    return;
                }

                state.BufferedBytes = 0;
                state.DrainRemainder = 0;
            }
            else
            {
                state.BufferedBytes += byteCount;
            }

            UpdateFlags(state);
            UpdateActiveState(state);
            update = TryBuildUpdate(state, now, forcePublish: false);
            workspaceId = state.WorkspaceId;
        }

        if (update is not null)
        {
            statusStream.Publish(workspaceId, update);
        }
    }

    public void ForcePublish(Printer printer, PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        PrinterStatusUpdate? update = null;
        Guid workspaceId = Guid.Empty;

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var state = GetOrCreateState(printer, settings, now);
            ApplyDrain(state, now);
            UpdateActiveState(state);
            update = TryBuildUpdate(state, now, forcePublish: true);
            workspaceId = state.WorkspaceId;
        }

        if (update is not null)
        {
            statusStream.Publish(workspaceId, update);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Sleep until at least one printer becomes active to avoid idle ticks.
            await activeSignal.WaitAsync(stoppingToken).ConfigureAwait(false);

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            using var timer = new PeriodicTimer(DrainTickInterval);
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = DrainAndCollectUpdates();
                foreach (var pending in result.Updates)
                {
                    statusStream.Publish(pending.WorkspaceId, pending.Update);
                }

                if (result.ActiveCount == 0)
                {
                    break;
                }

                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private DrainResult DrainAndCollectUpdates()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = new List<PendingPublish>();
        var activeCount = 0;

        lock (gate)
        {
            foreach (var state in states.Values)
            {
                if (!state.IsActive)
                {
                    continue;
                }

                ApplyDrain(state, now);
                UpdateActiveState(state);
                if (state.IsActive)
                {
                    activeCount++;
                }

                var update = TryBuildUpdate(state, now, forcePublish: false);
                if (update is not null)
                {
                    pending.Add(new PendingPublish(state.WorkspaceId, update));
                }
            }
        }

        return new DrainResult(pending, activeCount);
    }

    private PrinterBufferState GetOrCreateState(
        Printer printer,
        PrinterSettings settings,
        DateTimeOffset now)
    {
        if (!states.TryGetValue(printer.Id, out var state))
        {
            state = new PrinterBufferState(printer.Id, printer.OwnerWorkspaceId, now);
            states.Add(printer.Id, state);
        }

        state.WorkspaceId = printer.OwnerWorkspaceId;
        UpdateSettings(state, settings);
        UpdateFlags(state);

        return state;
    }

    private static void UpdateSettings(PrinterBufferState state, PrinterSettings settings)
    {
        state.EmulateBufferCapacity = settings.EmulateBufferCapacity;
        state.BufferDrainRate = settings.BufferDrainRate;
        state.BufferMaxCapacity = settings.BufferMaxCapacity;
        state.MinDeltaBytes = CalculateMinDeltaBytes(settings.BufferMaxCapacity);
        state.BusyThresholdBytes = settings.BufferMaxCapacity.GetValueOrDefault() / 2;
    }

    private static int CalculateMinDeltaBytes(int? bufferMaxCapacity)
    {
        if (bufferMaxCapacity is null or <= 0)
        {
            return 0;
        }

        // Cap the number of publishes needed to drain a full buffer.
        return Math.Max(1, (bufferMaxCapacity.Value + MaxDrainEventsPerFullBuffer - 1) / MaxDrainEventsPerFullBuffer);
    }

    private static bool IsBufferSimulationEnabled(PrinterSettings settings)
    {
        return settings is { EmulateBufferCapacity: true, BufferDrainRate: > 0, BufferMaxCapacity: > 0 };
    }

    private void ApplyDrain(PrinterBufferState state, DateTimeOffset now)
    {
        if (!IsBufferSimulationEnabled(state))
        {
            state.BufferedBytes = 0;
            state.DrainRemainder = 0;
            state.LastUpdatedAt = now;
            UpdateFlags(state);
            return;
        }

        var elapsed = now - state.LastUpdatedAt;
        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        // TODO: rename BufferDrainRate to BufferDrainRateInBytesPerSec.
        state.DrainRemainder += state.BufferDrainRate.GetValueOrDefault() * (decimal)elapsed.TotalSeconds;

        // Keep fractional drain to avoid dropping small rates across ticks.
        var drainedWhole = (int)state.DrainRemainder;
        if (drainedWhole > 0)
        {
            state.BufferedBytes = Math.Max(0, state.BufferedBytes - drainedWhole);
            state.DrainRemainder -= drainedWhole;
        }

        state.LastUpdatedAt = now;
        UpdateFlags(state);
    }

    private static bool IsBufferSimulationEnabled(PrinterBufferState state)
    {
        return state is { EmulateBufferCapacity: true, BufferDrainRate: > 0, BufferMaxCapacity: > 0 };
    }

    private void UpdateActiveState(PrinterBufferState state)
    {
        var wasActive = state.IsActive;
        // Only tick printers with pending buffer bytes.
        state.IsActive = IsBufferSimulationEnabled(state) && state.BufferedBytes > 0;

        if (!wasActive && state.IsActive)
        {
            SignalActive();
        }
    }

    private void SignalActive()
    {
        try
        {
            activeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Signal already pending.
        }
    }

    private void UpdateFlags(PrinterBufferState state)
    {
        state.IsEmpty = state.BufferedBytes == 0;
        state.IsFull = state.BufferMaxCapacity is > 0 && state.BufferedBytes >= state.BufferMaxCapacity.Value;
        state.IsBusy = state.BufferDrainRate is > 0
            && state.BusyThresholdBytes > 0
            && state.BufferedBytes >= state.BusyThresholdBytes;
    }

    private PrinterStatusUpdate? TryBuildUpdate(
        PrinterBufferState state,
        DateTimeOffset now,
        bool forcePublish)
    {
        var bufferedBytes = state.BufferedBytes;
        var delta = Math.Abs(bufferedBytes - state.LastPublishedBytes);
        var elapsed = now - state.LastPublishedAt;

        var crossedEmpty = state.IsEmpty != state.LastPublishedIsEmpty;
        var crossedFull = state.IsFull != state.LastPublishedIsFull;
        var crossedBusy = state.IsBusy != state.LastPublishedIsBusy;
        var crossedThreshold = crossedEmpty || crossedFull || crossedBusy;
        var reachedDelta = state.MinDeltaBytes > 0 && delta >= state.MinDeltaBytes;
        var reachedInterval = elapsed >= MaxPublishInterval;

        // Publish on threshold crossings, forced flushes, or when delta/interval limits are hit.
        var shouldPublish = bufferedBytes != state.LastPublishedBytes
            && (crossedThreshold || forcePublish || reachedDelta || reachedInterval);

        if (!shouldPublish)
        {
            return null;
        }

        state.LastPublishedBytes = bufferedBytes;
        state.LastPublishedAt = now;
        state.LastPublishedIsEmpty = state.IsEmpty;
        state.LastPublishedIsFull = state.IsFull;
        state.LastPublishedIsBusy = state.IsBusy;

        var runtimeUpdate = new PrinterRuntimeStatusUpdate(
            state.PrinterId,
            now,
            BufferedBytes: bufferedBytes);
        runtimeStatusStore.Update(runtimeUpdate);

        return new PrinterStatusUpdate(
            state.PrinterId,
            now,
            RuntimeUpdate: runtimeUpdate);
    }

    private sealed class PrinterBufferState
    {
        public PrinterBufferState(Guid printerId, Guid workspaceId, DateTimeOffset now)
        {
            PrinterId = printerId;
            WorkspaceId = workspaceId;
            LastUpdatedAt = now;
            LastPublishedAt = DateTimeOffset.MinValue;
            LastPublishedBytes = 0;
            LastPublishedIsEmpty = true;
        }

        public Guid PrinterId { get; }
        public Guid WorkspaceId { get; set; }
        public DateTimeOffset LastUpdatedAt { get; set; }
        public DateTimeOffset LastPublishedAt { get; set; }
        public int BufferedBytes { get; set; }
        public decimal DrainRemainder { get; set; }
        public bool EmulateBufferCapacity { get; set; }
        public decimal? BufferDrainRate { get; set; }
        public int? BufferMaxCapacity { get; set; }
        public int MinDeltaBytes { get; set; }
        public int BusyThresholdBytes { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmpty { get; set; }
        public bool IsFull { get; set; }
        public bool IsBusy { get; set; }
        public int LastPublishedBytes { get; set; }
        public bool LastPublishedIsEmpty { get; set; }
        public bool LastPublishedIsFull { get; set; }
        public bool LastPublishedIsBusy { get; set; }
    }

    private sealed record PendingPublish(Guid WorkspaceId, PrinterStatusUpdate Update);

    private sealed record DrainResult(IReadOnlyList<PendingPublish> Updates, int ActiveCount);
}
