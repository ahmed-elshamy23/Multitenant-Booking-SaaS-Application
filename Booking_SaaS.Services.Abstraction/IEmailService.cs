using Booking_SaaS.Services.Abstraction.DTOs.Email;

namespace Booking_SaaS.Services.Abstraction;

public interface IEmailService
{
    void SendEmail(EmailDto email, string username, string tenantName);
}