using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Services;
using Printify.Domain.Users;
using Printify.Web.Contracts.Users.Requests;
using Printify.Web.Contracts.Users.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

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
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var metadata = HttpContext.CaptureRequestMetadata(null);

        var saveRequest = DomainMapper.ToSaveUserRequest(request, metadata.IpAddress);
        var id = await commandService.CreateUserAsync(saveRequest, cancellationToken).ConfigureAwait(false);
        var user = await queryService.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(nameof(Get), new { id = user.Id }, ContractMapper.ToUserDto(user));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDto>> Get(long id, CancellationToken cancellationToken)
    {
        var user = await queryService.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null ? NotFound() : Ok(ContractMapper.ToUserDto(user));
    }
}
