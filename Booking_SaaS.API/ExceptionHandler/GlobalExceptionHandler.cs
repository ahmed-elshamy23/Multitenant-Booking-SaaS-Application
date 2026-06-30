using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.API.ExceptionHandler;

public class ExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequestStatusCode = 499;

    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = exception switch
        {
            OperationCanceledException => ClientClosedRequestStatusCode,
            DbUpdateConcurrencyException => (int)HttpStatusCode.Conflict,
            _ => (int)HttpStatusCode.InternalServerError
        };

        if (exception is not OperationCanceledException)
            _logger.LogError(exception, "Request failed: {Method} {Path}", httpContext.Request.Method,
                httpContext.Request.Path);
        else
            _logger.LogWarning(exception, "Request cancelled: {Method} {Path}", httpContext.Request.Method,
                httpContext.Request.Path);

        var response = new
        {
            statusCode = httpContext.Response.StatusCode,
            message = httpContext.Response.StatusCode switch
            {
                (int)HttpStatusCode.InternalServerError => "An error occurred",
                ClientClosedRequestStatusCode => "Request was canceled",
                (int)HttpStatusCode.Conflict => "Entity was modified by another user",
                _ => exception.Message
            }
        };
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        return true;
    }
}