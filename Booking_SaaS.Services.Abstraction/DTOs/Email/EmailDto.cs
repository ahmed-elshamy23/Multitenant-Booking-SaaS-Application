namespace Booking_SaaS.Services.Abstraction.DTOs.Email;

public class EmailDto
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Link { get; set; }
    public MailTemplate Template { get; set; }
}