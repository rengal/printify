using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Resources;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrintersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] SavePrinterRequest request)
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
