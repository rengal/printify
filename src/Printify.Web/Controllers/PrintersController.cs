using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;
using Printify.Web.Security;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrintersController : ControllerBase
{
    private readonly IResourceCommandService commandService;
    private readonly IResourceQueryService queryService;

    public PrintersController(IResourceCommandService commandService, IResourceQueryService queryService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavePrinterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? request.CreatedFromIp;
        var normalizedRequest = request with { OwnerUserId = user.Id, CreatedFromIp = remoteIp };

        var id = await commandService.CreatePrinterAsync(normalizedRequest, cancellationToken).ConfigureAwait(false);
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(nameof(Get), new { id = printer.Id }, printer);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Printer>>> List(CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        var printers = await queryService.ListPrintersAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Ok(printers);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Printer>> Get(long id, CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null || printer.OwnerUserId != user.Id)
        {
            return NotFound();
        }

        return Ok(printer);
    }

    private async Task<User?> GetAuthenticatedUserAsync(CancellationToken cancellationToken)
    {
        if (!TokenService.TryExtractUsername(HttpContext, out var username))
        {
            return null;
        }

        return await queryService.FindUserByNameAsync(username, cancellationToken).ConfigureAwait(false);
    }
}