using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Documents;
using Printify.Contracts.Resources;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] SaveUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet("{id:long}")]
    public IActionResult Get(long id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
