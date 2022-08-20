using AdminLte.Mvc.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AdminLte.Mvc.Services;

public class BTEmailService : IEmailSender
{
    private readonly MailSettings _mailSettings;

    public BTEmailService(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings.Value;
    }
    public async Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
    {
        MimeMessage email = new();

        email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
        email.To.Add(MailboxAddress.Parse(emailTo));
        email.Subject = subject;

        var builder = new BodyBuilder()
        {
            HtmlBody = htmlMessage
        };

        email.Body = builder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception e)
        {
            Console.WriteLine($"*** Error *** - Error sending Email in (SendEmailAsync) function.  ---> {e.Message}");
            throw;
        }
    }
}
