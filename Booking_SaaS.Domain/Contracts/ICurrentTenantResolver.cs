namespace Booking_SaaS.Domain.Contracts;

public interface ITenantResolver
{
    int CurrentTenantId { get; set; }
}