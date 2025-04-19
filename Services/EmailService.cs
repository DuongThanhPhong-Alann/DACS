using Microsoft.Extensions.Options;
using QLCCCC.Models;
using QLCCCC.Repositories.Interfaces;
using System.Net.Mail;
using System.Net;

namespace QLCCCC.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, string? from = null)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(from ?? _emailSettings.FromAddress, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            using (var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                smtpClient.EnableSsl = true;

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (SmtpException ex)
                {
                    // Ghi log lỗi nếu cần
                    Console.WriteLine($"SMTP Error: {ex.Message}");
                }
            }
        }
    }
}
