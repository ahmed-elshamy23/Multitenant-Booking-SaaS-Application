using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction.Contracts;
using Booking_SaaS.Services.Abstraction.DTOs.Email;
using Booking_SaaS.Services.Abstraction.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Mail;

namespace Booking_SaaS.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<ISmtpClientWrapper> _smtpClientMock = new();
    private readonly IOptions<EmailOptions> _options;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _options = Options.Create(new EmailOptions()
        {
            EmailAddress = "test@test.com",
            Password = "P@ssw0rd",
            LogoUrl = "https://logo.com"
        });

        _emailService = new EmailService(_options, _smtpClientMock.Object);
    }

    [Fact]
    public void SendEmail_ShouldThrowArgumentOutOfRangeExceptionWhenTemplateIsUnsupported()
    {
        var dto = new EmailDto
        {
            To = "user@example.com",
            Subject = "Test",
            Link = "https://test.com",
            Template = (MailTemplate)999
        };

        var action = () => _emailService.SendEmail(dto, "John Doe", "TenantA");

        action.Should().Throw<ArgumentOutOfRangeException>();
        _smtpClientMock.Verify(x => x.Send(It.IsAny<MailMessage>()), Times.Never);
    }

    [Fact]
    public void SendEmail_ShouldPassWhenTemplateIsConfirmEmail()
    {
        var dto = new EmailDto
        {
            To = "user@example.com",
            Subject = "Confirm your email",
            Link = "https://test.com",
            Template = MailTemplate.ConfirmEmail
        };

        MailMessage? sentMessage = null;
        _smtpClientMock
            .Setup(x => x.Send(It.IsAny<MailMessage>()))
            .Callback<MailMessage>(m => sentMessage = m);

        _emailService.SendEmail(dto, "John Doe", "TenantA");

        _smtpClientMock.Verify(x => x.Send(It.IsAny<MailMessage>()), Times.Once);

        sentMessage.Should().NotBeNull();
        sentMessage.Subject.Should().Be(dto.Subject);

        sentMessage.From.Should().NotBeNull();
        sentMessage.From!.Address.Should().Be(_options.Value.EmailAddress);

        sentMessage.IsBodyHtml.Should().BeTrue();
        sentMessage.Body.Should().Contain(_options.Value.LogoUrl);
        sentMessage.Body.Should().Contain("Confirm Email");

        sentMessage!.To.Should().ContainSingle()
            .Which.Address.Should().Be(dto.To);

        sentMessage.Body.Should().Contain("Confirm your email")
            .And.Contain("John Doe")
            .And.Contain("TenantA")
            .And.Contain(dto.Link);
    }

    [Fact]
    public void SendEmail_ShouldPassWhenTemplateIsResetPassword()
    {
        var dto = new EmailDto
        {
            To = "user@example.com",
            Subject = "Reset your password",
            Link = "https://test.com",
            Template = MailTemplate.ResetPassword
        };

        MailMessage? sentMessage = null;
        _smtpClientMock
            .Setup(x => x.Send(It.IsAny<MailMessage>()))
            .Callback<MailMessage>(m => sentMessage = m);

        _emailService.SendEmail(dto, "John Doe", "TenantB");

        _smtpClientMock.Verify(x => x.Send(It.IsAny<MailMessage>()), Times.Once);

        sentMessage.Should().NotBeNull();
        sentMessage.Subject.Should().Be(dto.Subject);

        sentMessage.From.Should().NotBeNull();
        sentMessage.From!.Address.Should().Be(_options.Value.EmailAddress);

        sentMessage.IsBodyHtml.Should().BeTrue();
        sentMessage.Body.Should().Contain(_options.Value.LogoUrl);
        sentMessage.Body.Should().Contain("Reset Password");

        sentMessage!.To.Should().ContainSingle()
            .Which.Address.Should().Be(dto.To);

        sentMessage.Body.Should().Contain("Reset your password")
            .And.Contain("John Doe")
            .And.Contain("TenantB")
            .And.Contain(dto.Link);
    }
}