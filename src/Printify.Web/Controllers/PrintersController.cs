using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Printers;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrintersController : ControllerBase
{
    private readonly IResouceCommandService commandService;
    private readonly IResouceQueryService queryService;

    public PrintersController(IResouceCommandService commandService, IResouceQueryService queryService)
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

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Printer>> Get(long id, CancellationToken cancellationToken)
    {
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        return printer is null ? NotFound() : Ok(printer);
    }
}
