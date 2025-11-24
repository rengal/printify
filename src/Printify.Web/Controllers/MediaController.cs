using System;
using System.Diagnostics.CodeAnalysis;
using MediatR;
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
        var result = await mediator.Send(new GetMediaQuery(mediaId), cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return NotFound();
        }

        return File(result.Content, result.ContentType);
    }
}
