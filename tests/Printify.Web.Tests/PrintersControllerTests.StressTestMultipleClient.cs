using System.Net.Http.Json;
using Printify.TestServices;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using PrinterRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterDto;
using PrinterSettingsRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterSettingsDto;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    //[Theory] //todo debugnow
    //[InlineData(1)]
    //[InlineData(2)]
    //[InlineData(5)]
    //[InlineData(10)]
    //[InlineData(20)]
    //[InlineData(50)]
    //[InlineData(100)]
    public async Task CreateNPrintersInParallel_AssignsUniquePorts(int n)
    {
        // Scenario: many concurrent clients create printers; server must assign unique ports.
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var statusTask = ListenForStatusEventsAsync(environment.Client, n, TimeSpan.FromSeconds(10));

        var createTasks = new List<Task<PrinterResponseDto?>>(n);

        for (var i = 0; i < n; i++)
        {
            var client = environment.CreateClient();
            client.DefaultRequestHeaders.Authorization = environment.Client.DefaultRequestHeaders.Authorization;

            var printerId = Guid.NewGuid();
            var request = new CreatePrinterRequestDto(
                new PrinterRequestDto(printerId, $"Parallel-{i}"),
                new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
            createTasks.Add(CreatePrinterAsync(client, request));
        }

        try
        {
            var created = await Task.WhenAll(createTasks).WaitAsync(TimeSpan.FromSeconds(15));
            var ports = created
                .Where(p => p != null)
                .Select(p => p!.Settings.TcpListenPort)
                .ToList();

            Assert.Equal(ports.Count, ports.Distinct().Count());
            Assert.True(ports.All(p => p > 0));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException("Timed out while creating printers in parallel.");
        }

        var statusEvents = await statusTask;
        var distinctStatusPrinters = statusEvents.Select(e => e.Printer.Id).Distinct().Count();
        Assert.True(distinctStatusPrinters >= n, "Did not observe status events for all printers.");
    }

    private static async Task<PrinterResponseDto?> CreatePrinterAsync(HttpClient client, CreatePrinterRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/printers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrinterResponseDto>();
    }

}
