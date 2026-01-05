using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
            printerId,
            "Realtime Missing",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(printerResponse);
        Assert.NotNull(printerResponse!.RealtimeStatus);
        Assert.Equal("Started", printerResponse.RealtimeStatus!.TargetState);
    }

    [Fact]
    public async Task GetPrinter_ReturnsRealtimeStatus_WhenAvailable()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            printerId,
            "Realtime Ready",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var patchRequest = new UpdatePrinterRealtimeStatusRequestDto(
            TargetStatus: null,
            IsCoverOpen: true,
            IsPaperOut: false,
            IsOffline: false,
            HasError: false,
            IsPaperNearEnd: true,
            Drawer1State: DrawerState.OpenedManually.ToString(),
            Drawer2State: DrawerState.Closed.ToString());
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/realtime-status",
            patchRequest);
        patchResponse.EnsureSuccessStatusCode();

        var printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(printerResponse);
        Assert.NotNull(printerResponse!.RealtimeStatus);
        Assert.Equal("Started", printerResponse.RealtimeStatus!.TargetState);
        Assert.Equal("Started", printerResponse.RealtimeStatus.State);
        Assert.NotEqual(default, printerResponse.RealtimeStatus!.UpdatedAt);
        Assert.True(printerResponse.RealtimeStatus.IsCoverOpen ?? false);
        Assert.False(printerResponse.RealtimeStatus.IsPaperOut ?? true);
        Assert.False(printerResponse.RealtimeStatus.IsOffline ?? true);
        Assert.False(printerResponse.RealtimeStatus.HasError ?? true);
        Assert.True(printerResponse.RealtimeStatus.IsPaperNearEnd ?? false);
        Assert.Equal(DrawerState.OpenedManually.ToString(), printerResponse.RealtimeStatus.Drawer1State);
        Assert.Equal(DrawerState.Closed.ToString(), printerResponse.RealtimeStatus.Drawer2State);
    }

    [Fact]
    public async Task StatusStream_EmitsPublishedEvent()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await CreateWorkspaceAndLoginAsync(client);
        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            printerId,
            "Realtime Stream",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var listenTask = ListenForStatusEventsAsync(
            client,
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(2),
            breakOnDistinct: true,
            url: "/api/printers/status/stream?scope=state");

        // Ensure the SSE reader is active before we publish the event.
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var stopResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: "Stopped",
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: null,
                Drawer2State: null));
        stopResponse.EnsureSuccessStatusCode();

        var events = await listenTask;
        Assert.Single(events);
        Assert.Equal(printerId, events[0].PrinterId);
        Assert.Equal("Stopped", events[0].TargetState);
        Assert.Equal("Stopped", events[0].State);
    }

    [Fact]
    public async Task StatusStream_StateScope_IncludesAllPrinters()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await CreateWorkspaceAndLoginAsync(client);

        var printerA = Guid.NewGuid();
        var createRequestA = new CreatePrinterRequestDto(
            printerA,
            "Stream State A",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var responseA = await client.PostAsJsonAsync("/api/printers", createRequestA);
        responseA.EnsureSuccessStatusCode();

        var printerB = Guid.NewGuid();
        var createRequestB = new CreatePrinterRequestDto(
            printerB,
            "Stream State B",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var responseB = await client.PostAsJsonAsync("/api/printers", createRequestB);
        responseB.EnsureSuccessStatusCode();

        var listenTask = ListenForStatusEventsAsync(
            client,
            expectedCount: 2,
            timeout: TimeSpan.FromSeconds(2),
            breakOnDistinct: false,
            url: "/api/printers/status/stream?scope=state");

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var stopResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerA}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: "Stopped",
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: null,
                Drawer2State: null));
        stopResponse.EnsureSuccessStatusCode();
        var startResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerB}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: "Started",
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: null,
                Drawer2State: null));
        startResponse.EnsureSuccessStatusCode();

        var events = await listenTask;
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.PrinterId == printerA && e.TargetState == "Stopped");
        Assert.Contains(events, e => e.PrinterId == printerB && e.TargetState == "Started");
    }

    [Fact]
    public async Task StatusStream_FullScope_FiltersByPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await CreateWorkspaceAndLoginAsync(client);

        var printerA = Guid.NewGuid();
        var createRequestA = new CreatePrinterRequestDto(
            printerA,
            "Stream Full A",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var responseA = await client.PostAsJsonAsync("/api/printers", createRequestA);
        responseA.EnsureSuccessStatusCode();

        var printerB = Guid.NewGuid();
        var createRequestB = new CreatePrinterRequestDto(
            printerB,
            "Stream Full B",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var responseB = await client.PostAsJsonAsync("/api/printers", createRequestB);
        responseB.EnsureSuccessStatusCode();

        var listenTask = ListenForFullStatusEventsAsync(
            client,
            printerA,
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(2));

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var patchResponseB = await client.PatchAsJsonAsync(
            $"/api/printers/{printerB}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: null,
                IsCoverOpen: true,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: null,
                Drawer2State: null));
        patchResponseB.EnsureSuccessStatusCode();

        var patchResponseA = await client.PatchAsJsonAsync(
            $"/api/printers/{printerA}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: null,
                IsCoverOpen: true,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: DrawerState.Closed.ToString(),
                Drawer2State: DrawerState.Closed.ToString()));
        patchResponseA.EnsureSuccessStatusCode();

        var events = await listenTask;
        Assert.Single(events);
        var full = events[0];
        Assert.Equal(printerA, full.PrinterId);
        Assert.True(full.IsCoverOpen ?? false);
        Assert.Equal(DrawerState.Closed.ToString(), full.Drawer2State);
    }

    [Fact]
    public async Task GetPrinter_ReflectsState_AfterStatusChange()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            printerId,
            "State On Demand",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var stopResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: "Stopped",
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: null,
                Drawer2State: null));
        stopResponse.EnsureSuccessStatusCode();
        await WaitForPrinterStateAsync(client, printerId, PrinterState.Stopped, CancellationToken.None);

        var stopped = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(stopped);
        Assert.Equal("Stopped", stopped!.RealtimeStatus?.State);

        var startResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/realtime-status",
            new UpdatePrinterRealtimeStatusRequestDto(
                TargetStatus: "Started",
                IsCoverOpen: null,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                Drawer1State: null,
                Drawer2State: null));
        startResponse.EnsureSuccessStatusCode();
        await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);

        var started = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(started);
        Assert.Equal("Started", started!.RealtimeStatus?.State);
    }

    [Fact]
    public async Task UpdateRealtimeStatus_RejectsOpenedByCommand()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            printerId,
            "Realtime Invalid Drawer",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var patchRequest = new UpdatePrinterRealtimeStatusRequestDto(
            TargetStatus: null,
            IsCoverOpen: null,
            IsPaperOut: null,
            IsOffline: null,
            HasError: null,
            IsPaperNearEnd: null,
            Drawer1State: DrawerState.OpenedByCommand.ToString(),
            Drawer2State: null);
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/realtime-status",
            patchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateRealtimeStatus_DoesNotAffectOtherPrinters()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerA = Guid.NewGuid();
        var createRequestA = new CreatePrinterRequestDto(
            printerA,
            "Realtime A",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var responseA = await client.PostAsJsonAsync("/api/printers", createRequestA);
        responseA.EnsureSuccessStatusCode();

        var printerB = Guid.NewGuid();
        var createRequestB = new CreatePrinterRequestDto(
            printerB,
            "Realtime B",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
        var responseB = await client.PostAsJsonAsync("/api/printers", createRequestB);
        responseB.EnsureSuccessStatusCode();

        var patchRequest = new UpdatePrinterRealtimeStatusRequestDto(
            TargetStatus: null,
            IsCoverOpen: true,
            IsPaperOut: null,
            IsOffline: null,
            HasError: null,
            IsPaperNearEnd: null,
            Drawer1State: DrawerState.OpenedManually.ToString(),
            Drawer2State: null);
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerA}/realtime-status",
            patchRequest);
        patchResponse.EnsureSuccessStatusCode();

        var printerBResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerB}");
        Assert.NotNull(printerBResponse);
        Assert.NotNull(printerBResponse!.RealtimeStatus);
        Assert.Equal(DrawerState.Closed.ToString(), printerBResponse.RealtimeStatus!.Drawer1State);
    }

    [Fact]
    public async Task PulseCommand_UpdatesDrawerState()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            printerId,
            "Realtime Pulse",
            "EscPos",
            512,
            null,
            false,
            null,
            null);
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

    private static async Task<PrinterRealtimeStatusDto> WaitForRealtimeStatusAsync(
        HttpClient client,
        Guid printerId,
        Func<PrinterRealtimeStatusDto, bool> predicate,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/printers/{printerId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var printer = await response.Content.ReadFromJsonAsync<PrinterResponseDto>(cancellationToken: ct);
                var realtime = printer?.RealtimeStatus;
                if (realtime is not null && predicate(realtime))
                {
                    return realtime;
                }
            }

            await Task.Delay(100, ct);
        }

        throw new TimeoutException($"Printer {printerId} did not reach expected realtime status within timeout");
    }
}
