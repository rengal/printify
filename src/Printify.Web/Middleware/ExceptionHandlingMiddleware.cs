using System.Net;
using System.Net.Mime;
using System.Text.Json;
using FluentValidation;
using Printify.Application.Exceptions;

namespace Printify.Web.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return;
        }

        // Stream disconnection is expected during SSE - not an error
        if (ex is StreamDisconnectedException)
        {
            logger.LogDebug("SSE stream disconnected: {Message}", ex.Message);
            return;
        }

        logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        var status = ex switch
        {
            // 400-series errors represent invalid client input.
            AuthenticationFailedException => HttpStatusCode.Unauthorized,
            PrinterNotFoundException => HttpStatusCode.NotFound,
            BadRequestException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            ValidationException => HttpStatusCode.BadRequest,
            // 500-series errors represent server-side failures.
            PrinterListenerStartFailedException => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };

        var problem = new
        {
            Status = (int)status,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        var json = JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json);
    }
}
