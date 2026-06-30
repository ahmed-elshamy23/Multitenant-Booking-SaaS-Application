namespace Booking_SaaS.Domain.Enums;

public enum ErrorType
{
    Failure = 1,
    NotFound,
    Validation,
    Conflict,
    Unauthorized,
    AccessForbidden
}