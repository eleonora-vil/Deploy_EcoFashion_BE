using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using EcoFashionBackEnd.Helpers;
using EcoFashionBackEnd.Services;

public class EmailService : IEmailService
{
    private readonly MailSettings _mailSettings;

    public EmailService(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings.Value;
    }

    public async Task<bool> SendEmailAsync(MailData mailData)
    {
        try
        {
            using var smtpClient = new SmtpClient(_mailSettings.Server)
            {
                Port = int.Parse(_mailSettings.Port),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password),
                EnableSsl = true,
                Timeout = 10000
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_mailSettings.SenderEmail, _mailSettings.SenderName),
                Subject = mailData.EmailSubject,
                Body = mailData.EmailBody,
                IsBodyHtml = false
            };

            mailMessage.To.Add(new MailAddress(mailData.EmailToId, mailData.EmailToName));

            await smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine("[EmailService] Email sent OK to " + mailData.EmailToId);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"[EmailService] SMTP error: {smtpEx.StatusCode} - {smtpEx.Message}");
            Console.WriteLine(smtpEx.ToString());
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] General error: {ex.GetType()} - {ex.Message}");
            Console.WriteLine(ex.ToString());
            return false;
        }
    }
}
