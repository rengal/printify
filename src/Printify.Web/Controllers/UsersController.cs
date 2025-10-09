using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Services;
using Printify.Domain.Users;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IResourceCommandService commandService;
    private readonly IResourceQueryService queryService;

    public UsersController(IResourceCommandService commandService, IResourceQueryService queryService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var createdFromIp = string.IsNullOrWhiteSpace(request.CreatedFromIp)
            ? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : request.CreatedFromIp;

        var normalized = request with { CreatedFromIp = createdFromIp };
        var id = await commandService.CreateUserAsync(normalized, cancellationToken).ConfigureAwait(false);
        var user = await queryService.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<User>> Get(long id, CancellationToken cancellationToken)
    {
        var user = await queryService.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null ? NotFound() : Ok(user);
    }
}
