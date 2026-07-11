namespace Booking_SaaS.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RequireIdempotencyKeyAttribute : Attribute
{
}