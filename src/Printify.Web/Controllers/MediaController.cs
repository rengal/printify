using System;
using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Services;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MediaController : ControllerBase
{
    private readonly IMediaStorage mediaStorage;

    public MediaController(IMediaStorage mediaStorage)
    {
        this.mediaStorage = mediaStorage;
    }

    [HttpGet("{mediaId:guid}")]
    public async Task<IActionResult> GetAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var stream = await mediaStorage.OpenReadAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return NotFound();
        }

        // Return the raw blob stream; MVC disposes the stream once the response completes.
        return File(stream, "application/octet-stream");
    }
}
