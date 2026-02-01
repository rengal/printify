using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using EscPosCommands = Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Specifications;
using Printify.Tests.Shared.Document;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Printify.Web.Contracts.Documents.Responses.Canvas;
using Printify.TestServices;

namespace Printify.Web.Tests.EscPos;

public class EscPosTests(WebApplicationFactory<Program> factory)
    : ProtocolTestsBase<EscPosScenario>(
        factory,
        Protocol.EscPos)
{
    protected const byte Esc = 0x1B;
    protected const byte Gs = 0x1D;

    // ESC/POS has dynamic height (null) since content height varies
    protected override int? DefaultPrinterHeightInDots => null;
    protected override int DefaultPrinterWidthInDots => EscPosSpecs.DefaultCanvasWidth;

    public override async Task DocumentCompletesAfterIdleTimeout()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(Factory);
        await AuthenticateAsync(environment, "escpos-idle-user");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(
            environment,
            printerId,
            "EscPos Idle Printer",
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);

        await using var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();
        var nextEventTask = streamEnumerator.MoveNextAsync().AsTask();

        var bytesSent = 0;
        channel.OnResponse(data => bytesSent += data.Length);

        await channel.SendToServerAsync(new byte[] { 0x07 }, CancellationToken.None);

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var elapsed = 0;
        const int stepMs = 50;
        while (elapsed + stepMs < PrinterConstants.ListenerIdleTimeoutMs)
        {
            clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
            await Task.Delay(1);
            Assert.False(nextEventTask.IsCompleted);
            elapsed += stepMs;
        }

        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
        await nextEventTask.WaitAsync(TimeSpan.FromMilliseconds(500));

        var expectedElements = new List<Command> { new EscPosCommands.Bell { LengthInBytes = 1 } };

        Assert.True(nextEventTask.Result);
        var documentEvent = streamEnumerator.Current;
        DocumentAssertions.Equal(
            expectedElements,
            Protocol.EscPos,
            documentEvent.Document,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(expectedElements.Sum(element => element.LengthInBytes), bytesSent, documentEvent.Document);

        // Verify view endpoint works
        var ct = CancellationToken.None;
        var canvasResponse = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/canvas?limit=10");
        canvasResponse.EnsureSuccessStatusCode();
        var canvasDocumentList = await canvasResponse.Content.ReadFromJsonAsync<CanvasDocumentListResponseDto>(cancellationToken: ct);
        Assert.NotNull(canvasDocumentList);
        var canvasDocuments = canvasDocumentList.Result.Items;
        var documentId = documentEvent.Document.Id;
        var canvasDocument = canvasDocuments.FirstOrDefault(doc => doc.Id == documentId)
            ?? canvasDocuments.FirstOrDefault();
        Assert.NotNull(canvasDocument);

        DocumentAssertions.EqualCanvas(
            [[new CanvasDebugElementDto("bell") { LengthInBytes = 1 }]],
            Protocol.EscPos,
            canvasDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(expectedElements.Sum(element => element.LengthInBytes), bytesSent, canvasDocument);

        // Verify RasterImage elements have accessible Media URLs
        foreach (var element in canvasDocument.Canvases[0].Items)
        {
            if (element is CanvasImageElementDto viewImage)
            {
                Assert.False(string.IsNullOrWhiteSpace(viewImage.Media.Url));

                // Verify the media URL is accessible
                var mediaResponse = await environment.Client.GetAsync(viewImage.Media.Url);
                mediaResponse.EnsureSuccessStatusCode();

                // Verify content type is an image
                var contentType = mediaResponse.Content.Headers.ContentType?.MediaType;
                Assert.NotNull(contentType);
                Assert.True(contentType.StartsWith("image/"),
                    $"Expected image content type, but got {contentType}");

                var responseLength = mediaResponse.Content.Headers.ContentLength;
                Assert.NotNull(responseLength);
                Assert.Equal(viewImage.Media.Size, responseLength.Value);

                var payload = await mediaResponse.Content.ReadAsByteArrayAsync();
                Assert.Equal(responseLength.Value, payload.LongLength);
                Assert.Equal(viewImage.Media.Size, payload.LongLength);

                // Verify SHA-256 checksum is valid
                if (!string.IsNullOrWhiteSpace(viewImage.Media.StorageKey))
                {
                    var computedChecksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
                    Assert.Equal(viewImage.Media.StorageKey, computedChecksum);

                    var eTag = mediaResponse.Headers.ETag?.Tag;
                    Assert.NotNull(eTag);
                    Assert.StartsWith("\"sha256:", eTag, StringComparison.OrdinalIgnoreCase);
                    var eTagValue = eTag.Trim('"');
                    var eTagChecksum = eTagValue.Split(':', 2).ElementAtOrDefault(1);
                    Assert.False(string.IsNullOrWhiteSpace(eTagChecksum));
                    Assert.Equal(computedChecksum, eTagChecksum.ToLowerInvariant());
                }
            }
        }
    }
}
