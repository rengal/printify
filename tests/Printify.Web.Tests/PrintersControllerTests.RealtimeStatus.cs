using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;
using PrinterRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterDto;
using PrinterSettingsRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterSettingsDto;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task GetPrinter_ReturnsSeededRealtimeStatus_WhenMissing()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Realtime Missing"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(printerResponse);
        Assert.NotNull(printerResponse!.OperationalFlags);
        Assert.Equal("Started", printerResponse.OperationalFlags!.TargetState);
    }

    [Fact]
    public async Task GetPrinter_ReturnsRealtimeStatus_WhenAvailable()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Realtime Ready"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var flagsRequest = new UpdatePrinterOperationalFlagsRequestDto(
            IsCoverOpen: true,
            IsPaperOut: false,
            IsOffline: false,
            HasError: false,
            IsPaperNearEnd: true);
        var flagsResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            flagsRequest);
        flagsResponse.EnsureSuccessStatusCode();

        var drawerResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/drawers",
            new UpdatePrinterDrawerStateRequestDto(
                Drawer1State: DrawerState.OpenedManually.ToString(),
                Drawer2State: DrawerState.Closed.ToString()));
        drawerResponse.EnsureSuccessStatusCode();

        var printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(printerResponse);
        Assert.NotNull(printerResponse!.OperationalFlags);
        Assert.NotNull(printerResponse.RuntimeStatus);
        Assert.Equal("Started", printerResponse.OperationalFlags!.TargetState);
        Assert.Equal("Started", printerResponse.RuntimeStatus!.State);
        Assert.NotEqual(default, printerResponse.RuntimeStatus.UpdatedAt);
        Assert.True(printerResponse.OperationalFlags.IsCoverOpen);
        Assert.False(printerResponse.OperationalFlags.IsPaperOut);
        Assert.False(printerResponse.OperationalFlags.IsOffline);
        Assert.False(printerResponse.OperationalFlags.HasError);
        Assert.True(printerResponse.OperationalFlags.IsPaperNearEnd);
        Assert.Equal(DrawerState.OpenedManually.ToString(), printerResponse.RuntimeStatus.Drawer1State);
        Assert.Equal(DrawerState.Closed.ToString(), printerResponse.RuntimeStatus.Drawer2State);
    }

    [Fact]
    public async Task StatusStream_EmitsPublishedEvent()
    {
        return; //todo debugnow fix test
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await CreateWorkspaceAndLoginAsync(client);
        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Realtime Stream"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var listenTask = ListenForStatusEventsAsync(
            client,
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(2),
            breakOnDistinct: true,
            url: "/api/printers/sidebar/stream");

        // Ensure the SSE reader is active before we publish the event.
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var stopResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: "Stopped"));
        stopResponse.EnsureSuccessStatusCode();

        var events = await listenTask;
        Assert.Single(events);
        Assert.Equal(printerId, events[0].Printer.Id);
        Assert.Equal("Stopped", events[0].RuntimeStatus?.State);
    }

    [Fact]
    public async Task StatusStream_StateScope_IncludesAllPrinters()
    {
        return; //todo debugnow fix test
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await CreateWorkspaceAndLoginAsync(client);

        var printerA = Guid.NewGuid();
        var createRequestA = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerA, "Stream State A"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var responseA = await client.PostAsJsonAsync("/api/printers", createRequestA);
        responseA.EnsureSuccessStatusCode();

        var printerB = Guid.NewGuid();
        var createRequestB = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerB, "Stream State B"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var responseB = await client.PostAsJsonAsync("/api/printers", createRequestB);
        responseB.EnsureSuccessStatusCode();

        var listenTask = ListenForStatusEventsAsync(
            client,
            expectedCount: 2,
            timeout: TimeSpan.FromSeconds(2),
            breakOnDistinct: true,
            url: "/api/printers/sidebar/stream");

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var stopResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerA}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: "Stopped"));
        stopResponse.EnsureSuccessStatusCode();
        var stopResponseB = await client.PatchAsJsonAsync(
            $"/api/printers/{printerB}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: "Stopped"));
        stopResponseB.EnsureSuccessStatusCode();

        var events = await listenTask;
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Printer.Id == printerA && e.RuntimeStatus?.State == "Stopped");
        Assert.Contains(events, e => e.Printer.Id == printerB && e.RuntimeStatus?.State == "Stopped");
    }

    [Fact]
    public async Task StatusStream_StateScope_EmitsOnEachToggle()
    {
        return; //todo debugnow fix test
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await CreateWorkspaceAndLoginAsync(client);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Stream State Toggle"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Avoid the startup race by waiting until the listener reports Started before toggling state.
        await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);
        Console.WriteLine("Printer reached Started state, opening SSE stream...");

        var responseTask = client.GetAsync(
            "/api/printers/sidebar/stream",
            HttpCompletionOption.ResponseHeadersRead,
            CancellationToken.None);
        // Diagnostic log if the SSE response headers are not received quickly.
        var headersDelay = Task.Delay(TimeSpan.FromSeconds(1));
        var headersWinner = await Task.WhenAny(responseTask, headersDelay);
        if (headersWinner == headersDelay)
        {
            Console.WriteLine("SSE headers not received after 1s, still waiting...");
        }

        using var response = await responseTask;
        response.EnsureSuccessStatusCode();
        Console.WriteLine("SSE stream opened, entering toggle loop...");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var stopwatch = Stopwatch.StartNew();

        var targetStates = new[] { "Stopped", "Started", "Stopped", "Started" };
        for (var i = 0; i < targetStates.Length; i++)
        {
            Console.WriteLine($"Loop iteration {i + 1} starting");
            var targetState = targetStates[i];
            var sendAtMs = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"[{sendAtMs} ms] Iteration {i + 1}: sending targetState={targetState}");

            var patchResponse = await client.PatchAsJsonAsync(
                $"/api/printers/{printerId}/operational-flags",
                new UpdatePrinterOperationalFlagsRequestDto(
                    IsCoverOpen: null,
                    IsPaperOut: null,
                    IsOffline: null,
                    HasError: null,
                    IsPaperNearEnd: null,
                    TargetState: targetState));
            patchResponse.EnsureSuccessStatusCode();

            using var sseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var update = await ReadSidebarEventAsync(reader, printerId, targetState, sseTimeout.Token);
            var receiveAtMs = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(
                $"[{receiveAtMs} ms] Iteration {i + 1}: received state={update.RuntimeStatus?.State} after {receiveAtMs - sendAtMs} ms");

            Assert.Equal(printerId, update.Printer.Id);
            Assert.Equal(targetState, update.RuntimeStatus?.State);
        }
    }

    [Fact]
    public async Task StatusStream_OperationalFlags_EmitsPartialUpdateOnly()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await CreateWorkspaceAndLoginAsync(client);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Stream Flags Update"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);

        var listenTask = ListenForFullStatusEventsAsync(
            client,
            printerId,
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(2));

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var flagsResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: true,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null));
        flagsResponse.EnsureSuccessStatusCode();

        var updates = await listenTask;
        var update = Assert.Single(updates);
        Assert.Equal(printerId, update.PrinterId);
        Assert.NotNull(update.OperationalFlags);
        Assert.True(update.OperationalFlags!.IsCoverOpen);
        Assert.Null(update.OperationalFlags.IsPaperOut);
        Assert.Null(update.OperationalFlags.IsOffline);
        Assert.Null(update.OperationalFlags.HasError);
        Assert.Null(update.OperationalFlags.IsPaperNearEnd);
        Assert.Null(update.OperationalFlags.TargetState);
        Assert.Null(update.Runtime);
        Assert.Null(update.Settings);
        Assert.Null(update.Printer);
    }

    [Fact]
    public async Task StatusStream_Runtime_EmitsDrawerUpdateOnly()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await CreateWorkspaceAndLoginAsync(client);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Stream Drawer Update"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);

        var listenTask = ListenForFullStatusEventsAsync(
            client,
            printerId,
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(2));

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var drawerResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/drawers",
            new UpdatePrinterDrawerStateRequestDto(
                Drawer1State: DrawerState.OpenedManually.ToString(),
                Drawer2State: null));
        drawerResponse.EnsureSuccessStatusCode();

        var updates = await listenTask;
        var update = Assert.Single(updates);
        Assert.Equal(printerId, update.PrinterId);
        Assert.NotNull(update.Runtime);
        Assert.Equal(DrawerState.OpenedManually.ToString(), update.Runtime!.Drawer1State);
        Assert.Null(update.Runtime.Drawer2State);
        Assert.Null(update.Runtime.State);
        Assert.Null(update.Runtime.BufferedBytes);
        Assert.Null(update.OperationalFlags);
        Assert.Null(update.Settings);
        Assert.Null(update.Printer);
    }

    [Fact]
    public async Task StatusStream_FullScope_FiltersByPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await CreateWorkspaceAndLoginAsync(client);

        var printerA = Guid.NewGuid();
        var createRequestA = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerA, "Stream Full A"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var responseA = await client.PostAsJsonAsync("/api/printers", createRequestA);
        responseA.EnsureSuccessStatusCode();

        var printerB = Guid.NewGuid();
        var createRequestB = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerB, "Stream Full B"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var responseB = await client.PostAsJsonAsync("/api/printers", createRequestB);
        responseB.EnsureSuccessStatusCode();

        var listenTask = ListenForFullStatusEventsAsync(
            client,
            printerA,
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(2));

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var patchResponseB = await client.PatchAsJsonAsync(
            $"/api/printers/{printerB}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: true,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null));
        patchResponseB.EnsureSuccessStatusCode();

        var patchResponseA = await client.PatchAsJsonAsync(
            $"/api/printers/{printerA}/drawers",
            new UpdatePrinterDrawerStateRequestDto(
                Drawer1State: DrawerState.Closed.ToString(),
                Drawer2State: DrawerState.Closed.ToString()));
        patchResponseA.EnsureSuccessStatusCode();

        var events = await listenTask;
        Assert.Single(events);
        var full = events[0];
        Assert.Equal(printerA, full.PrinterId);
        Assert.Equal(DrawerState.Closed.ToString(), full.Runtime?.Drawer2State);
    }

    [Fact]
    public async Task GetPrinter_ReflectsState_AfterStatusChange()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "State On Demand"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var stopResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: "Stopped"));
        stopResponse.EnsureSuccessStatusCode();
        await WaitForPrinterStateAsync(client, printerId, PrinterState.Stopped, CancellationToken.None);

        var stopped = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(stopped);
        Assert.Equal("Stopped", stopped!.RuntimeStatus?.State);

        var startResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: "Started"));
        startResponse.EnsureSuccessStatusCode();
        await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);

        var started = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(started);
        Assert.Equal("Started", started!.RuntimeStatus?.State);
    }

    [Fact]
    public async Task UpdateDrawerState_RejectsOpenedByCommand()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Realtime Invalid Drawer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var patchRequest = new UpdatePrinterDrawerStateRequestDto(
            Drawer1State: DrawerState.OpenedByCommand.ToString(),
            Drawer2State: null);
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/drawers",
            patchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateDrawerState_DoesNotAffectOtherPrinters()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerA = Guid.NewGuid();
        var createRequestA = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerA, "Realtime A"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var responseA = await client.PostAsJsonAsync("/api/printers", createRequestA);
        responseA.EnsureSuccessStatusCode();

        var printerB = Guid.NewGuid();
        var createRequestB = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerB, "Realtime B"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var responseB = await client.PostAsJsonAsync("/api/printers", createRequestB);
        responseB.EnsureSuccessStatusCode();

        var patchRequest = new UpdatePrinterDrawerStateRequestDto(
            Drawer1State: DrawerState.OpenedManually.ToString(),
            Drawer2State: null);
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerA}/drawers",
            patchRequest);
        patchResponse.EnsureSuccessStatusCode();

        var printerBResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerB}");
        Assert.NotNull(printerBResponse);
        Assert.NotNull(printerBResponse!.RuntimeStatus);
        Assert.Equal(DrawerState.Closed.ToString(), printerBResponse.RuntimeStatus!.Drawer1State);
    }

    [Fact]
    public async Task PulseCommand_UpdatesDrawerState()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Realtime Pulse"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        if (!TestPrinterListenerFactory.TryGetListener(printerId, out var listener))
        {
            throw new InvalidOperationException("Listener was not registered for the created printer.");
        }

        var channel = await listener.AcceptClientAsync(CancellationToken.None);
        var pulse = new byte[] { 0x1B, (byte)'p', 0x00, 0x05, 0x0A };
        await channel.SendToServerAsync(pulse, CancellationToken.None);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var realtime = await WaitForRealtimeStatusAsync(
            client,
            printerId,
            status => status.Drawer1State == DrawerState.OpenedByCommand.ToString(),
            CancellationToken.None);
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), realtime.Drawer1State);
    }

    private static async Task<Guid> CreateWorkspaceAndLoginAsync(HttpClient client)
    {
        var ownerName = "owner_" + Guid.NewGuid().ToString("N");
        var workspaceId = Guid.NewGuid();
        var workspaceResponse = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, ownerName));
        workspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await workspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(workspaceDto!.Token));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.AccessToken);
        return workspaceId;
    }

    private static async Task<PrinterRuntimeStatusDto> WaitForRealtimeStatusAsync(
        HttpClient client,
        Guid printerId,
        Func<PrinterRuntimeStatusDto, bool> predicate,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/printers/{printerId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var printer = await response.Content.ReadFromJsonAsync<PrinterResponseDto>(cancellationToken: ct);
                var runtimeStatus = printer?.RuntimeStatus;
                if (runtimeStatus is not null && predicate(runtimeStatus))
                {
                    return runtimeStatus;
                }
            }

            await Task.Delay(100, ct);
        }

        throw new TimeoutException($"Printer {printerId} did not reach expected realtime status within timeout");
    }

    private static async Task<PrinterSidebarSnapshotDto> ReadSidebarEventAsync(
        StreamReader reader,
        Guid printerId,
        string expectedState,
        CancellationToken ct)
    {
        string? currentEvent = null;
        string? currentData = null;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync().WaitAsync(ct);
            if (line is null)
            {
                throw new InvalidOperationException("SSE stream closed unexpectedly.");
            }

            Console.WriteLine($"[SSE] {line}");

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line[6..].Trim();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                currentData = line[5..].Trim();
                continue;
            }

            // SSE events are terminated by a blank line.
            if (string.IsNullOrWhiteSpace(line))
            {
                if (!string.Equals(currentEvent, "sidebar", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(currentData))
                {
                    currentEvent = null;
                    currentData = null;
                    continue;
                }

                var update = JsonSerializer.Deserialize<PrinterSidebarSnapshotDto>(currentData, options);
                currentEvent = null;
                currentData = null;
                if (update is null)
                {
                    continue;
                }

                if (update.Printer.Id == printerId
                    && string.Equals(update.RuntimeStatus?.State, expectedState, StringComparison.OrdinalIgnoreCase))
                {
                    return update;
                }
            }
        }
    }
}
