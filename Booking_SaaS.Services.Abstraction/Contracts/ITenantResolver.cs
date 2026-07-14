namespace Booking_SaaS.Services.Abstraction.Contracts;

public interface ITenantResolver
{
    int CurrentTenantId { get; set; }
}