using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Services;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MediaController : ControllerBase
{
    private readonly IBlobStorage blobStorage;

    public MediaController(IBlobStorage blobStorage)
    {
        this.blobStorage = blobStorage;
    }

    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetAsync(string mediaId, CancellationToken cancellationToken)
    {
        var stream = await blobStorage.GetAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return NotFound();
        }

        // Return the raw blob stream; MVC disposes the stream once the response completes.
        return File(stream, "application/octet-stream");
    }
}
