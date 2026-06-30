using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace Booking_SaaS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiController : ControllerBase
{
    protected virtual ActionResult<Result> ToApiResponse(Result result)
    {
        if (result.IsSuccess)
            return Ok(result);

        return ToErrorResponse(result, result.Error!.ErrorType);
    }

    protected virtual ActionResult<Result<T>> ToApiResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result);

        return ToErrorResponse(result, result.Error!.ErrorType);
    }

    private ActionResult ToErrorResponse(object result, ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Failure => StatusCode(StatusCodes.Status500InternalServerError, result),
            ErrorType.NotFound => NotFound(result),
            ErrorType.Validation => BadRequest(result),
            ErrorType.Conflict => Conflict(result),
            ErrorType.Unauthorized => Unauthorized(result),
            ErrorType.AccessForbidden => StatusCode(StatusCodes.Status403Forbidden, result),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result)
        };
    }
}