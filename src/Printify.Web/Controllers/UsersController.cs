using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printify.Application.Features.Users.CreateUser;
using Printify.Application.Features.Users.GetUserById;
using Printify.Application.Features.Users.ListUsers;
using Printify.Web.Contracts.Users.Requests;
using Printify.Web.Contracts.Users.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserRequestDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Capture caller identity metadata so the application layer can persist provenance.
        var context = HttpContext.CaptureRequestContext();
        var command = request.ToCommand(context);

        var user = await mediator.Send(command, ct).ConfigureAwait(false);
        var userDto = user.ToDto();

        return CreatedAtAction(nameof(GetUserById), new { id = userDto.Id }, userDto);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserByIdQuery(id), ct).ConfigureAwait(false);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user.ToDto());
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> ListUsers(CancellationToken ct)
    {
        // Query handler returns active users only; no extra filtering is needed here.
        var users = await mediator.Send(new ListUsersQuery(), ct).ConfigureAwait(false);

        return Ok(users.ToDtos());
    }
}
