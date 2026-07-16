using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.Contracts;
using Booking_SaaS.Services.Abstraction.DTOs.Email;
using Booking_SaaS.Services.Abstraction.Options;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Booking_SaaS.Services;

internal class EmailService : IEmailService
{
    private readonly string _emailAddress;
    private readonly string _logoUrl;
    private readonly ISmtpClientWrapper _smtpClient;

    public EmailService(IOptions<EmailOptions> options, ISmtpClientWrapper smtpClient)
    {
        _emailAddress = options.Value.EmailAddress;
        _logoUrl = options.Value.LogoUrl;
        _smtpClient = smtpClient;
    }

    public void SendEmail(EmailDto email, string username, string tenantName)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_emailAddress, "Booking Application"),
            IsBodyHtml = true,
            Subject = email.Subject,
        };
        message.To.Add(email.To);

        message.Body = email.Template switch
        {
            MailTemplate.ConfirmEmail => BuildConfirmEmailTemplate(email.Link, username, tenantName),
            MailTemplate.ResetPassword => BuildResetPasswordTemplate(email.Link, username, tenantName),
            _ => throw new ArgumentOutOfRangeException(nameof(email.Template), email.Template,
                                                       "Unsupported email template.")
        };

        _smtpClient.Send(message);
    }

    private string BuildConfirmEmailTemplate(string link, string username, string tenantName)
    {
        return
            $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Email Confirmation</title><style>body{{font-family:Arial,sans-serif;background-color:#f4f4f7;margin:0;padding:20px}}.container{{max-width:600px;margin:auto;background:#ffffff;padding:28px;border-radius:10px;box-shadow:0 2px 8px rgba(0,0,0,0.08)}}.header{{margin-bottom:20px}}.header img{{width:140px}}h2{{margin:0 0 16px;color:#111827}}p{{color:#4b5563;line-height:1.6;margin:0 0 14px}}.button{{background-color:#2563eb;color:#ffffff !important;padding:12px 22px;text-decoration:none;border-radius:6px;display:inline-block;font-weight:600;margin:10px 0 18px}}.link-box{{background:#f9fafb;border:1px solid #e5e7eb;border-radius:6px;padding:10px;word-break:break-word;font-size:12px}}</style></head><body><div class=\"container\"><div class=\"header\"><img src=\"{_logoUrl}\" alt=\"Booking Application Logo\"></div><h2>Confirm your email</h2><p>Hello {username},</p><p>Thank you for creating your account at {tenantName}. Please confirm your email address to activate your account and securely continue.</p><a href=\"{link}\" target=\"_blank\" class=\"button\">Confirm Email</a><p>If the button does not work, copy and paste this link in your browser:</p><div class=\"link-box\">{link}</div><p>If you did not create this account, you can safely ignore this message.</p></div></body></html>";
    }

    private string BuildResetPasswordTemplate(string link, string username, string tenantName)
    {
        return
            $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Reset Password</title><style>body{{font-family:Arial,sans-serif;background-color:#f4f4f7;margin:0;padding:20px}}.container{{max-width:600px;margin:auto;background:#ffffff;padding:28px;border-radius:10px;box-shadow:0 2px 8px rgba(0,0,0,0.08)}}.header{{margin-bottom:20px}}.header img{{width:140px}}h2{{margin:0 0 16px;color:#111827}}p{{color:#4b5563;line-height:1.6;margin:0 0 14px}}.button{{background-color:#0f766e;color:#ffffff !important;padding:12px 22px;text-decoration:none;border-radius:6px;display:inline-block;font-weight:600;margin:10px 0 18px}}.link-box{{background:#f9fafb;border:1px solid #e5e7eb;border-radius:6px;padding:10px;word-break:break-word;font-size:12px}}.notice{{background:#fffbeb;border:1px solid #fde68a;color:#92400e;padding:10px;border-radius:6px;font-size:13px}}</style></head><body><div class=\"container\"><div class=\"header\"><img src=\"{_logoUrl}\" alt=\"Booking Application Logo\"></div><h2>Reset your password</h2><p>Hello {username},</p><p>We received a request to reset password for your {tenantName} account. Use the button below to create a new one.</p><a href=\"{link}\" target=\"_blank\" class=\"button\">Reset Password</a><p>If the button does not work, copy and paste this link in your browser:</p><div class=\"link-box\">{link}</div><p class=\"notice\">For your security, this link should only be used once and may expire shortly.</p><p>If you did not request a password reset, please ignore this email.</p></div></body></html>";
    }
}