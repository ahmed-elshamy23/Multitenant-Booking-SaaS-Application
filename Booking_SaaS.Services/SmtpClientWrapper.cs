using Booking_SaaS.Services.Abstraction.Contracts;
using Booking_SaaS.Services.Abstraction.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

internal class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly string _emailAddress;
    private readonly string _password;

    public SmtpClientWrapper(IOptions<EmailOptions> options)
    {
        _emailAddress = options.Value.EmailAddress;
        _password = options.Value.Password;
    }

    public void Send(MailMessage message)
    {
        using var smtpClient = new SmtpClient("smtp.gmail.com", 587);
        smtpClient.EnableSsl = true;
        smtpClient.Credentials = new NetworkCredential(_emailAddress, _password);
        smtpClient.Send(message);
    }
}