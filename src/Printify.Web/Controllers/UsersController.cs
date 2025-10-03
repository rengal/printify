using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Users;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IResouceCommandService commandService;
    private readonly IResouceQueryService queryService;

    public UsersController(IResouceCommandService commandService, IResouceQueryService queryService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var id = await commandService.CreateUserAsync(request, cancellationToken).ConfigureAwait(false);
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
