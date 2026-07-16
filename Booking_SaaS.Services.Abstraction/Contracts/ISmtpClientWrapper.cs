using System.Net.Mail;

namespace Booking_SaaS.Services.Abstraction.Contracts;

public interface ISmtpClientWrapper
{
    void Send(MailMessage message);
}
