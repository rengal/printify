using System.Diagnostics.CodeAnalysis;
using Mediator.Net;
using Microsoft.AspNetCore.Mvc;
using Printify.Application.Features.Media.GetMedia;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MediaController([NotNull] IMediator mediator) : ControllerBase
{
    [HttpGet("{mediaId:guid}")]
    public async Task<IActionResult> GetAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var result = await mediator
            .RequestAsync<GetMediaQuery, MediaDownloadResult?>(new GetMediaQuery(mediaId), cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(result.Checksum))
        {
            Response.Headers.ETag = $"\"sha256:{result.Checksum}\"";
        }

        // Explicitly set the content type so downstream callers (and tests) observe the original media MIME type.
        var contentType = string.IsNullOrWhiteSpace(result.ContentType)
            ? "application/octet-stream"
            : result.ContentType;

        return File(result.Content, contentType);
    }
}
