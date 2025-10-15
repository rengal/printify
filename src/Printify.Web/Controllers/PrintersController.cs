using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrintersController(IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = await mediator.Send(request.ToCommand(HttpContext.CaptureRequestContext()), cancellationToken);

        return Ok(new { id });
    }

    [HttpGet]
    public async Task<ActionResult<PrinterGroupsResponse>> List(CancellationToken cancellationToken)
    {
        return null;
    }

    [HttpPost("resolveTemporary")]
    public async Task<IActionResult> ResolveTemporary([FromBody] ResolveTemporaryRequest request, CancellationToken cancellationToken)
    {
        return NoContent();
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PrinterDto>> Get(long id, CancellationToken cancellationToken)
    {
        // var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        // _ = HttpContext.CaptureRequestMetadata(session.Id);
        //
        // var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        // if (printer is null)
        // {
        //     return NotFound();
        // }
        //
        // if (printer.OwnerSessionId != session.Id && printer.OwnerUserId != session.ClaimedUserId)
        // {
        //     return NotFound();
        // }
        //
        // return Ok(ContractMapper.ToPrinterDto(printer));
        return null;
    }

    public sealed record ResolveTemporaryRequest(IReadOnlyList<long> PrinterIds);

    public sealed record PrinterGroupsResponse(IReadOnlyList<PrinterDto> Temporary, IReadOnlyList<PrinterDto> UserClaimed);
}



