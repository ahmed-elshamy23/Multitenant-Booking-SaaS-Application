using Booking_SaaS.Services.Abstraction.Orchestrators;

namespace Booking_SaaS.Services.Abstraction;

public interface IServiceManager
{
    IAuthenticationOrchestrator AuthenticationOrchestrator { get; }
    IBookingOrchestrator BookingOrchestrator { get; }
    ITenantService TenantService { get; }
    IResourceOrchestrator ResourceOrchestrator { get; }
}