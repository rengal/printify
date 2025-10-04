using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;

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
        var id = await commandService.CreatePrinterAsync(request, cancellationToken).ConfigureAwait(false);
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(nameof(Get), new { id = printer.Id }, printer);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Printer>>> List([FromQuery] long? userId, CancellationToken cancellationToken)
    {
        if (userId is <= 0)
        {
            // Prevent negative or zero owner identifiers from reaching the query layer.
            return ValidationProblem(detail: "userId must be greater than zero when provided.", statusCode: StatusCodes.Status400BadRequest);
        }

        var printers = await queryService.ListPrintersAsync(userId, cancellationToken).ConfigureAwait(false);
        return Ok(printers);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Printer>> Get(long id, CancellationToken cancellationToken)
    {
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        return printer is null ? NotFound() : Ok(printer);
    }
}