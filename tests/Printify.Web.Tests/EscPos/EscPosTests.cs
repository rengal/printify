using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using ApplicationEvents = Printify.Application.Printing.Events;
using DomainElements = Printify.Domain.Documents.Elements;
using EscPosElements = Printify.Domain.Documents.Elements.EscPos;
using Printify.Domain.Printers;
using Printify.Tests.Shared.Document;
using Printify.Tests.Shared.EscPos;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Documents.Responses.View;
using Printify.Web.Contracts.Documents.Responses.View.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosTests(WebApplicationFactory<Program> factory)
    : ProtocolTestsBase<EscPosScenario>(
        factory,
        Protocol.EscPos,
        "EscPos")
{
    protected const byte Esc = 0x1B;
    protected const byte Gs = 0x1D;

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

        var expectedElements = new List<DomainElements.Element> { new EscPosElements.Bell {LengthInBytes = 1} };

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
        var viewResponse = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/view?limit=10");
        viewResponse.EnsureSuccessStatusCode();
        var viewDocumentList = await viewResponse.Content.ReadFromJsonAsync<ViewDocumentListResponseDto>(cancellationToken: ct);
        Assert.NotNull(viewDocumentList);
        var viewDocuments = viewDocumentList.Result.Items;
        var documentId = documentEvent.Document.Id;
        var viewDocument = viewDocuments.FirstOrDefault(doc => doc.Id == documentId)
            ?? viewDocuments.FirstOrDefault();
        Assert.NotNull(viewDocument);

        DocumentAssertions.EqualView(
            [],
            Protocol.EscPos,
            viewDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(expectedElements.Sum(element => element.LengthInBytes), bytesSent, viewDocument);

        // Verify RasterImage elements have accessible Media URLs
        foreach (var element in viewDocument.Elements)
        {
            if (element is ViewImageElementDto viewImage)
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
                Assert.Equal(viewImage.Media.Length, responseLength.Value);

                var payload = await mediaResponse.Content.ReadAsByteArrayAsync();
                Assert.Equal(responseLength.Value, payload.LongLength);
                Assert.Equal(viewImage.Media.Length, payload.LongLength);

                // Verify SHA-256 checksum is valid
                if (!string.IsNullOrWhiteSpace(viewImage.Media.Sha256))
                {
                    var computedChecksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
                    Assert.Equal(viewImage.Media.Sha256, computedChecksum);

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
